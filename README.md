# AdImpactOs - Ad Measurement Platform

A cloud-native, privacy-first ad measurement platform for capturing, processing, and analyzing ad impression data at scale using Azure services.

## 🎯 Overview

AdImpactOs enables advertisers to measure campaign effectiveness through:

- Real-time ad impression tracking (pixel & server-to-server)
- Campaign management with impression analytics
- Panelist profile management with consent tracking
- Survey distribution and response collection for brand lift measurement
- Statistical analysis (Propensity Score Matching & Lift Calculation)
- Unified Dashboard (BFF) with Reports, Brand Lift Analysis, and Demographics
- External Demo UI for demonstrating tracker API integration
- Privacy-compliant data handling (GDPR, CCPA)

## 🚀 Quick Start

Get started in 5 minutes with Docker:

```powershell
# Windows
.\start-docker.ps1

# Linux/Mac
chmod +x start-docker.sh
./start-docker.sh
```

**Then access:**

| Service                | URL                               |
| ---------------------- | --------------------------------- |
| **Dashboard**          | http://localhost:5004             |
| **Demo UI (External)** | http://localhost:5010             |
| **Panelist API**       | http://localhost:5001/swagger     |
| **Survey API**         | http://localhost:5002/swagger     |
| **Campaign API**       | http://localhost:5003/swagger     |
| **Azure Functions**    | http://localhost:7071             |
| **Cosmos DB**          | https://localhost:8081/\_explorer |

**Full Documentation:** See **[docs/](docs/)** folder

## 📖 Documentation

| Document                                                                | Description                                               |
| ----------------------------------------------------------------------- | --------------------------------------------------------- |
| **[Quick Start](docs/setup/QUICK-START.md)**                            | Get running in 5 minutes                                  |
| **[Docker Guide](docs/setup/DOCKER-GUIDE.md)**                          | Complete Docker setup                                     |
| **[Deployment Guide](docs/setup/DEPLOYMENT-GUIDE.md)**                  | Azure deployment                                          |
| **[CI/CD & Repository Guide](docs/setup/CICD-AND-REPOSITORY-GUIDE.md)** | Branching, PR workflow, GitHub Actions pipelines, secrets |
| **[Solution Architecture](docs/architecture/SOLUTION-ARCHITECTURE.md)** | System design & architecture                              |
| **[Azure Functions](docs/components/AZURE-FUNCTIONS.md)**               | Pixel & S2S tracking                                      |
| **[Survey Service](docs/components/SURVEY-SERVICE.md)**                 | Brand lift surveys                                        |
| **[Documentation Index](docs/README.md)**                               | Complete documentation index                              |

## 🏗️ Architecture

### Core Components

1. **Azure Functions** - High-throughput impression tracking
   - Pixel Tracker: Browser-based 1x1 GIF tracking
   - S2S Tracker: Server-to-server JSON API

2. **Campaign API** - Campaign and impression management
   - Campaign CRUD operations
   - Impression storage and querying
   - Per-campaign impression summaries (total, bot rate, device/country breakdown)

3. **Panelist API** - Profile and consent management
   - CRUD operations for panelist data
   - Consent tracking (GDPR/CCPA)
   - Demographics and attributes

4. **Survey API** - Brand lift measurement
   - Survey creation and distribution
   - Response collection (exposed/control cohorts)
   - Automated lift calculation
   - Token-based panelist survey links with self-service web form
   - Integration with analytics pipeline

5. **Event Consumer** - Stream processing
   - Event Hub consumption
   - Data validation and enrichment
   - Bot detection
   - Writes validated impressions to Campaign API

6. **Dashboard** - Unified web UI (BFF pattern)
   - Overview with campaign, survey, panelist, and tracking stats
   - Campaign management with impression analytics
   - Survey management and response explorer
   - Panelist directory
   - Reports with brand lift analysis, demographics breakdown, and CSV export
   - Impression Tracking view with impression summaries

7. **Demo UI** - External client demonstration app
   - Pixel tracking demo with ad banner preview and HTML embed code
   - S2S tracking demo with payload builder and response viewer
   - Bulk impression simulation (configurable count, method, delay)
   - Full event log with session summary and CSV export

8. **Analytics** _(excluded from active solution — see [`_excluded/README.md`](_excluded/README.md))_
   - Azure Synapse/Fabric schemas for analytics warehouse
   - Databricks notebooks for PSM and lift calculations
   - Power BI report specification for visualization
   - Terraform/Bicep IaC for Azure infrastructure provisioning

## 📊 Project Structure

```
AdImpactOs/
├── src/
│   ├── AdImpactOs/              # Azure Functions (pixel & S2S tracking)
│   ├── AdImpactOs.PanelistAPI/  # Panelist management API
│   ├── AdImpactOs.Campaign/     # Campaign & impression management API
│   ├── AdImpactOs.Survey/       # Survey & brand lift API
│   ├── AdImpactOs.EventConsumer/# Event Hub stream processing
│   ├── AdImpactOs.Dashboard/    # Unified web UI (MVC + BFF)
│   └── AdImpactOs.DemoUI/       # External ad tracking demo app
├── tests/
│   ├── AdImpactOs.Tests/        # Azure Functions & integration tests
│   ├── AdImpactOs.PanelistAPI.Tests/
│   ├── AdImpactOs.Campaign.Tests/
│   └── AdImpactOs.Survey.Tests/
├── scripts/
│   ├── test/                           # Test scripts (Docker, E2E, etc.)
│   └── verify/                         # Verification scripts
├── docs/                               # 📖 Complete documentation
│   ├── setup/                          # Setup & Docker guides
│   ├── architecture/                   # Architecture docs
│   ├── components/                     # Component guides
│   ├── demo/                           # Demo data docs
│   └── planning/                       # Roadmap & planning
├── demo/                               # Demo scripts & sample data
├── _excluded/                          # ⚠️ Excluded from active solution (see _excluded/README.md)
│   ├── databricks/                     #   PSM & Lift analysis notebooks (PySpark)
│   ├── synapse/                        #   Data warehouse SQL schemas
│   ├── terraform/                      #   Infrastructure as Code (Azure)
│   ├── python/                         #   Standalone bot detection module
│   ├── powerbi/                        #   Power BI report specification
│   └── bicep/                          #   Azure Bicep IaC template
├── docker-compose.yml                  # Full stack orchestration
└── docker-compose.dev.yml              # Development stack orchestration
```

## 🔑 Key Features

### Impression Tracking

- **Pixel Tracking**: 1x1 transparent GIF for browser-based tracking
- **S2S API**: JSON endpoint for server-to-server integration
- **High Throughput**: Auto-scaling to handle 100k+ req/sec
- **Low Latency**: < 50ms pixel response time

### Panelist Management

- Demographics and profile data
- Consent management (GDPR/CCPA compliant)
- Pseudonymization for privacy
- CRUD API with Swagger docs

### Survey System

- Campaign-linked survey creation
- Multi-question types (rating, Likert scale, multiple choice)
- Cohort tracking (exposed vs control groups)
- Automated lift metric calculation
- Brand awareness, favorability, purchase intent measurement
- **Token-based panelist survey links** — trigger surveys and generate unique, signed URLs for panelists to take surveys via a self-service web form
- **Panelist survey-taking page** — standalone responsive UI served at `/survey/take/{token}` with question-by-question navigation, progress tracking, and automatic response time capture

### Analytics

- Propensity Score Matching for cohort creation
- Statistical lift calculation with confidence intervals
- Power BI dashboards for visualization
- Real-time operational metrics

## 🛠️ Development

### Prerequisites

- Docker Desktop (with 8GB+ RAM)
- .NET 8 SDK
- Azure Functions Core Tools v4 (optional)
- Visual Studio 2022 or VS Code

### Local Development

```bash
# Start infrastructure only
docker-compose up -d cosmosdb eventhub azurite

# Run APIs locally with hot reload
cd src/AdImpactOs.PanelistAPI
dotnet watch run

# In another terminal
cd src/AdImpactOs.Survey
dotnet watch run
```

### Testing

```bash
# Run all tests
dotnet test

# Run specific test project
dotnet test tests/AdImpactOs.Tests

# Run with coverage
dotnet test /p:CollectCoverage=true
```

### Build & Deploy

```bash
# Build solution
dotnet build

# Build Docker images
docker-compose build

# Deploy to Azure (requires Azure CLI)
az functionapp deployment source config-zip \
  --resource-group adimpactos-rg \
  --name adimpactos-functions \
  --src functions.zip
```

## 🧪 Testing

### Service Endpoints

| Service         | Port | Endpoint                          |
| --------------- | ---- | --------------------------------- |
| Panelist API    | 5001 | http://localhost:5001/swagger     |
| Survey API      | 5002 | http://localhost:5002/swagger     |
| Campaign API    | 5003 | http://localhost:5003/swagger     |
| Dashboard       | 5004 | http://localhost:5004             |
| Azure Functions | 7071 | http://localhost:7071/api/pixel   |
| Demo UI         | 5010 | http://localhost:5010             |
| Cosmos DB       | 8081 | https://localhost:8081/\_explorer |

### API Examples

**Create Panelist:**

```bash
curl -X POST http://localhost:5001/api/panelists \
  -H "Content-Type: application/json" \
  -d '{
    "email": "test@example.com",
    "firstName": "John",
    "lastName": "Doe",
    "age": 30,
    "consentGdpr": true
  }'
```

**Create Survey:**

```bash
curl -X POST http://localhost:5002/api/surveys \
  -H "Content-Type: application/json" \
  -d '{
    "campaignId": "summer2024",
    "surveyName": "Brand Lift Study",
    "questions": [
      {
        "questionText": "How familiar are you with our brand?",
        "questionType": "LikertScale",
        "metric": "brand_awareness",
        "options": ["Not at all", "Slightly", "Moderately", "Very", "Extremely"],
        "required": true,
        "order": 1
      }
    ]
  }'
```

## 🔒 Security & Privacy

- **Pseudonymization**: PII protected with SHA256 hashing
- **Consent Management**: Explicit GDPR/CCPA consent tracking
- **Encryption**: TLS 1.2+ for data in transit, AES-256 at rest
- **Access Control**: Azure AD B2C authentication with RBAC
- **Audit Logging**: All data access logged and monitored

## 🤝 Contributing

1. Fork the repository
2. Create a feature branch: `git checkout -b feature/my-feature`
3. Commit changes: `git commit -am 'Add feature'`
4. Push to branch: `git push origin feature/my-feature`
5. Submit a Pull Request

## 📝 License

Internal use only - Proprietary

## 🆘 Support

For issues and questions:

- Check **[Documentation](docs/)** for guides and troubleshooting
- Review [Docker Guide](docs/setup/DOCKER-GUIDE.md) troubleshooting section
- Contact the development team

## 🗺️ Roadmap

### Current (v1.0)

- ✅ Pixel & S2S tracking
- ✅ Campaign management with impression analytics
- ✅ Panelist management
- ✅ Survey system for brand lift
- ✅ Token-based survey links with panelist self-service response collection
- ✅ Event Consumer stream processing
- ✅ Unified Dashboard (Overview, Campaigns, Surveys, Panelists, Reports, Tracking)
- ✅ External Demo UI for tracker API demonstration
- ✅ Basic analytics pipeline
- ✅ Docker containerization (11 services)

### Next (v1.1)

- 🔄 Advanced bot detection with ML
- 🔄 Real-time dashboards with SignalR
- 🔄 Multi-tenant support
- 🔄 Mobile SDK

### Future (v2.0)

- 📅 Cross-platform measurement
- 📅 AI-powered insights
- 📅 Differential privacy
- 📅 Federated learning

---

**Built with**: .NET 8, Azure Functions, Cosmos DB, Event Hubs, Databricks, Power BI (excluded from this solution)

**Documentation**: See **[docs/](docs/)** folder for complete guides
