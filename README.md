# FitTrack

A small social fitness tracker for a group of friends. Log workouts set by set, meals with calories and macros, and body weight; set goals; follow what your friends share in a live feed; and see your progress on a dashboard.

**Stack:** C# · ASP.NET Core 10 (minimal APIs) · Entity Framework Core 10 · MySQL 8.4 · SignalR · React 18 + TypeScript (Vite) · Recharts · Docker · xUnit + Testcontainers · GitHub Actions

## Features

- **Invite-only accounts.** Members create invite links; whoever joins with one becomes their friend automatically. JWT authentication, passwords hashed with PBKDF2.
- **Workouts** with ordered exercises: strength exercises as individual sets (reps × kg), cardio as time and/or distance. Exercise names are suggested from your own history.
- **Meals** with calories and optional protein, carbs and fat, totalled per day in your own time zone.
- **Body measurements**: weight, body fat and waist, one per day.
- **Goals**: target weight, workouts per week, daily calories and daily protein, with progress computed from what you log. Reaching a shared goal is posted to the feed.
- **Privacy per entry.** Every workout, meal, measurement and goal is either *Private* or visible to *Friends*, with a default you choose. Anything you may not see returns 404, exactly like something that does not exist.
- **Live feed.** Shared workouts and achieved goals appear in your friends' feeds the moment they are saved, over SignalR.
- **Progress dashboard**: weight trend against your target, workouts per week, weekly strength volume, and daily calories and protein.
- **Your data is yours**: download everything as JSON, or delete your account and all data.

## Run it

You need [Docker](https://www.docker.com/). From the repository root:

```bash
cp .env.example .env   # then fill in your own passwords and a JWT key
docker compose up --build
```

Open <http://localhost:8080>.

FitTrack is invite-only, so the very first account needs an invite that nobody issued. While no account exists, the API writes one to its log:

```bash
docker compose logs api | grep "invite code"
```

Register with that code, then invite everyone else from the Friends page.

## Develop

Requirements: [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0), [Node.js 22](https://nodejs.org/) and Docker.

```bash
# Database in Docker
docker compose up -d db

# API on http://localhost:5080 (creates the schema on first start)
dotnet run --project src/FitTrack.Api

# Web client on http://localhost:5173 (proxies /api and /hubs to the API)
cd web
npm install
npm run dev
```

In development the OpenAPI document is served at <http://localhost:5080/openapi/v1.json>, and `src/FitTrack.Api/FitTrack.Api.http` has requests you can send from VS Code (REST Client) or Rider.

## Tests

```bash
dotnet test           # unit + integration tests; integration tests start MySQL with Testcontainers, so Docker must run
cd web && npm test    # client unit tests
```

GitHub Actions runs both on every push, type-checks and builds the client, and starts the whole Docker stack for a smoke test through nginx.

## Project layout

```
src/FitTrack.Api/
  Domain/        entities and enums
  Data/          AppDbContext: keys, indexes, conversions, cascade deletes
  Auth/          JWT options and token creation
  Common/        problem-details errors, validation, time zones, paging
  Features/      one folder per area: endpoints, service, request/response models
    Accounts/ Invites/ Friends/ Workouts/ Meals/ Measurements/ Goals/ Feed/ Stats/
tests/FitTrack.Api.Tests/
  ApiFactory.cs  boots the API against a throwaway MySQL container
web/
  src/api/       typed fetch client and DTO types
  src/pages/     feed, logging, progress, friends, settings
  nginx.conf     serves the build and proxies /api and /hubs in Docker
```

## Design notes

- **Visibility is one rule in one place.** `VisibilityService.VisibleTo` filters any query of owned entries to "mine, or shared by a friend". The friendship check is an `EXISTS` subquery, so it stays in SQL.
- **Friendships are stored once**, in the direction they were requested; "are A and B friends" looks both ways.
- **Feed posts are stored**, pointing at the workout or goal they are about. A post is removed (and removed live from friends' screens) when its entry is deleted or made private.
- **Times are UTC, days are local.** Timestamps are stored in UTC; "today" and "this week" are evaluated in each member's own time zone, so a late-night snack counts for the right day.
- **Friends see goal progress, not the numbers behind it.** Progress can come from private entries, so for someone else's goal the API returns the percentage but hides current and starting values.
- **The dashboard is computed on demand** from the member's own entries and is never shown to anyone else.

## Not in this version

Habits and streaks, kudos and comments, food database lookup, workout templates, password reset by e-mail, and refresh tokens.
