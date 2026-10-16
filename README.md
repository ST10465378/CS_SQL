RaceDay

RaceDay is a race-event management system that lets running/athletics event organisers publish races and manage race-day logistics, while participants can browse events, enrol in categories, and view their results. The system is built around a relational database (SQL Server) and a RESTful API, planned in full before any application code was written (see `/docs`), and will be implemented as a **.NET C# application**.

Brief description of the system

RaceDay allows an **Organiser** to create and manage events (e.g. a spring road run), each of which can have multiple **Categories** (e.g. 5km, 10km, 21km) with their own distance, entry fee, and participant cap. A **Participant** can browse published events, enrol in a specific category, and receive a bib number. Once a race has taken place, the organiser captures each participant's finish time and position, which is stored as that enrolment's **Result** and can be viewed as a per-category leaderboard.

The full data model, API endpoint plan, and SQL Server database script for this system are committed under `/docs`:
- `docs/erd.png` – Entity Relationship Diagram
- `docs/RaceDay_Planning_Part1.docx` – ERD + full API endpoint plan
- `docs/raceday.sql` – database schema + seed data (SSMS)

User roles

**Organiser**
An Organiser account represents a race event host (e.g. a running club or event company). Organisers can register, log in, create and manage their own events, add race categories to those events, view who has enrolled in each category, and capture race results once an event has taken place. An organiser can only view/edit/delete events, categories, and results that belong to them.

**Participant**
A Participant account represents a runner. Participants can register, log in, browse published events and their categories, enrol themselves in a category (receiving a bib number), view their own list of enrolments, withdraw from an event before it takes place, and view their own results once captured by the organiser.

## CI/CD Pipeline

A GitHub Actions workflow validates the repository structure on every push (e.g. checking that `/docs` exists and contains the required planning files).

**Screenshot of a successful green build:**

<img width="1100" height="480" alt="Screenshot 2026-09-04 165038" src="https://github.com/user-attachments/assets/36b99fee-f985-42fe-bfdb-9048f687a190" />


## YouTube Demonstration Video

[Watch the demonstration video](https://youtu.be/gV_SXnpjdcE?si=6px-sD5x7pTKPpiD)

# Part 2 – RESTful API (`/Part2-API`)

ASP.NET Core 8 Web API with Entity Framework Core (code-first, SQL Server), server-side **session** authentication with role-based access (Organiser / Participant), Swagger UI, unit tests and a GitHub Actions build.

## Folder structure

```
.github/workflows/part2-api-ci.yml   CI: restore, build, run unit tests on every push
Part2-API/
  RaceDay.sln
  Database/part2_schema_changes.sql  Changes to the Part 1 script (Events.DistanceKM, Events.EventType)
  src/RaceDay.Api/
    Controllers/  Auth, Users, Events, Categories, Enrolments, Results
    Models/       Entities (User, Organiser, Participant, Event, Category, Enrolment, Result)
    Data/         RaceDayDbContext (schema matches Part 1 ERD) + seeder
    Dtos/         Request/response models + validation
    Security/     PBKDF2 password hashing + session authorisation attribute
  tests/RaceDay.Api.Tests/           Unit tests (added in the next stage)
```

## Setup and run

**Requirements:** .NET 8 SDK, SQL Server (LocalDB ships with Visual Studio), Visual Studio 2022 or the `dotnet` CLI.

1. Clone the repo and open `Part2-API/RaceDay.sln`.
2. Check the connection string in `src/RaceDay.Api/appsettings.json` (default: `(localdb)\localhost\SQLEXPRESS01`, database `raceday_db`).
3. Create the database (code-first). From `Part2-API/src/RaceDay.Api`:
   ```
   dotnet tool install --global dotnet-ef
   dotnet ef migrations add InitialCreate
   dotnet ef database update
   ```
   (If no migration exists the app creates the database on first start and seeds demo data.)
4. Run: press F5 in Visual Studio, or `dotnet run`. Swagger UI opens at `/swagger`.

**Demo accounts** (password `Password123!`): `thabo.mokoena@raceday.co.za` (Organiser), `sipho.nkosi@example.com` (Participant).

## Authentication and roles

- `POST /api/auth/register` – choose role `Organiser` or `Participant`. Organisers supply `organisationName`; Participants supply `dateOfBirth`. Optionally set `Registration:OrganiserCode` in `appsettings.json` so Organiser accounts need an invite code (stops anyone self-assigning the Organiser role).
- `POST /api/auth/login` – verifies the password hash and creates a **server-side session** storing the user ID, role and profile ID; the browser keeps only the session cookie. `POST /api/auth/logout` destroys it.
- Passwords are hashed with PBKDF2-SHA256 (100 000 iterations, random salt); the plain password is never stored.
- Protected endpoints return **401** with no session and **403** for the wrong role or for resources the user does not own.

## Endpoints

| Area | Endpoint | Access |
|---|---|---|
| Auth | `POST /api/auth/register`, `POST /api/auth/login`, `POST /api/auth/logout` | Public |
| Profile | `GET/PUT/DELETE /api/users/me` | Logged in |
| Events | `GET /api/events`, `GET /api/events/{id}` | Public |
| Events | `GET /api/events/mine`, `POST /api/events`, `PUT/DELETE /api/events/{id}` | Organiser (owner) |
| Categories | `GET /api/events/{id}/categories` | Public |
| Categories | `POST /api/events/{id}/categories`, `PUT/DELETE /api/categories/{id}` | Organiser (owner) |
| Enrolments | `POST /api/categories/{id}/enrolments`, `GET /api/users/me/enrolments`, `DELETE /api/enrolments/{id}` | Participant |
| Enrolments | `GET /api/categories/{id}/enrolments` | Organiser (owner) |
| Results | `POST /api/enrolments/{id}/results` | Organiser (owner) |
| Results | `GET /api/categories/{id}/results` | Public |
| Results | `GET /api/enrolments/{id}/results` | Participant or Organiser (owner) |

Every endpoint is documented in Swagger UI with its purpose, request body and possible responses.

## Changes from the Part 1 plan

- Events now also store **distance** (`DistanceKm`) and **event type** (`run`, `walk` or `cycle`), as required by the Part 2 brief. See `Part2-API/Database/part2_schema_changes.sql`.
- Login uses a **server-side session cookie** instead of returning a token; `POST /api/auth/logout` and `GET /api/events/mine` were added.

## CI/CD – Part 2

The workflow `.github/workflows/part2-api-ci.yml` restores, builds and runs all unit tests on every push.

**Screenshot of a successful green build (Part 2):**

[Part 2 green build](docs/ci%20workflow.png)

## Part 2 video demonstration

<!-- TODO: replace with your unlisted YouTube link covering all endpoints, authentication, Swagger, unit tests and code explanation -->
[Watch the Part 2 demonstration video](https://youtu.be/Trwgtz7dKxw)