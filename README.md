RaceDay

RaceDay is a race-event management system that lets running/athletics event organisers publish races and manage race-day logistics, while participants can browse events, enrol in categories, and view their results. The system is built around a relational database (SQL Server) and a RESTful API, planned in full before any application code was written (see `/docs`), and will be implemented as a The system is implemented using ASP.NET Core 8, C#, Entity Framework Core, SQL Server, an MVC frontend, Azure Blob Storage, and Docker containerisation.

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


# Part 3 – ASP.NET Core MVC, Azure Blob Storage and Docker

RaceDay Part 3 completes the race-event management platform by integrating an **ASP.NET Core 8 MVC frontend** with the existing Part 2 RESTful API, implementing **Azure Blob Storage** for image uploads, and introducing **Docker containerisation** for consistent application deployment.

The application provides separate role-based interfaces for Organisers and Participants, with a responsive sporting-themed design.

## Part 3 Architecture

The application follows a layered architecture:

```text
ASP.NET Core MVC (RaceDay.Web)
            |
            | HttpClient
            v
ASP.NET Core Web API (RaceDay.Api)
            |
            +---- SQL Server (Application Data)
            |
            +---- Azure Blob Storage (Images)
```

The MVC application communicates exclusively with the API through `HttpClient`. It does not access SQL Server or Azure Blob Storage directly.

The REST API handles authentication, validation, database operations, business rules and image uploads.

## Part 3 Project Structure

```text
Part2-API/
  src/RaceDay.Api/
    Controllers/
    Services/
      BlobImageService.cs
    Data/
    Models/
    Dtos/
  tests/
  Dockerfile

Part3-MVC/
  RaceDay.Web/
    Controllers/
    Models/
    Services/
      RaceDayClient.cs
    Views/
    wwwroot/
    Program.cs
    appsettings.json
  Dockerfile

.github/
  workflows/
    part3-build.yml

scripts/
  ci-docker-smoke.sh

docker-compose.yml
.env.example
DOCKER-SUBMISSION.md
```

## ASP.NET Core MVC and HttpClient Integration

The `RaceDay.Web` project consumes the existing Part 2 REST API using `HttpClient`.

The MVC frontend handles presentation and user interaction, while the API remains responsible for data operations.

Features include:

- User registration and login through the API.
- Session-based authentication and role-aware navigation.
- Public event browsing and filtering.
- Organiser event management.
- Event category creation and management.
- Participant enrolments.
- Race result capture and display.
- Participant profile management.

The application separates Organiser and Participant functionality using role-based access controls.

## Organiser Interface

Organisers can:

- Access a dashboard showing their events.
- Create, edit and delete or cancel events.
- Upload event banner images.
- Manage event categories.
- View registered participants and enrolment statuses.
- Capture and publish race results, including finish times and positions.

Organisers can only manage events and related records that belong to their accounts.

## Participant Interface

Participants can:

- Browse and filter published events.
- View event details and available categories.
- Enrol in race categories.
- View their existing enrolments and enrolment statuses.
- View their race results and finishing positions.
- View and update their profile information.
- Upload a profile picture.

## Azure Blob Storage Integration

Azure Blob Storage is integrated into the existing REST API to manage image uploads.

**Azure configuration:**

| Configuration | Value |
|---|---|
| Storage account | `racedaypoe3` |
| Resource group | `RaceDay-UAE-RG` |
| Azure region | UAE North |
| Storage tier | Standard / Hot |
| Redundancy | Locally-redundant storage (LRS) |
| Authentication | Storage connection string configured securely |
| Container access | Private |

The application uses two containers:

- `event-banners` – stores uploaded event banner images.
- `profile-pictures` – stores participant profile pictures.

### Image upload process

1. The user selects an image in the MVC application.
2. MVC sends the file to the REST API.
3. The API validates the image format and size.
4. The API uploads the image using the `Azure.Storage.Blobs` NuGet package.
5. The permanent blob URL is saved in SQL Server.
6. The API generates temporary read-only SAS URLs when images need to be displayed.

Supported image formats include JPG, PNG and WEBP, with a maximum upload size of 5 MB.

**Security:** Azure credentials are stored using .NET User Secrets during local development and must never be committed to GitHub. Uploaded images remain in private Blob containers.

## Docker Containerisation

RaceDay supports containerisation using Docker and Docker Compose.

The Docker Compose configuration defines three services:

| Service | Purpose | Local port |
|---|---|---|
| `db` | SQL Server 2022 database | Internal database service |
| `api` | ASP.NET Core REST API | 5080 |
| `web` | ASP.NET Core MVC frontend | 5180 |

Docker networking allows the MVC application to communicate with the API using the internal address:

`http://api:8080/`

The API communicates with SQL Server using the Docker service hostname `db`.

### Running with Docker

**Requirements:**

- Docker Engine with Docker Compose support.
- Sufficient resources for SQL Server and the .NET applications.
- Valid environment configuration.

1. Clone or extract the repository.
2. Copy `.env.example` to `.env`.
3. Set a strong `MSSQL_SA_PASSWORD`.
4. Configure `AZURE_BLOB_CONNECTION_STRING` if Azure uploads are required.
5. Open a terminal in the repository root.
6. Run:

```bash
docker compose up --build -d
```

Once the services are ready, access:

- MVC application: `http://localhost:5180`
- REST API: `http://localhost:5080`

To stop the containers:

```bash
docker compose down
```

See `DOCKER-SUBMISSION.md` for additional setup instructions.

**Important:** Real Azure credentials are not included in the repository. They must be configured securely by whoever runs the application.

## GitHub Actions – Part 3 CI/CD

The Part 3 workflow extends the existing automated CI pipeline.

The workflow performs:

1. Repository checkout and .NET 8 setup.
2. Dependency restoration.
3. API build and automated unit tests.
4. MVC project build.
5. Docker image builds for the API and MVC.
6. Docker Compose startup.
7. Automated smoke testing of the containerised application.

**CI/CD result:** The latest verified GitHub Actions workflow passed, including the Docker Compose smoke test.

### Part 3 Successful Workflow

![RaceDay Part 3 successful GitHub Actions workflow](docs/images/part3-ci-success.png)

The screenshot demonstrates the successful automated build and testing pipeline.

## Testing and Verification

The following functionality has been tested during development:

| Feature | Result |
|---|---|
| API Swagger interface | Passed |
| MVC frontend launch | Passed |
| MVC to API HttpClient communication | Passed |
| Participant registration and login | Passed |
| Event browsing and enrolment | Passed |
| Participant event and profile pages | Passed |
| Azure Blob image uploads | Passed |
| Private Azure image display | Passed |
| GitHub Actions builds and tests | Passed |
| Docker Compose smoke testing | Passed |

The application was tested locally in Visual Studio, while Docker image builds and container smoke tests were executed through GitHub Actions.

## Part 3 Demonstration Video

The final demonstration covers:

- The RaceDay application architecture.
- The ASP.NET Core MVC frontend.
- API integration using `HttpClient`.
- Organiser and Participant functionality.
- Event creation, categories, enrolments and results.
- Azure Blob Storage and image uploads.
- Docker and Docker Compose configuration.
- GitHub Actions CI/CD workflow.
- Explanation of the project structure and implementation.

**YouTube demonstration:**

[Watch RaceDay Part 3 Demonstration](https://youtu.be/9yNKtCHBIQ0)

*The video is uploaded as an unlisted YouTube video for assessment.*

## Submission

The final RaceDay submission contains the complete Part 1 planning documentation, Part 2 REST API, Part 3 MVC frontend, Docker configuration, GitHub Actions workflows, and supporting documentation.

The repository contains all source code required to rebuild the application. Secrets and environment-specific credentials are excluded for security.