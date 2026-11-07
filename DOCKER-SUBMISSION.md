# RaceDay Part 3 — Docker and CI instructions

## What this patch changes

- Builds the **existing** .NET 8 API and MVC images through GitHub Actions.
- Starts SQL Server, API and MVC in Compose on a GitHub-hosted Linux runner.
- Verifies that the API returns a seeded event **from SQL Server** and MVC displays that same event **through HttpClient**. A blank/error homepage does not count as a pass.
- Retries API startup if SQL Server's TCP port opens before database initialization is ready.
- Keeps the SQL port internal to Docker and avoids storing Azure keys in GitHub.
- Leaves Azure image upload testing to the already tested local environment or a separate secure integration test.

## How to apply to your CURRENT GitHub project

At your repository root (the folder with Part2-API, Part3-MVC and docker-compose.yml), **copy these files preserving their paths**:

- `docker-compose.yml` (replace existing)
- `.env.example` (replace existing)
- `.github/workflows/part3-build.yml` (replace the current workflow of the same name)
- `scripts/ci-docker-smoke.sh` (new)
- `DOCKER-SUBMISSION.md` (new)

Do not replace your current API/MVC source with older ZIP versions. Your local Azure upload and SAS changes should remain intact.

**Commit and push this patch**. On GitHub → Actions, inspect the latest run. It must show the build, the Compose startup, and **'PASS: MVC rendered an event from the API'**. The changes are prepared and checked structurally, but the Docker runtime has NOT been executed in this environment. If GitHub fails, read the `Show diagnostics` logs for the failing container.

## Local one-command startup (Linux Docker / Docker Desktop where available)

From the repository root:

1. Copy `.env.example` to `.env`.
2. Replace `MSSQL_SA_PASSWORD` with a strong **local** SQL Server password.
3. For Azure image uploads, set `AZURE_BLOB_CONNECTION_STRING` in `.env` to your **private** storage connection string. The CI job intentionally leaves it blank. A blank string allows event browsing and demo workflows but disables image uploads.
4. Run `docker compose up -d --build`.
5. Open MVC at **http://localhost:5180/** and Swagger at **http://localhost:5080/swagger**.
6. For logs: `docker compose logs -f api web db`.
7. To stop: `docker compose down`. `docker compose down -v` also **deletes the SQL data**.

The API uses `db` for SQL Server and MVC uses `http://api:8080/`; do not use `localhost` for inter-container calls. This Compose setup is intended for **local demonstration** and uses HTTP on host ports; a public deployment needs TLS, more robust secret management and production safeguards.

## Important limits

- The smoke test needs the **existing `DbInitializer`** to seed the sample event `Midrand Spring Run` on a fresh SQL volume. If you changed the seed records, update the text used in `scripts/ci-docker-smoke.sh`.
- The existing SQL container health probe checks the TCP port, not a full SQL query. The API's `restart: on-failure:5` mitigates the startup race; the smoke test verifies the end-to-end result.
- Real Azure Blob Storage is external to Docker. Connection strings must **never** be stored in Git, your submission ZIP or CI logs. The lecturer must supply their own authorized credentials to test cloud uploads, or assess your live recorded demonstration.
- Running in CI does not replace a full browser acceptance test of login, images, results or roles.
- Your lecturer needs a computer with Docker Engine/Compose, Internet access to fetch images, and sufficient memory for SQL Server.
- Existing GitHub history and meaningful commits remain in your repository, not in any patch archive.
