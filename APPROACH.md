
# Northwind Support Desk – Implementation Approach

## Overview

This document explains the implementation approach, key design
decisions, testing strategy, and trade-offs for the four tasks
completed in the Northwind Support Desk assessment.

The existing application uses a layered .NET 10 backend, an
Entity Framework Core database layer, a React/TypeScript frontend,
and SQL Server.

The work was completed incrementally, keeping existing
functionality and adding tests to verify changes.

## Task 1 – Defect Fix and Server-Side Filtering

### Problem identification

The ticket list filters were not consistently triggering new
requests when changed.

The root cause of DEFECT-117 was the dependency array in the
useTicketList React hook. It did not include all filter values.

Changing status, priority, category, or other filters updated
the frontend state without immediately fetching updated data.

### Approach

The missing dependencies were added so changing a filter
triggers the appropriate request.

The existing search debounce was retained to avoid sending
unnecessary requests on every keystroke.

On the backend, ticket filtering was applied through Entity
Framework Core IQueryable operations before counting and
pagination.

### Design decisions

Chosen:
- Correct the existing hook rather than replacing it.
- Apply filters in SQL Server rather than filtering returned pages.
- Combine supplied filter conditions using AND.
- Calculate total count after filtering but before pagination.

Rejected:
- Filtering only in React, because it produces incorrect results
  and counts when using server-side pagination.
- Removing search debounce, because that would generate excessive
  requests while typing.

### Verification

A regression test was added for filter changes.

Backend tests cover exact-match filtering, combined conditions,
and customer-name search.

## Task 2 – Ticket Escalation and Assignment

### Approach

Ticket creation was extended with automatic triage behavior.

Escalation and assignment logic were implemented in the backend,
with the frontend displaying the resulting state and history.

The application tracks ticket SLA status and escalation history.

### Design decisions

Chosen:
- Keep ticket business rules in the backend.
- Record escalation history rather than only updating a ticket.
- Display assignment, escalation, and SLA information in the UI.
- Enforce business rules through application logic.

Rejected:
- Performing triage only in the browser, because API clients
  could otherwise bypass the rules.
- Overwriting escalation state without history, because that
  would remove the audit trail.

### Verification

Backend tests were used to verify ticket-related business rules.

The frontend was tested to confirm that assignment and
escalation information could be displayed and used.

## Task 3 – Login and JWT Authentication

### Approach

Agent login was implemented using the existing agent model.

Passwords are stored as password hashes rather than plain text.

After successful authentication, the API issues a signed JWT.

Protected endpoints require bearer authentication, while
the login endpoint is publicly accessible.

The React frontend provides a login page, protected routes,
and logout functionality.

### Design decisions

Chosen:
- JWT bearer authentication for protected API endpoints.
- ASP.NET Core password hashing for stored credentials.
- Reject login attempts for inactive agents.
- Keep the JWT in frontend memory for the current session.
- Read the escalation actor from the authenticated JWT identity.

Rejected:
- Storing raw passwords.
- Trusting an actor name supplied by the browser.
- Adding refresh tokens, roles, MFA, or external identity
  providers, because these were outside the assessment scope.

### Verification

Unit tests cover successful login, invalid credentials,
inactive agents, token validation, and token expiry.

Manual tests confirmed that unauthenticated API requests
receive HTTP 401 and that logout protects frontend routes.

## Task 4 – Dockerised API

### Approach

The backend was containerised using a multi-stage Dockerfile.

The build stage uses the .NET 10 SDK. The final stage uses the
ASP.NET Core 10 runtime image.

Docker Compose runs SQL Server and the API together.

The React application remains outside Docker and runs using
the Vite development server.

### Design decisions

Chosen:
- Use multi-stage builds to exclude the SDK from the final image.
- Copy central configuration and project files before source
  code so Docker can cache NuGet restore.
- Exclude frontend files, dependencies, build outputs, and
  secrets through .dockerignore.
- Run the API as the built-in non-root .NET application user.
- Listen over HTTP on port 8080 inside the container.
- Publish the API on localhost:5080.
- Wait for SQL Server to become healthy before starting the API.
- Supply connection details, environment name, and JWT signing
  key through runtime environment variables.
- Provide a public database connectivity health endpoint.
- Keep an isolated Docker Compose configuration for testing
  initial database creation without deleting existing data.

Rejected:
- Dockerising the React frontend, because the task explicitly
  excludes it.
- Installing HTTPS certificates inside the API container,
  because the task requires HTTP.
- Baking database credentials or JWT secrets into the image.
- Running the API container as root.
- Deleting the main SQL Server volume to test initialization.

### Health endpoint

GET /health uses an ASP.NET Core health check backed by
Entity Framework Core database connectivity.

A successful database connection returns HTTP 200.
An unavailable database returns HTTP 503.

The health endpoint is accessible without a JWT.

### Development migrations and seeding

In Development, the API applies EF Core migrations and
initializes the demonstration data during startup.

For production, a separately controlled migration operation
would be preferable to automatically migrating on each API
startup. This supports review, backup, and deployment control.

### Verification performed

- Docker Compose started SQL Server and the API successfully.
- SQL Server reported healthy.
- The API was reachable on localhost:5080.
- GET /health returned HTTP 200 without a token.
- GET /api/tickets returned HTTP 401 without a token.
- `docker compose exec api whoami` returned `app`.
- The Docker build reused its cached NuGet restore step.
- React successfully connected to the Docker API through
  VITE_API_PROXY_TARGET=http://localhost:5080.
- Login and ticket listing worked through the local React app.

### Empty-volume verification

An isolated Docker Compose project named supportdesk-verify
was used on 15 October 2026.

It created a separate SQL Server volume and started the API
on localhost:5081.

The API successfully initialized the database and logged:

Seeded 5 categories, 6 customers, 6 agents and 40 tickets.

GET /health returned HTTP 200.

The temporary verification environment and volume were then
removed. The original database remained healthy and available.

### Image size comparison

Docker reported approximately 455 MB disk usage for the
final API image.

BuildKit cache inspection indicated approximate base sizes of
1.22 GB for the .NET SDK and 340 MB for ASP.NET Core runtime.

The base-image figures are estimates from cached layers,
not direct image-size measurements.

## Testing Strategy

The backend uses automated unit and architecture tests.
The frontend uses Vitest.

At the end of the Docker implementation, all 43 backend
tests passed.

Manual integration checks covered login, JWT protection,
database connectivity, ticket listing, and the Docker-to-Vite
connection.

## Known Limitations

The implementation focuses on the assessment requirements.

It does not include registration, password reset, roles,
OAuth/SSO, refresh tokens, MFA, notifications, CI/CD,
Kubernetes, or a production-grade secrets system.

Development credentials and automatic startup migrations are
not intended for production deployment.

## AI Assistance

AI assistance was used for planning, code suggestions,
troubleshooting, documentation drafting, and review.

Suggested changes were applied incrementally and validated
using local builds, automated tests, Docker output, and
manual application checks.
