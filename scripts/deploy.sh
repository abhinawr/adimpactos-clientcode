#!/usr/bin/env bash
# ============================================================================
# AdImpactOs – Manual Build & Deploy Script
# Builds Docker images, pushes to ACR, updates App Services, deploys Functions.
#
# Prerequisites:
#   - Azure CLI installed and logged in
#   - Docker running
#   - Infrastructure already provisioned (run provision-azure.sh first)
#
# Usage:
#   ./scripts/deploy.sh
#   ./scripts/deploy.sh --prefix myapp --env dev
#   ./scripts/deploy.sh --skip-build    # Only deploy, skip Docker build
# ============================================================================

set -euo pipefail

# ── Defaults ──
PREFIX="${PREFIX:-adimpact}"
ENV="${ENV:-dev}"
SKIP_BUILD=false
TAG="${TAG:-latest}"

while [[ $# -gt 0 ]]; do
  case $1 in
    --prefix)     PREFIX="$2";     shift 2 ;;
    --env)        ENV="$2";        shift 2 ;;
    --tag)        TAG="$2";        shift 2 ;;
    --skip-build) SKIP_BUILD=true; shift ;;
    *) echo "Unknown argument: $1"; exit 1 ;;
  esac
done

# ── Derived names ──
RG="${PREFIX}-${ENV}-rg"
ACR_NAME="${PREFIX}${ENV}acr"
FUNC_APP="${PREFIX}-fn-${ENV}"

ACR_LOGIN_SERVER=$(az acr show --name "$ACR_NAME" --query loginServer --output tsv)

echo "==========================================="
echo "AdImpactOs – Build & Deploy"
echo "==========================================="
echo "ACR:       $ACR_LOGIN_SERVER"
echo "Tag:       $TAG"
echo "Skip build: $SKIP_BUILD"
echo "==========================================="

# ── Log in to ACR ──
echo ""
echo "▸ Logging in to ACR"
az acr login --name "$ACR_NAME"

# ── Build + Push Docker Images ──
if [ "$SKIP_BUILD" = false ]; then
  SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
  REPO_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"
  cd "$REPO_ROOT"

  declare -A SERVICES=(
    ["panelistapi"]="src/AdImpactOs.PanelistAPI/Dockerfile"
    ["campaignapi"]="src/AdImpactOs.Campaign/Dockerfile"
    ["surveyapi"]="src/AdImpactOs.Survey/Dockerfile"
    ["dashboard"]="src/AdImpactOs.Dashboard/Dockerfile"
    ["demoui"]="src/AdImpactOs.DemoUI/Dockerfile"
    ["eventconsumer"]="src/AdImpactOs.EventConsumer/Dockerfile"
  )

  for IMAGE in "${!SERVICES[@]}"; do
    DOCKERFILE="${SERVICES[$IMAGE]}"
    FULL_TAG="${ACR_LOGIN_SERVER}/${IMAGE}:${TAG}"
    echo ""
    echo "▸ Building $IMAGE"
    docker build -t "$FULL_TAG" -f "$DOCKERFILE" .
    echo "▸ Pushing $IMAGE"
    docker push "$FULL_TAG"
  done
fi

# ── Update App Service Container Images ──
echo ""
echo "▸ Updating App Service container images"

ACR_USERNAME=$(az acr credential show --name "$ACR_NAME" --query username --output tsv)
ACR_PASSWORD=$(az acr credential show --name "$ACR_NAME" --query "passwords[0].value" --output tsv)

declare -A APP_IMAGES=(
  ["${PREFIX}-panelist-${ENV}"]="panelistapi:${TAG}"
  ["${PREFIX}-campaign-${ENV}"]="campaignapi:${TAG}"
  ["${PREFIX}-survey-${ENV}"]="surveyapi:${TAG}"
  ["${PREFIX}-dashboard-${ENV}"]="dashboard:${TAG}"
  ["${PREFIX}-demoui-${ENV}"]="demoui:${TAG}"
  ["${PREFIX}-consumer-${ENV}"]="eventconsumer:${TAG}"
)

for APP in "${!APP_IMAGES[@]}"; do
  IMAGE="${ACR_LOGIN_SERVER}/${APP_IMAGES[$APP]}"
  az webapp config container set \
    --resource-group "$RG" \
    --name "$APP" \
    --docker-custom-image-name "$IMAGE" \
    --docker-registry-server-url "https://${ACR_LOGIN_SERVER}" \
    --docker-registry-server-user "$ACR_USERNAME" \
    --docker-registry-server-password "$ACR_PASSWORD" \
    --output none
  echo "  ✓ $APP → $IMAGE"
done

# ── Deploy Azure Functions ──
echo ""
echo "▸ Deploying Azure Functions"
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"
cd "$REPO_ROOT"

dotnet publish src/AdImpactOs/AdImpactOs.csproj --configuration Release --output ./functions-publish
cd functions-publish
func azure functionapp publish "$FUNC_APP" --dotnet-isolated
cd "$REPO_ROOT"
rm -rf functions-publish

# ── Health Checks ──
echo ""
echo "▸ Running health checks (waiting 30s for containers to start)"
sleep 30

HEALTH_OK=true
for APP_SHORT in "panelist" "campaign" "survey"; do
  APP_NAME="${PREFIX}-${APP_SHORT}-${ENV}"
  URL="https://${APP_NAME}.azurewebsites.net/health"
  if curl --fail --silent --max-time 10 "$URL" > /dev/null 2>&1; then
    echo "  ✓ $APP_NAME healthy"
  else
    echo "  ✗ $APP_NAME NOT healthy ($URL)"
    HEALTH_OK=false
  fi
done

DASHBOARD_URL="https://${PREFIX}-dashboard-${ENV}.azurewebsites.net/"
if curl --fail --silent --max-time 10 "$DASHBOARD_URL" > /dev/null 2>&1; then
  echo "  ✓ Dashboard reachable"
else
  echo "  ✗ Dashboard NOT reachable"
  HEALTH_OK=false
fi

echo ""
if [ "$HEALTH_OK" = true ]; then
  echo "==========================================="
  echo "✓ Deployment complete – all services healthy"
  echo "==========================================="
else
  echo "==========================================="
  echo "⚠ Deployment complete – some health checks failed"
  echo "  Services may still be starting up. Retry in a few minutes."
  echo "==========================================="
  exit 1
fi
