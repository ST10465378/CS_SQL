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



## YouTube Demonstration Video

[Watch the demonstration video](https://youtu.be/gV_SXnpjdcE?si=6px-sD5x7pTKPpiD)
