FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["src/AdImpactOs.DemoUI/AdImpactOs.DemoUI.csproj", "src/AdImpactOs.DemoUI/"]
RUN dotnet restore "src/AdImpactOs.DemoUI/AdImpactOs.DemoUI.csproj"
COPY . .
WORKDIR "/src/src/AdImpactOs.DemoUI"
RUN dotnet build "AdImpactOs.DemoUI.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "AdImpactOs.DemoUI.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "AdImpactOs.DemoUI.dll"]
