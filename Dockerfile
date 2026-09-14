# Stage 1: Runtime Base
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

# Stage 2: SDK Build & Publish
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy full repository source tree
COPY . .

# Restore all solution projects. The solution is in the repository subdirectory.
RUN dotnet restore TransactionDisputePortalApp/TransactionDisputePortalApp.slnx

# Build and publish the API project dynamically
RUN dotnet publish TransactionDisputeAPI/TransactionDisputePortal.API.csproj -c Release -o /app/publish /p:UseAppHost=false

# Stage 3: Final Image
FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "TransactionDisputePortal.API.dll"]
