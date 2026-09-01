# dy-dashboard-backend-dotnet

A **.NET 10** REST API that powers the [dy-dashboard](https://github.com/nathmsi/dy-dashboard) campaigns view. It is a faithful, idiomatic port of the [Node/Express backend](https://github.com/nathmsi/dy-dashboard-backend) — same endpoints, same request/response contract, same behaviour — rebuilt on the modern ASP.NET Core stack.

## Stack

| Concern | Node backend | This .NET backend |
| --- | --- | --- |
| Runtime / framework | Node.js + Express 5 | .NET 10 + ASP.NET Core Minimal APIs |
| Persistence | better-sqlite3 (raw SQL) | EF Core 10 + SQLite |
| Schema / migrations | hand-rolled migration table | EF Core migrations |
| Validation | Zod | FluentValidation |
| Logging | Pino | Serilog |
| API docs | swagger-ui-express | Swashbuckle (Swagger UI) |
| Rate limiting | express-rate-limit | built-in `RateLimiter` (fixed window) |
| Tests | Vitest + Supertest | xUnit + `WebApplicationFactory` |

## Architecture

Layered, feature-oriented — the same separation of concerns as the Node app:

```
src/DyDashboard.Api/
  Program.cs                     # composition root (app + server wiring)
  Configuration/ApiOptions.cs    # validated, fail-fast config (env-bound)
  Data/                          # DbContext, EF migrations, seeder
  Common/
    Errors/                      # typed AppException hierarchy
    Middleware/                  # central JSON error handler
    Validation/                  # FluentValidation endpoint filter
  Features/Campaigns/
    Campaign.cs                  # entity
    CampaignDtos.cs              # request/response records
    CampaignValidators.cs        # FluentValidation rules
    CampaignRepository.cs        # data access (the only place with EF queries)
    CampaignService.cs           # business rules (throws NotFoundException, …)
    CampaignEndpoints.cs         # HTTP mapping (thin, no business logic)
tests/DyDashboard.Api.Tests/     # xUnit integration tests (in-memory SQLite)
```

Rule of thumb, identical to the Node version: **endpoints** only do HTTP, the **service** holds business rules, the **repository** is the only place that touches the database.

## API

Canonical prefix: **`/api/v1/campaigns`**. `/api/campaigns` is kept as a **deprecated alias** for the current frontend.

| Method | Path | Description |
| --- | --- | --- |
| `GET` | `/api/v1/campaigns` | List — paginated, filterable (`status`, `channel`, `search`), sortable (`sort`, `order`) |
| `GET` | `/api/v1/campaigns/{id}` | Get one |
| `POST` | `/api/v1/campaigns` | Create → `201` + `Location` |
| `PATCH` | `/api/v1/campaigns/{id}` | Partial update |
| `DELETE` | `/api/v1/campaigns/{id}` | Delete → `204` |
| `GET` | `/health` | Health check (not rate-limited) |

The list endpoint returns a **bare array**; pagination metadata travels in the `X-Total-Count`, `X-Total-Pages`, `X-Page`, `X-Limit` and RFC 5988 `Link` response headers — matching the Node contract exactly.

Errors use a consistent envelope: `{ "error": { "code", "message", "details"? } }` with codes `NOT_FOUND` (404), `VALIDATION_ERROR` (422), `BAD_REQUEST` (400), `INTERNAL_ERROR` (500).

- Swagger UI: `/api/docs`
- OpenAPI spec: `/api/openapi.json`

## Getting started

Requires the [.NET 10 SDK](https://dotnet.microsoft.com/download).

```bash
dotnet restore
dotnet run --project src/DyDashboard.Api
```

The API listens on <http://localhost:3001>. On first boot it applies migrations and seeds ten demo campaigns (mirroring the frontend's original mock data).

```bash
dotnet test          # run the integration test suite
dotnet format        # apply code style
```

## Configuration

Bound from the `Api` section of `appsettings.json` and overridable via environment variables (ASP.NET's `Api__Key` convention). The process fails fast on startup if a value is invalid.

| Key | Env var | Default | Description |
| --- | --- | --- | --- |
| `Api:DatabasePath` | `Api__DatabasePath` | `./.data/dashboard.db` | SQLite path (`:memory:` for ephemeral) |
| `Api:CorsOrigin` | `Api__CorsOrigin` | `http://localhost:5173` | Comma-separated allowed origins |
| `Api:RateLimitWindowMs` | `Api__RateLimitWindowMs` | `900000` | Rate-limit window (ms) |
| `Api:RateLimitMax` | `Api__RateLimitMax` | `100` | Max requests per window per IP |
| — | `PORT` | `3001` | Listening port (injected by hosts like Render) |

## Deployment

A `Dockerfile` (multi-stage) and a `render.yaml` Blueprint are included, mirroring the Node repo. On Render's free plan the SQLite file is ephemeral and re-seeds on each deploy — fine for a demo. Set `Api__CorsOrigin` to the deployed dashboard's origin.
