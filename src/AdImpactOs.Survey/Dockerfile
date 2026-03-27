FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["src/AdImpactOs.Survey/AdImpactOs.Survey.csproj", "src/AdImpactOs.Survey/"]
RUN dotnet restore "src/AdImpactOs.Survey/AdImpactOs.Survey.csproj"
COPY . .
WORKDIR "/src/src/AdImpactOs.Survey"
RUN dotnet build "AdImpactOs.Survey.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "AdImpactOs.Survey.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "AdImpactOs.Survey.dll"]
