#!/bin/bash

# AdImpact Os - Docker Quick Start Script

set -e

echo -e "\033[1;36m=== Ad Tracking JiangXi - Docker Startup ===\033[0m"
echo ""

# Check if Docker is running
echo -e "\033[1;33mChecking Docker status...\033[0m"
if ! docker info > /dev/null 2>&1; then
    echo -e "\033[1;31mERROR: Docker is not running!\033[0m"
    echo -e "\033[1;31mPlease start Docker and try again.\033[0m"
    exit 1
fi
echo -e "\033[1;32m? Docker is running\033[0m"

# Ask user which stack to start
echo ""
echo -e "\033[1;36mSelect deployment option:\033[0m"
echo -e "\033[1;37m1. Development Stack (Lightweight - Recommended)\033[0m"
echo -e "\033[0;37m   - Cosmos DB, Kafka, Azurite, Panelist API\033[0m"
echo -e "\033[0;37m   - ~4GB RAM required\033[0m"
echo ""
echo -e "\033[1;37m2. Full Stack (Heavy)\033[0m"
echo -e "\033[0;37m   - All above + Spark cluster + Azure Functions + Event Consumer\033[0m"
echo -e "\033[0;37m   - ~8GB RAM required\033[0m"
echo ""
read -p "Enter choice (1 or 2): " choice

COMPOSE_FILE=""
if [ "$choice" == "1" ]; then
    COMPOSE_FILE="docker-compose.dev.yml"
    echo -e "\033[1;36mStarting Development Stack...\033[0m"
elif [ "$choice" == "2" ]; then
    COMPOSE_FILE="docker-compose.yml"
    echo -e "\033[1;36mStarting Full Stack...\033[0m"
else
    echo -e "\033[1;31mInvalid choice. Exiting.\033[0m"
    exit 1
fi

# Stop any existing containers
echo ""
echo -e "\033[1;33mStopping existing containers...\033[0m"
docker-compose -f $COMPOSE_FILE down > /dev/null 2>&1

# Start services
echo -e "\033[1;33mStarting services (this may take a few minutes)...\033[0m"
docker-compose -f $COMPOSE_FILE up -d

if [ $? -ne 0 ]; then
    echo ""
    echo -e "\033[1;31mERROR: Failed to start services!\033[0m"
    echo -e "\033[1;31mCheck Docker logs for details.\033[0m"
    exit 1
fi

# Wait for services to be healthy
echo ""
echo -e "\033[1;33mWaiting for services to be ready...\033[0m"
echo -e "\033[0;37mThis can take 2-3 minutes for first-time startup...\033[0m"
sleep 15

# Check service status
echo ""
echo -e "\033[1;36mService Status:\033[0m"
docker-compose -f $COMPOSE_FILE ps

# Test if Panelist API is responding
echo ""
echo -e "\033[1;33mTesting Panelist API...\033[0m"
retries=0
max_retries=30
api_ready=false

while [ $retries -lt $max_retries ] && [ "$api_ready" = false ]; do
    if curl -s -o /dev/null -w "%{http_code}" http://localhost:5001/health | grep -q "200"; then
        api_ready=true
        echo -e "\033[1;32m? Panelist API is responding\033[0m"
    else
        retries=$((retries + 1))
        if [ $retries -lt $max_retries ]; then
            echo -e "\033[0;37m  Waiting for API... ($retries/$max_retries)\033[0m"
            sleep 2
        fi
    fi
done

if [ "$api_ready" = false ]; then
    echo -e "\033[1;33m? API took longer than expected to start. It may still be initializing.\033[0m"
    echo -e "\033[0;37m  Check logs: docker-compose -f $COMPOSE_FILE logs panelist-api\033[0m"
fi

# Display endpoints
echo ""
echo -e "\033[1;36m=== Service Endpoints ===\033[0m"
echo ""
echo -e "\033[0;36m--- Infrastructure ---\033[0m"
echo -e "\033[1;37mCosmos DB Explorer:  https://localhost:8081/_explorer/index.html\033[0m"
echo -e "\033[1;37mKafka (EventHub):    localhost:29092\033[0m"
echo -e "\033[1;37mAzurite Storage:     http://localhost:10000 (Blob) / :10001 (Queue) / :10002 (Table)\033[0m"
echo ""
echo -e "\033[0;36m--- Microservice APIs ---\033[0m"
echo -e "\033[1;37mPanelist API:        http://localhost:5001          Swagger: http://localhost:5001/swagger\033[0m"
echo -e "\033[1;37mSurvey API:          http://localhost:5002          Swagger: http://localhost:5002/swagger\033[0m"
echo -e "\033[1;37mCampaign API:        http://localhost:5003          Swagger: http://localhost:5003/swagger\033[0m"
echo ""
echo -e "\033[0;36m--- Azure Functions (Ad Tracking) ---\033[0m"
echo -e "\033[1;37mPixel Tracker:       http://localhost:7071/api/pixel?cid=CAMPAIGN&crid=CREATIVE&uid=USER\033[0m"
echo -e "\033[1;37mS2S Tracker:         http://localhost:7071/api/s2s/track  (POST)\033[0m"
echo ""
echo -e "\033[0;36m--- Web UIs ---\033[0m"
echo -e "\033[1;37mDashboard:           http://localhost:5004\033[0m"
echo -e "\033[1;37mDemo UI (External):  http://localhost:5010\033[0m"
echo -e "\033[1;37mSurvey Take Page:    http://localhost:5002/survey/take/{token}  (generated via Trigger)\033[0m"
echo ""

# Display test commands
echo -e "\033[1;36m=== Quick Test Commands ===\033[0m"
echo -e "\033[1;33mGet All Panelists:\033[0m"
echo -e "\033[0;37m  curl http://localhost:5001/api/panelists\033[0m"
echo ""
echo -e "\033[1;33mGet All Campaigns:\033[0m"
echo -e "\033[0;37m  curl http://localhost:5003/api/campaigns\033[0m"
echo ""
echo -e "\033[1;33mGet All Surveys:\033[0m"
echo -e "\033[0;37m  curl http://localhost:5002/api/surveys\033[0m"
echo ""
echo -e "\033[1;33mTest Pixel Tracker:\033[0m"
echo -e '\033[0;37m  curl "http://localhost:7071/api/pixel?cid=campaign_summer_beverage_2024&crid=creative_banner_728x90_v1&uid=panelist_001"\033[0m'
echo ""
echo -e "\033[1;33mCreate Panelist:\033[0m"
echo -e '\033[0;37m  curl -X POST http://localhost:5001/api/panelists -H "Content-Type: application/json" -d "{\"email\":\"test@example.com\",\"firstName\":\"John\",\"lastName\":\"Doe\",\"age\":30,\"consentGdpr\":true}"\033[0m'
echo ""

echo -e "\033[1;33mView Logs:\033[0m"
echo -e "\033[0;37m  docker-compose -f $COMPOSE_FILE logs -f\033[0m"
echo ""
echo -e "\033[1;33mStop Services:\033[0m"
echo -e "\033[0;37m  docker-compose -f $COMPOSE_FILE down\033[0m"
echo ""

echo -e "\033[1;32m=== Startup Complete ===\033[0m"
echo ""
echo -e "\033[1;36mNext Steps:\033[0m"
echo -e "\033[1;37m1. Open Dashboard:     http://localhost:5004\033[0m"
echo -e "\033[1;37m2. Open Demo UI:       http://localhost:5010\033[0m"
echo -e "\033[1;37m3. Explore Swagger:    http://localhost:5001/swagger\033[0m"
echo -e "\033[1;37m4. View logs:          docker-compose -f $COMPOSE_FILE logs -f\033[0m"
echo ""
echo -e "\033[0;37mPress Ctrl+C to exit (services will keep running)\033[0m"
echo ""

# Optionally follow logs
read -p "Follow logs now? (y/n): " follow_logs
if [ "$follow_logs" == "y" ] || [ "$follow_logs" == "Y" ]; then
    docker-compose -f $COMPOSE_FILE logs -f
fi
