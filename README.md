# Transaction Dispute Portal

A full-stack transaction dispute system where customers can view account transactions, submit disputes, and review dispute activity. Staff users can review and manage disputes through the administrative area.

## Quick start with Docker

Docker Compose is the recommended way to run the complete application because it starts the frontend, API, SQL Server, and database initialization process together.

### Prerequisites

- Git
- Docker Desktop with Docker Compose enabled
- Ports `8080`, `8081`, and `1433` available on the host machine

### Clone and start the application

From PowerShell, clone the repository and change into its root folder:

```powershell
git clone https://github.com/Adikai/TransactionDisputePortal.git
cd TransactionDisputePortal
docker compose up --build
```

The first startup downloads the required Docker images and builds the API and Blazor frontend images. The startup order is:

1. SQL Server starts and passes its health check.
2. `db-init` runs `init.sql` and `StoredProcs.sql`.
3. The API starts after database initialization completes.
4. nginx starts and serves the Blazor frontend while proxying `/api` requests to the API.

Keep the terminal open to see service logs. To run in the background instead, use:

```powershell
docker compose up --build -d
```

### Application URLs

| Component | URL | Purpose |
| --- | --- | --- |
| Web application | <http://localhost:8080> | Main Blazor WebAssembly application |
| API | <http://localhost:8081> | Direct API access |
| Scalar API documentation | <http://localhost:8081/scalar/v1> | Explore and test API endpoints |
| OpenAPI JSON | <http://localhost:8081/openapi/v1.json> | OpenAPI document |
| SQL Server | `localhost,1433` | Database access from host tools |

The browser normally calls the API through the frontend origin, for example `http://localhost:8080/api/Customer/login`. nginx forwards those requests to the internal `api` Compose service.

### Demo login

The SQL initialization script creates demo data. 
I had hardcoded a pre-login details to make it easier to test the application from a Customer point of view.
The administrator demo account is:

```text
Username/email: admin
Password: admin
```

These credentials, the SQL Server password, and the placeholder token are for local demonstration only. They must be replaced with secure configuration and real authentication before production use.

## Stopping and resetting the application

Stop the running services with:

```powershell
docker compose down
```

The current Compose setup does not define a persistent SQL Server volume, so removing and recreating the SQL Server container resets the database. The initialization scripts are run by the `db-init` service on startup. To force a clean rebuild of images and containers:

```powershell
docker compose down
docker compose build --no-cache
docker compose up
```

Useful diagnostics:

```powershell
docker compose ps
docker compose logs api
docker compose logs web-app
docker compose logs sqlserver
```

If the API reports a database connection error, verify that the API uses `sqlserver,1433` inside Docker. `localhost` refers to the current container and is not the SQL Server container.

## Application workflow

1. A user opens the Blazor WebAssembly client in a browser.
2. The user submits login details from the login page.
3. The client sends a request to `api/Customer/login` through nginx.
4. The API validates the request and calls the account repository.
5. The repository invokes a SQL Server stored procedure through the shared SQL execution abstraction.
6. The API returns the user details and the current demo token.
7. The client stores the authentication information using `AuthService` and routes the user to the customer or staff area.
8. Customers can view transactions and submit disputes. Staff users can access administrative dispute workflows.
9. Transaction, customer, dispute, and audit operations are handled by API controllers and repository implementations backed by stored procedures.

## Architecture

```text
Browser
  |
  v
nginx / Blazor WebAssembly (web-app :8080)
  |  serves static client files
  |  proxies /api/*
  v
ASP.NET Core API (api :8080 inside Compose, :8081 on host)
  |
  v
Repositories and ISqlExecuter
  |
  v
SQL Server (sqlserver :1433)
```

### Projects

- `TransactionDisputePortalApp` — Blazor WebAssembly client, pages, client services, static assets, SQL initialization scripts, and nginx configuration.
- `TransactionDisputeAPI` — ASP.NET Core API, controllers, middleware, CORS, Serilog, OpenAPI, and Scalar.
- `TransactionDisputePortal.Services` — infrastructure and repository implementations using Dapper and `Microsoft.Data.SqlClient`.
- `TransactionDisputePortal.Core` — core interfaces and shared application abstractions.
- `TransactionDisputePortal.Shared` — DTOs and models shared by the client and API.

### Container build

- `Dockerfile` publishes the API into a .NET 10 ASP.NET runtime image.
- `Dockerfile.client` publishes the Blazor WebAssembly project and serves its `wwwroot` output from nginx.
- `docker-compose.yml` supplies the service network, connection string, ports, health checks, and startup dependencies.

## Development notes

The API uses environment-specific configuration. Local development can use `localhost,1433` when SQL Server is running on the host. The Docker Compose environment overrides the connection string with `Server=sqlserver,1433` because the API reaches SQL Server through the Compose network.

The current implementation is a demonstration system. I had nt done anything to restric CORS, enable proper JWT Auth, DB Security etc. To save time.
