# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj and restore dependencies
COPY ["src/AdImpactOs.PanelistAPI/AdImpactOs.PanelistAPI.csproj", "AdImpactOs.PanelistAPI/"]
RUN dotnet restore "AdImpactOs.PanelistAPI/AdImpactOs.PanelistAPI.csproj"

# Copy everything else and build
COPY src/AdImpactOs.PanelistAPI/ AdImpactOs.PanelistAPI/
WORKDIR "/src/AdImpactOs.PanelistAPI"
RUN dotnet build "AdImpactOs.PanelistAPI.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "AdImpactOs.PanelistAPI.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
EXPOSE 80
EXPOSE 443

COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "AdImpactOs.PanelistAPI.dll"]
