# Faaz — Backend

.NET 10 microservices backend for the Faaz online consultancy platform (student ↔ consultant booking, video sessions, payments, reviews).

The frontend (Next.js) lives in a separate repository: [Faaz_Frontend](https://github.com/Abrar-Kamoka/Faaz_Frontend).

## Architecture

A YARP API gateway fronts eight independently-deployable services, each following a `Domain` / `Infrastructure` / `WebHost` split. Services communicate asynchronously over RabbitMQ (event-driven) and share cross-cutting building blocks via `Faaz.SharedKernel` / `Faaz.BuildingBlocks`.

| Service | Purpose | Port (local) |
|---|---|---|
| Gateway | YARP reverse proxy, single entry point | 5000 |
| Identity | Auth, JWT (RS256), user accounts | 5101 |
| Student | Student profiles | 5102 |
| Consultant | Consultant applications & profiles | 5103 |
| Notification | Email (SMTP), SignalR hub, announcements | 55133 |
| Booking | Bookings, slots, sessions, reviews | 55134 |
| Payment | Stripe payments & payouts | 55135 |
| Administration | Admin config, roles, templates, audit log | 55136 |
| BackgroundJobs | Scheduled/cleanup jobs | 55137 |

Supporting infrastructure (via `docker-compose`): MSSQL (1433), RabbitMQ (5672 / mgmt UI 15672), Redis (6379), Seq structured logging (5341), Jaeger tracing (16686), MailHog dev SMTP (1025 / UI 8025).

## Tech stack

- .NET 10, EF Core, MSSQL
- YARP (gateway), RabbitMQ (events), Redis (cache)
- Stripe (payments), LiveKit (video sessions), SignalR (realtime)
- Seq + OpenTelemetry/Jaeger (observability)

## Getting started

**Prerequisites:** .NET 10 SDK, Docker Desktop.

1. Copy the environment template and fill in real values (JWT key, Stripe/LiveKit keys, etc.):
   ```bash
   cp .env.example .env
   ```
2. Start infrastructure + services:
   ```bash
   docker-compose up -d
   ```
3. Apply EF Core migrations (or use `infra/scripts/migrate-all.ps1`):
   ```powershell
   ./infra/scripts/migrate-all.ps1
   ```
4. Gateway is now reachable at `http://localhost:5000`. Seq at `http://localhost:5341`, Jaeger at `http://localhost:16686`, MailHog at `http://localhost:8025`.

To run a single service outside Docker for debugging, open `Faaz.slnx` in Visual Studio / Rider and launch the relevant `*.WebHost` project — it will still talk to the Dockerized infra (MSSQL, RabbitMQ, Redis).

## Project structure

```
src/
  Gateway/                  # YARP reverse proxy
  Services/
    Identity/  Student/  Consultant/  Notification/
    Booking/   Payment/  Administration/  BackgroundJobs/
  Shared/
    Faaz.SharedKernel/      # domain primitives shared by all services
    Faaz.BuildingBlocks/    # cross-cutting infra (messaging, telemetry, etc.)
tests/                      # integration tests
infra/
  docker/                   # DB init scripts
  scripts/                  # migration/dev helper scripts
Frontend/                   # separate git repo (Faaz_Frontend), gitignored here
```

## Notes

- `.env.example` documents every required variable — see inline comments for where to source Stripe/LiveKit keys.
- The Identity service's `appsettings.Development.json` holds an RSA private key and is intentionally gitignored — generate your own or pull from the team vault (see comment in `.gitignore`).
