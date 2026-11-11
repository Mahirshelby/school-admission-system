# School Admission System

A seat-hold and admission service for schools. A student can place a temporary hold on a seat in a class, then pay to confirm it or cancel it. Unpaid holds expire automatically and the seat returns to the pool. An admin page shows all classes and all applications.

## Tech stack

- Backend: .NET 8 (C#), ASP.NET Core Web API, Entity Framework Core
- Database: PostgreSQL 16
- Frontend: Vite + React + TypeScript (axios, react-router-dom), served by nginx in Docker
- Tests: xunit, Testcontainers (PostgreSQL), WebApplicationFactory, FakeTimeProvider
- Containers: Docker and Docker Compose

## Project structure

    backend/
      SchoolAdmission.sln
      Domain/           entities and enums
      Application/      service abstractions, DTOs, exception types
      Infrastructure/   EF Core DbContext, migrations, AdmissionService
      API/              controllers, exception middleware, hold expiry sweeper, Program.cs
      Tests/            integration tests
    frontend/           React app (StudentPage, AdminPage)
    docker-compose.yml

## Prerequisites

- Docker Desktop (Docker Compose included). On Windows it sometimes has to be started manually after a restart.
- To run the tests locally: .NET SDK 8, and Docker running (the tests start a real PostgreSQL container with Testcontainers).

## Run with Docker

From the repository root:

    docker compose up --build -d

Stop it with `docker compose down`. To also delete the database volume (`pgdata`), use `docker compose down -v`.

Database migrations are applied automatically when the backend starts.

## URLs and ports

| Service  | URL                           |
|----------|-------------------------------|
| Frontend | http://localhost:3000         |
| API      | http://localhost:8080         |
| Swagger  | http://localhost:8080/swagger |

## Configuration

| Variable                     | Default                   | Meaning                                          |
|------------------------------|---------------------------|--------------------------------------------------|
| `HOLD_DURATION_SECONDS`      | `600`                     | How long a hold lasts before it expires          |
| `ConnectionStrings__Default` | set in docker-compose.yml | PostgreSQL connection string used by the backend |

docker-compose.yml passes `HOLD_DURATION_SECONDS: ${HOLD_DURATION_SECONDS:-600}`, so it can be overridden for one run. In PowerShell:

    $env:HOLD_DURATION_SECONDS = "20"
    docker compose up --build -d
