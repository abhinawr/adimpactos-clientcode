FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["src/AdImpactOs.Dashboard/AdImpactOs.Dashboard.csproj", "src/AdImpactOs.Dashboard/"]
RUN dotnet restore "src/AdImpactOs.Dashboard/AdImpactOs.Dashboard.csproj"
COPY . .
WORKDIR "/src/src/AdImpactOs.Dashboard"
RUN dotnet build "AdImpactOs.Dashboard.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "AdImpactOs.Dashboard.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "AdImpactOs.Dashboard.dll"]
