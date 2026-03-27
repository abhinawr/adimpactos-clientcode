FROM mcr.microsoft.com/dotnet/runtime:8.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["src/AdImpactOs.EventConsumer/AdImpactOs.EventConsumer.csproj", "AdImpactOs.EventConsumer/"]
RUN dotnet restore "AdImpactOs.EventConsumer/AdImpactOs.EventConsumer.csproj"
COPY src/AdImpactOs.EventConsumer/ AdImpactOs.EventConsumer/
WORKDIR "/src/AdImpactOs.EventConsumer"
RUN dotnet build "AdImpactOs.EventConsumer.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "AdImpactOs.EventConsumer.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "AdImpactOs.EventConsumer.dll"]
