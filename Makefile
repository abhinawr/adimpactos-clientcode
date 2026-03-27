.PHONY: help dev full build up down restart logs clean test health

# Default target
help:
	@echo "Ad Tracking JiangXi - Docker Commands"
	@echo "======================================"
	@echo ""
	@echo "Setup Commands:"
	@echo "  make dev        - Start development stack (Cosmos, Kafka, Azurite, Panelist API)"
	@echo "  make full       - Start full stack (all services including Spark)"
	@echo "  make build      - Build all Docker images"
	@echo ""
	@echo "Control Commands:"
	@echo "  make up         - Start services (use with dev or full)"
	@echo "  make down       - Stop all services"
	@echo "  make restart    - Restart all services"
	@echo "  make logs       - View logs from all services"
	@echo ""
	@echo "Maintenance:"
	@echo "  make clean      - Stop services and remove volumes"
	@echo "  make test       - Run quick API tests"
	@echo "  make health     - Check service health"
	@echo ""
	@echo "Individual Services:"
	@echo "  make logs-api   - View Panelist API logs"
	@echo "  make logs-kafka - View Kafka logs"
	@echo "  make shell-api  - Open bash in Panelist API container"

# Development stack (lightweight)
dev:
	@echo "Starting development stack..."
	docker-compose -f docker-compose.dev.yml up -d
	@echo ""
	@echo "Services started! Endpoints:"
	@echo "  Panelist API: http://localhost:5001"
	@echo "  Swagger:      http://localhost:5001/swagger"
	@echo "  Cosmos DB:    https://localhost:8081/_explorer/index.html"

# Full stack
full:
	@echo "Starting full stack..."
	docker-compose up -d
	@echo ""
	@echo "All services started! Endpoints:"
	@echo "  Panelist API:     http://localhost:5001"
	@echo "  Incentives API:   http://localhost:5002"
	@echo "  Azure Functions:  http://localhost:7071"
	@echo "  Spark Master:     http://localhost:8080"
	@echo "  Cosmos DB:        https://localhost:8081/_explorer/index.html"

# Build all images
build:
	@echo "Building Docker images..."
	docker-compose build --parallel

# Start services (default to dev)
up:
	docker-compose -f docker-compose.dev.yml up -d

# Stop services
down:
	@echo "Stopping all services..."
	docker-compose down
	docker-compose -f docker-compose.dev.yml down

# Restart services
restart:
	@echo "Restarting services..."
	docker-compose restart

# View logs
logs:
	docker-compose logs -f

# Clean everything
clean:
	@echo "Cleaning up Docker resources..."
	docker-compose down -v
	docker-compose -f docker-compose.dev.yml down -v
	@echo "Cleanup complete!"

# Run API tests
test:
	@echo "Testing Panelist API..."
	@curl -s http://localhost:5001/health || echo "API not responding"
	@echo ""
	@echo "Testing Cosmos DB..."
	@curl -k -s https://localhost:8081/_explorer/emulator.pem > /dev/null && echo "Cosmos DB: OK" || echo "Cosmos DB: Not Ready"

# Check health
health:
	@echo "Service Health Status:"
	@echo "======================"
	@docker ps --format "table {{.Names}}\t{{.Status}}"

# Individual service logs
logs-api:
	docker-compose logs -f panelist-api

logs-kafka:
	docker-compose logs -f eventhub

logs-cosmos:
	docker-compose logs -f cosmosdb

# Shell access
shell-api:
	docker exec -it adtracking-panelist-api /bin/bash

shell-kafka:
	docker exec -it adtracking-eventhub /bin/bash

# Database operations
db-reset:
	@echo "Resetting database..."
	docker-compose down -v
	docker volume rm adimpactos_cosmosdb-data || true
	docker-compose up -d cosmosdb

# Show endpoints
endpoints:
	@echo "Service Endpoints:"
	@echo "=================="
	@echo "Panelist API:       http://localhost:5001"
	@echo "Swagger UI:         http://localhost:5001/swagger"
	@echo "Incentives API:     http://localhost:5002"
	@echo "Azure Functions:    http://localhost:7071"
	@echo "Cosmos DB Explorer: https://localhost:8081/_explorer/index.html"
	@echo "Spark Master UI:    http://localhost:8080"
	@echo "Azurite (Storage):  http://localhost:10000"

# Rebuild and restart specific service
rebuild-api:
	docker-compose build panelist-api
	docker-compose up -d panelist-api

# Quick start for new developers
quickstart: dev
	@echo ""
	@echo "Waiting for services to start..."
	@sleep 15
	@make test
	@make endpoints
	@echo ""
	@echo "Quick start complete! Open http://localhost:5001/swagger to get started."
