# Base image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80

# SDK image
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY PaymentSystem.API/PaymentSystem.API.csproj PaymentSystem.API/
COPY PaymentSystem.Core/PaymentSystem.Core.csproj PaymentSystem.Core/
RUN dotnet restore PaymentSystem.API/PaymentSystem.API.csproj

COPY . .

WORKDIR /src/PaymentSystem.API
RUN dotnet build PaymentSystem.API.csproj -c Release -o /app/build

# Publicar
FROM build AS publish
RUN dotnet publish PaymentSystem.API.csproj -c Release -o /app/publish

# Imagen final
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "PaymentSystem.API.dll"]
