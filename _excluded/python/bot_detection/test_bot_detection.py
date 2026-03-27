"""
Unit tests for bot detection module
Run with: pytest test_bot_detection.py -v
"""

import pytest
from datetime import datetime, timedelta
from detector import BotDetector, BotDetectionConfig, create_device_fingerprint


@pytest.fixture
def detector():
    """Create a bot detector instance for testing"""
    config = BotDetectionConfig(
        rate_limit_per_minute=10,
        rate_limit_per_hour=100,
        enable_appinsights_logging=False
    )
    return BotDetector(config)


class TestUserAgentAnalysis:
    """Test User-Agent based detection"""
    
    def test_legitimate_browser(self, detector):
        result = detector.detect(
            user_agent="Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Chrome/91.0",
            ip_address="192.168.1.1"
        )
        assert not result.is_bot
        assert result.confidence_score < 0.5
    
    def test_curl_detected(self, detector):
        result = detector.detect(
            user_agent="curl/7.64.1",
            ip_address="192.168.1.1"
        )
        assert result.is_bot
        assert "bot_pattern_in_ua" in result.reasons
    
    def test_bot_keyword(self, detector):
        result = detector.detect(
            user_agent="MyBot/1.0 (bot crawler)",
            ip_address="192.168.1.1"
        )
        assert result.is_bot
        assert "bot_pattern_in_ua" in result.reasons
    
    def test_missing_user_agent(self, detector):
        result = detector.detect(
            user_agent=None,
            ip_address="192.168.1.1"
        )
        assert result.is_bot
        assert "missing_user_agent" in result.reasons
    
    def test_unknown_user_agent(self, detector):
        result = detector.detect(
            user_agent="Unknown",
            ip_address="192.168.1.1"
        )
        assert result.is_bot or result.confidence_score > 0.5
        assert "unknown_user_agent" in result.reasons


class TestRateLimiting:
    """Test IP-based rate limiting"""
    
    def test_within_rate_limit(self, detector):
        for _ in range(5):
            result = detector.detect(
                user_agent="Mozilla/5.0",
                ip_address="192.168.1.100"
            )
        assert not result.is_bot
    
    def test_exceeds_per_minute_limit(self, detector):
        # Exceed per-minute limit
        for _ in range(12):
            result = detector.detect(
                user_agent="Mozilla/5.0",
                ip_address="192.168.1.101"
            )
        
        assert result.is_bot
        assert any("rate_limit_exceeded_per_minute" in reason for reason in result.reasons)
    
    def test_different_ips_not_limited(self, detector):
        for i in range(5):
            result = detector.detect(
                user_agent="Mozilla/5.0",
                ip_address=f"192.168.1.{i}"
            )
            assert not result.is_bot


class TestHeaderAnalysis:
    """Test HTTP header analysis"""
    
    def test_normal_headers(self, detector):
        result = detector.detect(
            user_agent="Mozilla/5.0",
            ip_address="192.168.1.1",
            headers={
                "Accept": "text/html,application/xhtml+xml",
                "Accept-Language": "en-US,en;q=0.9",
                "Accept-Encoding": "gzip, deflate"
            }
        )
        assert result.confidence_score < 0.7
    
    def test_missing_headers(self, detector):
        result = detector.detect(
            user_agent="Mozilla/5.0",
            ip_address="192.168.1.1",
            headers={
                "Accept": "*/*"
            }
        )
        assert result.confidence_score > 0.2
        assert any("missing_common_headers" in reason for reason in result.reasons)
    
    def test_automation_header(self, detector):
        result = detector.detect(
            user_agent="Mozilla/5.0",
            ip_address="192.168.1.1",
            headers={
                "X-Automated": "true",
                "Accept": "text/html"
            }
        )
        assert result.is_bot
        assert "automation_header_present" in result.reasons


class TestFingerprintAnalysis:
    """Test device fingerprint analysis"""
    
    def test_normal_fingerprint(self, detector):
        fp = create_device_fingerprint(
            user_agent="Mozilla/5.0",
            screen_resolution="1920x1080",
            timezone_offset=-300
        )
        result = detector.detect(
            user_agent="Mozilla/5.0",
            ip_address="192.168.1.1",
            fingerprint=fp
        )
        assert not result.is_bot
    
    def test_overused_fingerprint(self, detector):
        fp = "test_fingerprint_12345"
        
        # Simulate heavy usage
        for _ in range(1100):
            result = detector.detect(
                user_agent="Mozilla/5.0",
                ip_address=f"192.168.1.{(_ % 255)}",  # Vary IP to avoid rate limit
                fingerprint=fp
            )
        
        assert result.is_bot
        assert any("fingerprint_overused" in reason for reason in result.reasons)
    
    def test_weak_fingerprint(self, detector):
        result = detector.detect(
            user_agent="Mozilla/5.0",
            ip_address="192.168.1.1",
            fingerprint="short"
        )
        assert any("weak_fingerprint" in reason for reason in result.reasons)


class TestDeviceFingerprintGeneration:
    """Test fingerprint generation utility"""
    
    def test_consistent_fingerprint(self):
        fp1 = create_device_fingerprint(
            user_agent="Mozilla/5.0",
            screen_resolution="1920x1080",
            timezone_offset=-300
        )
        fp2 = create_device_fingerprint(
            user_agent="Mozilla/5.0",
            screen_resolution="1920x1080",
            timezone_offset=-300
        )
        assert fp1 == fp2
    
    def test_different_inputs_different_fingerprints(self):
        fp1 = create_device_fingerprint(
            user_agent="Mozilla/5.0",
            screen_resolution="1920x1080"
        )
        fp2 = create_device_fingerprint(
            user_agent="Mozilla/5.0",
            screen_resolution="1280x720"
        )
        assert fp1 != fp2
    
    def test_fingerprint_length(self):
        fp = create_device_fingerprint(
            user_agent="Mozilla/5.0",
            screen_resolution="1920x1080",
            timezone_offset=-300,
            plugins=["PDF", "Flash"]
        )
        assert len(fp) == 64  # SHA256 hex digest


class TestEdgeCases:
    """Test edge cases and error handling"""
    
    def test_all_none_inputs(self, detector):
        result = detector.detect(
            user_agent=None,
            ip_address=None,
            headers=None,
            fingerprint=None
        )
        assert result.is_bot  # Missing UA alone should trigger bot detection
    
    def test_empty_strings(self, detector):
        result = detector.detect(
            user_agent="",
            ip_address="",
            headers={},
            fingerprint=""
        )
        assert result.is_bot
    
    def test_clear_history(self, detector):
        # Generate some history
        for _ in range(5):
            detector.detect(user_agent="Mozilla/5.0", ip_address="192.168.1.1")
        
        # Clear history
        detector.clear_history()
        
        # Verify history is cleared
        assert len(detector._ip_request_history) == 0
        assert len(detector._fingerprint_history) == 0


class TestConfiguration:
    """Test configuration options"""
    
    def test_custom_thresholds(self):
        config = BotDetectionConfig(
            rate_limit_per_minute=5,
            fingerprint_score_threshold=0.5
        )
        detector = BotDetector(config)
        
        # Should trigger at 6 requests
        for _ in range(6):
            result = detector.detect(
                user_agent="Mozilla/5.0",
                ip_address="192.168.1.1"
            )
        
        assert result.is_bot
    
    def test_logging_disabled(self):
        config = BotDetectionConfig(enable_appinsights_logging=False)
        detector = BotDetector(config)
        
        # Should not raise exception even without App Insights
        result = detector.detect(
            user_agent="curl/7.0",
            ip_address="192.168.1.1"
        )
        assert result.is_bot


# Integration test scenarios
class TestRealWorldScenarios:
    """Test real-world bot scenarios"""
    
    def test_legitimate_user_session(self, detector):
        """Simulate normal user browsing"""
        results = []
        for page in range(5):
            result = detector.detect(
                user_agent="Mozilla/5.0 (Windows NT 10.0; Win64; x64) Chrome/91.0",
                ip_address="203.0.113.100",
                headers={
                    "Accept": "text/html",
                    "Accept-Language": "en-US",
                    "Accept-Encoding": "gzip"
                }
            )
            results.append(result)
        
        # All should be legitimate
        assert all(not r.is_bot for r in results)
    
    def test_aggressive_scraper(self, detector):
        """Simulate aggressive scraping bot"""
        results = []
        for _ in range(20):
            result = detector.detect(
                user_agent="Python-urllib/3.8",
                ip_address="203.0.113.200",
                headers={"Accept": "*/*"}
            )
            results.append(result)
        
        # Should be detected as bot
        assert any(r.is_bot for r in results)
    
    def test_sophisticated_bot(self, detector):
        """Simulate bot with realistic UA but suspicious behavior"""
        results = []
        for _ in range(50):
            result = detector.detect(
                user_agent="Mozilla/5.0 (Windows NT 10.0; Win64; x64) Chrome/91.0",
                ip_address="203.0.113.300",
                headers={
                    "Accept": "text/html",
                    "Accept-Language": "en-US"
                }
            )
            results.append(result)
        
        # Should eventually be caught by rate limiting
        assert results[-1].is_bot


if __name__ == "__main__":
    pytest.main([__file__, "-v", "--tb=short"])
