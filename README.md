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

## API

Base URL: `http://localhost:8080`. All requests and responses are JSON. The student is identified by the `X-Student-Id` header (17 digits). Interactive docs: `/swagger`.

Hold statuses in API responses: `HELD`, `CONFIRMED`, `CANCELLED`, `EXPIRED`. All timestamps are UTC.

| # | Method | Path                      | Purpose                                  |
|---|--------|---------------------------|------------------------------------------|
| 1 | POST   | `/classes`                | Create a class                           |
| 2 | GET    | `/classes`                | List classes with seat counts            |
| 3 | GET    | `/classes/{classId}`      | One class with seat counts               |
| 4 | POST   | `/classes/{classId}/holds`| Apply (hold a seat)                      |
| 5 | GET    | `/holds/{holdId}`         | Current state of an application          |
| 6 | POST   | `/holds/{holdId}/confirm` | Pay the fee and confirm admission        |
| 7 | DELETE | `/holds/{holdId}`         | Cancel an application (204, no body)     |
| 8 | GET    | `/me/holds`               | All applications of the student          |
| 9 | GET    | `/admin/holds`            | All applications, newest first (no auth) |

### POST /classes

`shiftName` is optional and defaults to `General`.

Request:

    {"name":"Readme Demo","totalSeats":2}

Response 201:

    {"id":4,"name":"Readme Demo","shiftName":"General","totalSeats":2,"heldSeats":0,"confirmedSeats":0,"availableSeats":2}

### GET /classes and GET /classes/{classId}

Response 200 (one item shown):

    {"id":2,"name":"class 2","shiftName":"Morning","totalSeats":120,"heldSeats":0,"confirmedSeats":2,"availableSeats":118}

### POST /classes/{classId}/holds

Header: `X-Student-Id`. Response 201:

    {"holdId":"4d5afdf9-9f4f-411c-8b80-a09c6534af8c","classId":4,"className":"Readme Demo","shiftName":"General","studentId":"10000000000000001","status":"HELD","createdAt":"2026-11-11T16:10:07.0554381+00:00","expiresAt":"2026-11-11T16:20:07.0554381+00:00","confirmedAt":null,"cancelledAt":null}

If the student already has an active application for the class, the API returns 409:

    {"code":"already_applied","message":"You already have an active application for this class."}

### POST /holds/{holdId}/confirm

Header: `X-Student-Id`. Response 200 (same shape as a hold, with `status` `CONFIRMED` and `confirmedAt` set):

    {"holdId":"4d5afdf9-9f4f-411c-8b80-a09c6534af8c","classId":4,"className":"Readme Demo","shiftName":"General","studentId":"10000000000000001","status":"CONFIRMED","createdAt":"2026-11-11T16:10:07.055438+00:00","expiresAt":"2026-11-11T16:20:07.055438+00:00","confirmedAt":"2026-11-11T16:13:57.7329301+00:00","cancelledAt":null}

### DELETE /holds/{holdId}

Header: `X-Student-Id`. Returns 204 with no body.

### GET /me/holds

Header `X-Student-Id` is required. Without it the API returns 400:

    {"code":"student_id_required","message":"The X-Student-Id header is required."}

### GET /admin/holds

No authentication (see ASSUMPTIONS.md). Returns every application, newest first, as an array of the same hold objects shown above.

## Testing

Docker must be running, because the integration tests start a real PostgreSQL container with Testcontainers. From the repository root:

    dotnet test backend/SchoolAdmission.sln

The test project uses xunit, WebApplicationFactory and FakeTimeProvider, and includes a concurrency test that sends many requests at the same time against a class with limited seats.

## Concurrency strategy

The guarantee that `held + confirmed` never exceeds `totalSeats` comes from the database, not from the tests:

- Creating a hold runs inside a transaction that first locks the class row with `SELECT ... FOR UPDATE`. Concurrent requests for the same class therefore run one after another, and each one counts the current held and confirmed seats before taking a seat.
- A partial unique index on holds `(student_id, class_id)` where status is `Held` or `Confirmed` stops a student from having more than one active application per class, even if two requests race.
- Because the lock is in PostgreSQL, it applies to every backend process that uses the same database, not only to one process.

A passing concurrency test is a sanity check, not a proof. Repeated green runs cannot prove the absence of a race; the row lock is what gives the guarantee. Two backend processes on different ports against the same database were not tested here.

## Expiry and behaviour notes

- A hold lasts `HOLD_DURATION_SECONDS` (default 600). A hold whose `expiresAt` has passed is reported as `EXPIRED` on every read, even before any cleanup runs.
- A background sweeper runs every 5 seconds and marks overdue holds as expired in the database, so seats return without any API call.
- Confirm and cancel are idempotent: repeating them does not change the seat count a second time.
- Both the Student page and the class list poll the API, so seat counts and hold status update without a page reload.
- The Admin page does not auto-refresh. The Classes and Applications sections each have their own Refresh button.
- The Admin create-class form still requires a shift value in the UI, although the backend makes it optional.

## Verification

Automated (real output, `dotnet test backend/SchoolAdmission.sln`): 28 tests passed, 0 failed. The tests run against a real PostgreSQL container.

Manually checked in the browser with `docker compose up --build -d`:

- Admin dashboard: class stats and the Classes table, and the Applications section (desktop and mobile width).
- Student page: apply shows a `HELD` badge with a countdown and the Bengali pay button; paying shows `CONFIRMED`; cancelling shows a toast, `CANCELLED`, and the seat is released.
- Expiry: with `HOLD_DURATION_SECONDS=20`, the hold showed a countdown, then `EXPIRED`, the pay button was disabled, and the seat came back. The value was then returned to 600.
- API calls with PowerShell: create class, create hold, confirm, duplicate apply (409 `already_applied`), and `GET /me/holds` without the header (400).

Not verified:

- The duplicate-apply error message in the browser UI.
- Student application history restoring after F5 or after closing the browser (the student id is kept in localStorage; this was not checked in the browser).
- The background sweeper working on its own. The 20-second test only proved the read-time `EXPIRED` rule and the frontend state.
- Two backend processes on different ports sharing one database.
- A clean from-scratch run (`docker compose down -v`, then `up --build -d`).
