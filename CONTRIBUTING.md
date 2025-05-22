# Contributing to PitchSync

Thank you for your interest in contributing!

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Node.js 20](https://nodejs.org/)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/) (for databases)

## Running Locally (3 terminals)

**Terminal 1 — databases:**
```bash
docker-compose up identity-db match-db
```

**Terminal 2 — backend services (3 separate terminals or use tmux):**
```bash
dotnet run --project src/PitchSync.Gateway
dotnet run --project src/PitchSync.IdentityService
dotnet run --project src/PitchSync.MatchService
```

**Terminal 3 — Angular SPA:**
```bash
cd src/PitchSync.Angular
npm install
ng serve
```

The app is then available at http://localhost:4200. The Gateway listens on :5000 and proxies to IdentityService (:5010) and MatchService (:5020).

## Running with Docker Compose

```bash
cp .env.example .env
# Edit .env and set JWT_SECRET_KEY to a random string of at least 32 characters
docker-compose up --build
```

All six services (2 databases, 3 .NET services, Angular) start together. The app is available at http://localhost:4200.

## Running Tests

**Unit tests (no database required):**
```bash
dotnet test --filter Category=Unit
```

**Integration tests (requires databases running):**
```bash
docker-compose up identity-db match-db -d
dotnet test --filter Category=Integration
```

**Angular tests:**
```bash
cd src/PitchSync.Angular
ng test
```

**Run a single test class:**
```bash
dotnet test --filter "FullyQualifiedName~MatchRoomServiceTests"
```

## Pull Request Process

1. Branch from `main`: `git checkout -b feat/your-feature`
2. Write tests first (the project follows TDD)
3. Implement the feature
4. Run linting: `cd src/PitchSync.Angular && ng lint`
5. Ensure all tests pass: `dotnet test` and `ng test`
6. Open a PR targeting `main` — all CI checks must pass before merge
7. Write a clear PR description explaining what changed and why

## Project Structure

See [CLAUDE.md](CLAUDE.md) for a full architecture overview, service port map, and domain invariants.
