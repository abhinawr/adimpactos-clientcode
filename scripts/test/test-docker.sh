#!/bin/bash

# AdImpactOs - Docker Test Script
# This script tests the containerized environment

set -e

echo "=================================="
echo "AdImpactOs - Docker Tests"
echo "=================================="
echo ""

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Function to check if a service is running
check_service() {
    local service_name=$1
    if docker ps --format '{{.Names}}' | grep -q "$service_name"; then
        echo -e "${GREEN}?${NC} $service_name is running"
        return 0
    else
        echo -e "${RED}?${NC} $service_name is NOT running"
        return 1
    fi
}

# Function to check if a URL is accessible
check_url() {
    local url=$1
    local description=$2
    local expected_status=${3:-200}
    
    if curl -s -o /dev/null -w "%{http_code}" "$url" | grep -q "$expected_status"; then
        echo -e "${GREEN}?${NC} $description ($url)"
        return 0
    else
        echo -e "${RED}?${NC} $description ($url) - Not responding"
        return 1
    fi
}

# Check if Docker is running
echo "Checking Docker..."
if ! docker info > /dev/null 2>&1; then
    echo -e "${RED}?${NC} Docker is not running. Please start Docker Desktop."
    exit 1
fi
echo -e "${GREEN}?${NC} Docker is running"
echo ""

# Check running containers
echo "Checking Services..."
check_service "adimpactos-cosmosdb"
check_service "adimpactos-zookeeper"
check_service "adimpactos-eventhub"
check_service "adimpactos-azurite"
check_service "adimpactos-panelist-api"
echo ""

# Test endpoints
echo "Testing Endpoints..."
sleep 2  # Give services time to be ready

# Test Panelist API
if check_url "http://localhost:5001/health" "Panelist API Health"; then
    echo -e "${YELLOW}  Testing API endpoints...${NC}"
    
    # Test GET all panelists
    if curl -s "http://localhost:5001/api/panelists" > /dev/null; then
        echo -e "${GREEN}  ?${NC} GET /api/panelists works"
    fi
    
    # Test Swagger
    if check_url "http://localhost:5001/swagger" "Swagger UI"; then
        echo -e "${GREEN}  ?${NC} Swagger documentation is available"
    fi
fi
echo ""

# Test Cosmos DB Emulator
echo "Testing Cosmos DB..."
if curl -k -s -o /dev/null "https://localhost:8081/_explorer/emulator.pem"; then
    echo -e "${GREEN}?${NC} Cosmos DB Emulator is accessible"
else
    echo -e "${RED}?${NC} Cosmos DB Emulator is not accessible"
fi
echo ""

# Test Azurite
echo "Testing Azurite (Storage Emulator)..."
if curl -s -o /dev/null "http://localhost:10000/devstoreaccount1?comp=list"; then
    echo -e "${GREEN}?${NC} Azurite Blob Service is accessible"
else
    echo -e "${RED}?${NC} Azurite is not accessible"
fi
echo ""

# Check if full stack is running
echo "Checking Full Stack Services (if running)..."
if docker ps --format '{{.Names}}' | grep -q "adimpactos-functions"; then
    check_service "adimpactos-functions"
    check_service "adimpactos-incentives-api"
    check_service "adimpactos-event-consumer"
    check_service "adimpactos-spark-master"
    
    # Test additional endpoints
    echo ""
    echo "Testing Full Stack Endpoints..."
    check_url "http://localhost:7071/admin/host/status" "Azure Functions" "200\|401"
    check_url "http://localhost:5002/health" "Incentives API"
    check_url "http://localhost:8080" "Spark Master UI"
else
    echo -e "${YELLOW}?${NC} Full stack not running (development stack detected)"
fi
echo ""

# Test API with sample data
echo "Testing API with Sample Data..."
echo -e "${YELLOW}Creating test panelist...${NC}"

response=$(curl -s -X POST http://localhost:5001/api/panelists \
  -H "Content-Type: application/json" \
  -d '{
    "email": "dockertest@example.com",
    "firstName": "Docker",
    "lastName": "Test",
    "age": 30,
    "gender": "M",
    "consentGdpr": true,
    "consentCcpa": true
  }')

if echo "$response" | grep -q "id\|email"; then
    echo -e "${GREEN}?${NC} Successfully created test panelist"
    panelist_id=$(echo "$response" | grep -o '"id":"[^"]*"' | cut -d'"' -f4)
    echo -e "${YELLOW}  Panelist ID: $panelist_id${NC}"
    
    # Test GET by ID
    if [ ! -z "$panelist_id" ]; then
        echo -e "${YELLOW}  Retrieving panelist by ID...${NC}"
        if curl -s "http://localhost:5001/api/panelists/$panelist_id" | grep -q "$panelist_id"; then
            echo -e "${GREEN}  ?${NC} Successfully retrieved panelist"
        fi
    fi
else
    echo -e "${RED}?${NC} Failed to create test panelist"
    echo -e "${YELLOW}Response: $response${NC}"
fi
echo ""

# Docker stats
echo "Docker Resource Usage..."
docker stats --no-stream --format "table {{.Name}}\t{{.CPUPerc}}\t{{.MemUsage}}" | head -n 10
echo ""

# Summary
echo "=================================="
echo "Test Summary"
echo "=================================="
echo ""
echo "Service Endpoints:"
echo "  Panelist API:       http://localhost:5001"
echo "  Swagger UI:         http://localhost:5001/swagger"
echo "  Cosmos DB Explorer: https://localhost:8081/_explorer/index.html"
echo "  Azurite (Blob):     http://localhost:10000"
echo ""

if docker ps --format '{{.Names}}' | grep -q "adimpactos-functions"; then
    echo "Full Stack Endpoints:"
    echo "  Azure Functions:    http://localhost:7071"
    echo "  Incentives API:     http://localhost:5002"
    echo "  Spark Master UI:    http://localhost:8080"
    echo ""
fi

echo "Next Steps:"
echo "  - Open Swagger UI to explore API: http://localhost:5001/swagger"
echo "  - View logs: docker-compose logs -f"
echo "  - Stop services: docker-compose down"
echo ""

echo -e "${GREEN}? Tests completed!${NC}"
