"""
Bot Detection Module for AdImpactOs
Integrates with Azure Application Insights for logging
"""

import re
from typing import Dict, Tuple, Optional
from dataclasses import dataclass
from datetime import datetime, timedelta
import hashlib
from collections import defaultdict

@dataclass
class BotDetectionConfig:
    """Configuration for bot detection thresholds"""
    rate_limit_per_minute: int = 100
    rate_limit_per_hour: int = 1000
    min_time_between_requests_ms: int = 100
    fingerprint_score_threshold: float = 0.7
    enable_appinsights_logging: bool = True


@dataclass
class BotDetectionResult:
    """Result of bot detection analysis"""
    is_bot: bool
    confidence_score: float
    reasons: list[str]
    detection_time: datetime
    
    def to_dict(self) -> dict:
        return {
            "is_bot": self.is_bot,
            "confidence_score": self.confidence_score,
            "reasons": self.reasons,
            "detection_time": self.detection_time.isoformat()
        }


class BotDetector:
    """
    Bot detection module with multiple heuristics:
    - User-Agent analysis
    - IP-based rate limiting
    - Device fingerprinting
    - Behavioral patterns
    """
    
    # Known bot user agent patterns
    BOT_UA_PATTERNS = [
        r'bot', r'crawler', r'spider', r'scraper',
        r'curl', r'wget', r'python-requests', r'java/',
        r'go-http-client', r'okhttp', r'httpclient',
        r'headless', r'phantom', r'selenium', r'webdriver',
        r'slurp', r'bingbot', r'googlebot', r'baiduspider',
        r'yandex', r'duckduckbot', r'facebookexternalhit',
        r'whatsapp', r'telegram', r'slack', r'discord'
    ]
    
    # Suspicious patterns
    SUSPICIOUS_PATTERNS = [
        r'test', r'check', r'monitor', r'scan',
        r'security', r'penetration', r'vulnerability'
    ]
    
    def __init__(self, config: Optional[BotDetectionConfig] = None):
        self.config = config or BotDetectionConfig()
        self._ip_request_history: Dict[str, list] = defaultdict(list)
        self._fingerprint_history: Dict[str, list] = defaultdict(list)
        
        # Compile regex patterns
        self._bot_ua_regex = re.compile(
            '|'.join(self.BOT_UA_PATTERNS),
            re.IGNORECASE
        )
        self._suspicious_regex = re.compile(
            '|'.join(self.SUSPICIOUS_PATTERNS),
            re.IGNORECASE
        )
    
    def detect(
        self,
        user_agent: Optional[str],
        ip_address: Optional[str],
        headers: Optional[Dict[str, str]] = None,
        fingerprint: Optional[str] = None
    ) -> BotDetectionResult:
        """
        Main bot detection method
        
        Args:
            user_agent: HTTP User-Agent string
            ip_address: Client IP address
            headers: HTTP headers dictionary
            fingerprint: Device fingerprint hash
            
        Returns:
            BotDetectionResult with detection outcome and reasons
        """
        reasons = []
        score = 0.0
        
        # 1. User-Agent analysis
        ua_score, ua_reasons = self._analyze_user_agent(user_agent)
        score += ua_score
        reasons.extend(ua_reasons)
        
        # 2. IP rate limiting
        if ip_address:
            rate_score, rate_reasons = self._check_rate_limit(ip_address)
            score += rate_score
            reasons.extend(rate_reasons)
        
        # 3. Header analysis
        if headers:
            header_score, header_reasons = self._analyze_headers(headers)
            score += header_score
            reasons.extend(header_reasons)
        
        # 4. Fingerprint analysis
        if fingerprint:
            fp_score, fp_reasons = self._analyze_fingerprint(fingerprint)
            score += fp_score
            reasons.extend(fp_reasons)
        
        # Normalize score (0-1)
        confidence = min(score / 4.0, 1.0)
        is_bot = confidence >= self.config.fingerprint_score_threshold
        
        result = BotDetectionResult(
            is_bot=is_bot,
            confidence_score=confidence,
            reasons=reasons,
            detection_time=datetime.utcnow()
        )
        
        # Log to Application Insights if enabled
        if self.config.enable_appinsights_logging:
            self._log_to_appinsights(result, user_agent, ip_address)
        
        return result
    
    def _analyze_user_agent(self, user_agent: Optional[str]) -> Tuple[float, list[str]]:
        """Analyze User-Agent string for bot indicators"""
        if not user_agent or user_agent.strip() == "":
            return (1.0, ["missing_user_agent"])
        
        if user_agent.lower() == "unknown":
            return (0.8, ["unknown_user_agent"])
        
        reasons = []
        score = 0.0
        
        # Check bot patterns
        if self._bot_ua_regex.search(user_agent):
            reasons.append("bot_pattern_in_ua")
            score += 1.0
        
        # Check suspicious patterns
        if self._suspicious_regex.search(user_agent):
            reasons.append("suspicious_ua_pattern")
            score += 0.5
        
        # Check for overly simple UA
        if len(user_agent) < 20:
            reasons.append("suspiciously_short_ua")
            score += 0.3
        
        # Check for lack of browser info
        if not any(browser in user_agent.lower() for browser in ['chrome', 'firefox', 'safari', 'edge']):
            reasons.append("no_common_browser_in_ua")
            score += 0.2
        
        return (score, reasons)
    
    def _check_rate_limit(self, ip_address: str) -> Tuple[float, list[str]]:
        """Check IP-based rate limiting"""
        now = datetime.utcnow()
        reasons = []
        score = 0.0
        
        # Clean up old entries
        self._ip_request_history[ip_address] = [
            ts for ts in self._ip_request_history[ip_address]
            if now - ts < timedelta(hours=1)
        ]
        
        # Add current request
        self._ip_request_history[ip_address].append(now)
        
        # Check per-minute rate
        recent_minute = [
            ts for ts in self._ip_request_history[ip_address]
            if now - ts < timedelta(minutes=1)
        ]
        
        if len(recent_minute) > self.config.rate_limit_per_minute:
            reasons.append(f"rate_limit_exceeded_per_minute:{len(recent_minute)}")
            score += 1.0
        
        # Check per-hour rate
        if len(self._ip_request_history[ip_address]) > self.config.rate_limit_per_hour:
            reasons.append(f"rate_limit_exceeded_per_hour:{len(self._ip_request_history[ip_address])}")
            score += 0.8
        
        # Check for rapid succession
        if len(recent_minute) >= 2:
            time_diffs = [
                (recent_minute[i] - recent_minute[i-1]).total_seconds() * 1000
                for i in range(1, len(recent_minute))
            ]
            if any(diff < self.config.min_time_between_requests_ms for diff in time_diffs):
                reasons.append("requests_too_rapid")
                score += 0.6
        
        return (score, reasons)
    
    def _analyze_headers(self, headers: Dict[str, str]) -> Tuple[float, list[str]]:
        """Analyze HTTP headers for bot indicators"""
        reasons = []
        score = 0.0
        
        # Normalize header keys to lowercase
        headers_lower = {k.lower(): v for k, v in headers.items()}
        
        # Check for missing common headers
        common_headers = ['accept', 'accept-language', 'accept-encoding']
        missing_headers = [h for h in common_headers if h not in headers_lower]
        
        if len(missing_headers) >= 2:
            reasons.append(f"missing_common_headers:{','.join(missing_headers)}")
            score += 0.4
        
        # Check for automation indicators
        if 'x-automated' in headers_lower or 'x-bot' in headers_lower:
            reasons.append("automation_header_present")
            score += 1.0
        
        # Check Accept header
        accept = headers_lower.get('accept', '')
        if accept == '*/*' or not accept:
            reasons.append("suspicious_accept_header")
            score += 0.2
        
        # Check for unusual header order (advanced bots)
        if 'x-forwarded-for' in headers_lower and 'x-real-ip' not in headers_lower:
            # Might be proxied bot traffic
            reasons.append("proxy_without_real_ip")
            score += 0.1
        
        return (score, reasons)
    
    def _analyze_fingerprint(self, fingerprint: str) -> Tuple[float, list[str]]:
        """Analyze device fingerprint for anomalies"""
        reasons = []
        score = 0.0
        
        # Track fingerprint frequency
        now = datetime.utcnow()
        self._fingerprint_history[fingerprint] = [
            ts for ts in self._fingerprint_history[fingerprint]
            if now - ts < timedelta(hours=24)
        ]
        self._fingerprint_history[fingerprint].append(now)
        
        # Check if same fingerprint used too frequently
        usage_count = len(self._fingerprint_history[fingerprint])
        if usage_count > 1000:  # More than 1000 requests in 24h from same fingerprint
            reasons.append(f"fingerprint_overused:{usage_count}")
            score += 1.0
        elif usage_count > 500:
            reasons.append(f"fingerprint_suspicious_usage:{usage_count}")
            score += 0.5
        
        # Check fingerprint characteristics
        if len(fingerprint) < 16:
            reasons.append("weak_fingerprint")
            score += 0.3
        
        return (score, reasons)
    
    def _log_to_appinsights(
        self,
        result: BotDetectionResult,
        user_agent: Optional[str],
        ip_address: Optional[str]
    ):
        """Log detection results to Azure Application Insights"""
        try:
            from opencensus.ext.azure.log_exporter import AzureLogHandler
            import logging
            
            # This would use the connection string from environment
            # For production, configure APPLICATIONINSIGHTS_CONNECTION_STRING
            logger = logging.getLogger(__name__)
            
            log_data = {
                'custom_dimensions': {
                    'is_bot': result.is_bot,
                    'confidence': result.confidence_score,
                    'reasons': ','.join(result.reasons),
                    'user_agent': user_agent or 'None',
                    'ip_address': ip_address or 'None',
                    'event_type': 'bot_detection'
                }
            }
            
            if result.is_bot:
                logger.warning("Bot detected", extra=log_data)
            else:
                logger.info("Legitimate traffic", extra=log_data)
                
        except ImportError:
            # opencensus not available, skip logging
            pass
        except Exception as e:
            # Don't fail detection if logging fails
            print(f"Failed to log to App Insights: {e}")
    
    def clear_history(self):
        """Clear rate limiting history (for testing)"""
        self._ip_request_history.clear()
        self._fingerprint_history.clear()


def create_device_fingerprint(
    user_agent: str,
    screen_resolution: Optional[str] = None,
    timezone_offset: Optional[int] = None,
    plugins: Optional[list[str]] = None
) -> str:
    """
    Create a device fingerprint hash
    
    Args:
        user_agent: Browser user agent
        screen_resolution: Screen resolution (e.g., "1920x1080")
        timezone_offset: Timezone offset in minutes
        plugins: List of browser plugins
        
    Returns:
        SHA256 hash of combined fingerprint
    """
    fingerprint_components = [
        user_agent or '',
        screen_resolution or '',
        str(timezone_offset or ''),
        ','.join(plugins or [])
    ]
    
    combined = '|'.join(fingerprint_components)
    return hashlib.sha256(combined.encode()).hexdigest()


# Example usage
if __name__ == "__main__":
    detector = BotDetector()
    
    # Example 1: Legitimate browser
    result1 = detector.detect(
        user_agent="Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Chrome/91.0",
        ip_address="192.168.1.100",
        headers={
            "Accept": "text/html,application/xhtml+xml",
            "Accept-Language": "en-US,en;q=0.9",
            "Accept-Encoding": "gzip, deflate"
        }
    )
    print(f"Legitimate browser: {result1.to_dict()}")
    
    # Example 2: Bot with curl
    result2 = detector.detect(
        user_agent="curl/7.64.1",
        ip_address="203.0.113.1",
        headers={"Accept": "*/*"}
    )
    print(f"Bot (curl): {result2.to_dict()}")
    
    # Example 3: Rate limit violation
    for _ in range(150):
        result3 = detector.detect(
            user_agent="Mozilla/5.0",
            ip_address="203.0.113.2"
        )
    print(f"Rate limit bot: {result3.to_dict()}")
