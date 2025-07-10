# Base image
FROM mcr.microsoft.com/dotnet/runtime:8.0 AS base
WORKDIR /app

# SDK image
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

#  csproj
COPY PaymentSystem.Worker/PaymentSystem.Worker.csproj PaymentSystem.Worker/
COPY PaymentSystem.Core/PaymentSystem.Core.csproj PaymentSystem.Core/
RUN dotnet restore PaymentSystem.Worker/PaymentSystem.Worker.csproj


COPY . .

WORKDIR /src/PaymentSystem.Worker
RUN dotnet build PaymentSystem.Worker.csproj -c Release -o /app/build

# Publicar
FROM build AS publish
RUN dotnet publish PaymentSystem.Worker.csproj -c Release -o /app/publish

# Imagen final
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "PaymentSystem.Worker.dll"]

