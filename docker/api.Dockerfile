FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY VehicleExplorer.Domain/VehicleExplorer.Domain.csproj VehicleExplorer.Domain/
COPY VehicleExplorer.Application/VehicleExplorer.Application.csproj VehicleExplorer.Application/
COPY VehicleExplorer.Infrastructure/VehicleExplorer.Infrastructure.csproj VehicleExplorer.Infrastructure/
COPY Vehicle-Explorer-Backend/VehicleExplorer.API.csproj Vehicle-Explorer-Backend/
RUN dotnet restore Vehicle-Explorer-Backend/VehicleExplorer.API.csproj

COPY VehicleExplorer.Domain/ VehicleExplorer.Domain/
COPY VehicleExplorer.Application/ VehicleExplorer.Application/
COPY VehicleExplorer.Infrastructure/ VehicleExplorer.Infrastructure/
COPY Vehicle-Explorer-Backend/ Vehicle-Explorer-Backend/
RUN dotnet publish Vehicle-Explorer-Backend/VehicleExplorer.API.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

USER root
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*
USER $APP_UID

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "VehicleExplorer.API.dll"]
