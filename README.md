Northwind Support Desk
A support ticket management system for creating, searching, filtering, assigning, escalating, and resolving support tickets.
Stack: .NET 10 Web API, React 19 / TypeScript, SQL Server 2022. The code is maintained in one monorepo.
Assignment and scope
Full requirements are in [`ASSIGNMENT_CANDIDATE.md`](ASSIGNMENT_CANDIDATE.md). The work covers four tasks:
Task	Scope
1	Fix DEFECT-117 and implement server-side ticket filtering
2	Automatic ticket triage, escalation/history, assignment, and SLA status
3	Agent login, JWT-based API authentication, and protected React routes
4	Dockerise only the API alongside SQL Server for local deployment


The React frontend is deliberately not Dockerised. Kubernetes, Helm, registry publishing, CI/CD, HTTPS inside the API container, Docker secrets/vault integration, and multi-architecture builds are outside Task 4's scope.
Prerequisites
- Docker Desktop using Linux containers and Docker Compose
- .NET SDK 10 (see global.json) for local backend development
- Node.js compatible with the project configuration (see .nvmrc) and npm for the frontend
Commands below run from the repository root.
Quick start: Docker API + local React (recommended)
1. Copy the example configuration, then set a unique, sufficiently long JWT_SIGNING_KEY in your private .env file. The example value is not a secure signing key.
   PowerShell:
   Copy-Item .env.example .env
   Bash:
   cp .env.example .env
   If .env already exists, keep it and do not overwrite your configured JWT key or existing database password.
2. Start SQL Server and the API:
   docker compose up -d --build
   docker compose ps
   Data usage note: A first-time build may download large SQL Server, .NET SDK, and ASP.NET Core images, as well as NuGet dependencies. Subsequent builds can reuse cached layers. For an already-built image and to prevent image pulls, use docker compose up -d --no-build --pull never.
3. Check the public health endpoint:
   curl.exe -i http://localhost:5080/health
   A successful database check returns HTTP/1.1 200 OK and Healthy.
4. Start the local frontend in a separate PowerShell terminal:
   $env:VITE_API_PROXY_TARGET = "http://localhost:5080"
   npm run web
   Or, in Bash:
   VITE_API_PROXY_TARGET=http://localhost:5080 npm run web
   Open http://localhost:5173. Vite proxies API calls to the container on port 5080. Install the frontend dependencies with npm install if they are not yet available; this may download packages.
Service	Address
React development server (local, not Docker)	http://localhost:5173
Docker API	http://localhost:5080
Public database health check	http://localhost:5080/health
SQL Server (local development only)	localhost:1433


Docker Compose starts the API only after the SQL Server container reports healthy. Within Docker, the API connects to sqlserver,1433 by service name, not localhost.
Local API alternative (without Dockerising the API)
For normal source-code development, SQL Server can run in Docker while the API runs on the host:
npm run db:up
npm run api
npm run web
The host-run API typically serves https://localhost:7043 and http://localhost:5043, with Swagger at https://localhost:7043/swagger in Development. Stop the Docker API if host port configurations conflict. If using Visual Studio, open SupportDesk.sln and run SupportDesk.Presentation with the appropriate launch profile.
Configuration and credentials
- MSSQL_SA_PASSWORD: SQL Server development SA password. Use the password already associated with an existing SQL Server volume; changing an environment variable does not reset the password inside an initialized database.
- JWT_SIGNING_KEY: read by Docker Compose and passed to ASP.NET Core as Jwt__SigningKey.
- ASPNETCORE_ENVIRONMENT: supplied at runtime; Development enables development behavior, including the project's startup migration and demo seeding.
- ConnectionStrings__Default: for a host-run API; Docker Compose supplies a container-network connection string whose host is sqlserver.
- Jwt__Issuer, Jwt__Audience, Jwt__TokenLifetimeMinutes: JWT settings for local development.
Never commit .env, production secrets, or real passwords. .env is Git-ignored; .env.example contains documentation and placeholders, not working production credentials. A local host-run API may use .NET user-secrets for the JWT signing key.
Demo account: The development seed provides agent accounts with the demo password DevPassword123! (including an active Alex Turner account). Use the exact seeded email from SupportDbSeeder.cs for the login username. Inactive agents cannot sign in. These demo credentials are for development only.
Tests
npm test
This runs backend and frontend tests. Backend tests can also be run with:
dotnet test
On 15 October 2026, the backend build succeeded and 43 of 43 backend tests passed, with no failures. The prior frontend test run passed 12 tests. Unit and architecture tests do not require a running SQL Server instance.
API overview
Endpoint	Purpose	Authentication
GET /health	Checks database reachability	Public
POST /api/auth/login	Agent login; issues a JWT	Public
GET /api/tickets	Paged, server-side filtered tickets	Bearer JWT
GET /api/tickets/{id}	Ticket details	Bearer JWT
POST /api/tickets	Create ticket with automatic triage	Bearer JWT
PATCH /api/tickets/{id}/status	Change ticket status	Bearer JWT
PATCH /api/tickets/{id}/assignment	Change assignee	Bearer JWT
GET /api/customers, /api/agents, /api/categories	Reference data	Bearer JWT


The ticket-list query supports search, status, priority, categoryId, customerId, assignedAgentId, unassignedOnly, sortBy, and sortDirection. Filters are applied in SQL before counting and paging; the older statement that filters are “accepted but not applied yet” is no longer true.
Ticket escalation and escalation-history operations are also implemented. The actor recorded for an escalation is taken from the authenticated identity rather than a browser-supplied name.
Errors use ASP.NET Core problem details for validation, not-found, and business-rule failures where applicable.
Demo seed data
A fresh development database is seeded with 5 categories, 6 customers, 6 agents, and 40 tickets covering multiple priorities, statuses, and SLA states. Existing seed data is preserved on subsequent starts.
Candidate work — Task 1
DEFECT-117: filters sometimes did not take effect
Root cause: In the frontend useTicketList hook, the useEffect dependency list included filters.search, filters.page, and filters.pageSize, but omitted the other filter fields. Updating status, priority, category, and similar fields changed React state without triggering a fresh API request. A later search change could appear to “fix” it because that request included the previously changed fields.
Fix: Include all request-relevant filter fields in the effect dependencies, preserving the 300 ms search debounce.
Verification: The regression test for useTicketList checks that changing status triggers a new request. Manual tests confirmed other filter changes cause requests and search remains debounced.
Server-side filtering
Filters are composed on IQueryable before CountAsync, Skip, and Take. Supplied filters combine using AND; search checks ticket title, reference, or customer name.
Filter	Behavior
status, priority, categoryId, customerId, assignedAgentId	Exact match
unassignedOnly	AssignedAgentId == null
search	Trimmed text search over title, reference, or customer name


TicketQueriesTests.cs covers priority filtering, combined priority/customer filtering, and customer-name searching. Applying filters before counting ensures that pagination totals describe the filtered result set.
Candidate work — Task 2
Ticket creation performs automatic triage/assignment according to the application's business rules. Agents can escalate tickets; escalation history and SLA status are available through the API and UI. The escalation actor is identified from the JWT-authenticated agent, rather than trusting a client-submitted identity. Tests and browser checks were performed during Task 2 and the subsequent authentication integration.
Candidate work — Task 3
Agents can log in using their seeded email and password. Password hashes are stored using ASP.NET Core's password hasher; inactive agents are denied access. Successful login issues an HMAC-SHA256 JWT with issuer, audience, and expiration checks. Ticket and reference-data endpoints require Bearer authentication, while login remains public.
The React application uses an authentication context, protected routes, and a logout action. Its JWT is held in memory, so reloading the page requires signing in again; no refresh-token system is implemented. The token signing key is supplied through environment variables or user-secrets, not committed to the repository.
Candidate work — Task 4: Dockerised API
Design and requirements (DK-1 to DK-8)
ID	Implementation
DK-1	apps/api/Dockerfile builds from the repository root; multi-stage .NET 10 SDK build and ASP.NET Core 10 runtime. Project files, Directory.Build.props, Directory.Packages.props, and global.json are copied before restore; source follows afterward.
DK-2	Root .dockerignore excludes .git, .env, build outputs (bin/obj), node_modules, and the entire apps/web/ directory.
DK-3	Runtime stage uses USER $APP_UID and HTTP port 8080; no development certificate is copied into the image.
DK-4	The connection string, JWT signing key, and ASP.NET Core environment are runtime configuration passed by Compose. Variables are documented in .env.example.
DK-5	The api Compose service publishes 127.0.0.1:5080:8080, connects to SQL Server using sqlserver, and depends on SQL Server's healthy state.
DK-6	In Development, startup applies EF Core migrations and seeds demo records on an empty database.
DK-7	Centrally registered ASP.NET Core health check maps GET /health, anonymously, and uses EF Core's CanConnectAsync to check SQL Server reachability.
DK-8	The local Vite application connects with VITE_API_PROXY_TARGET=http://localhost:5080 npm run web (Bash), or the equivalent PowerShell environment variable.


Migrations in development versus production
For this local Development exercise, migrations and demo seeding run when the API starts so a new Docker volume becomes usable automatically.
In a real production deployment, migrations should be applied as a separately approved, one-time deployment operation (for example, a reviewed SQL script or migration job) before deploying the new API version. This avoids multiple application instances racing to modify a schema and enables backups, review, rollback planning, and controlled permissions. Automatic demo seeding would be disabled outside Development.
Destructive command warning: docker compose down -v deletes the normal project's named database volume and its stored data. Never run it against a database you need to retain.
Verification performed — 15 October 2026
Test	Command / evidence	Result
Compose configuration	docker compose config --quiet and docker compose config --services	Valid; sqlserver and api only
Startup dependency	docker compose ps	SQL Server healthy; API running
Host port	docker compose ps	API at 127.0.0.1:5080->8080/tcp
Non-root runtime	docker compose exec api whoami	app, not root
Database health	curl.exe -i http://localhost:5080/health	HTTP 200, body Healthy, without JWT
Protected tickets	curl.exe -i http://localhost:5080/api/tickets	HTTP 401 with WWW-Authenticate: Bearer
Login and tickets	Sign in through local React against port 5080	Successful login and ticket listing
Restore cache	docker compose --progress plain build api after source edits	RUN dotnet restore ... reported CACHED
Empty-database migrations and seed	Isolated Compose project, described below	/health healthy; seeded 5 categories, 6 customers, 6 agents, 40 tickets
Cleanup / original database preserved	Verification project removed; original Compose services checked again	Original SQL Server healthy and /health still HTTP 200


Reproducing the empty-volume test safely
The empty-volume verification was performed using a separate docker-compose.verify.yml file and an isolated Compose project. The verification configuration uses a different named volume and publishes the SQL Server/API on ports 1434/5081, avoiding conflicts with the normal ports 1433/5080.
docker compose -p supportdesk-verify -f docker-compose.verify.yml config --quiet
docker compose -p supportdesk-verify -f docker-compose.verify.yml up -d --no-build --pull never
curl.exe -i http://localhost:5081/health
docker compose -p supportdesk-verify -f docker-compose.verify.yml logs api --tail 80
The logs on 15 October 2026 showed: Seeded 5 categories, 6 customers, 6 agents and 40 tickets. The verification health endpoint returned HTTP 200. Afterward the temporary containers, network, and test database volume were removed using:
docker compose -p supportdesk-verify -f docker-compose.verify.yml down -v
Do not omit -p supportdesk-verify -f docker-compose.verify.yml when running that cleanup command. The ordinary project and existing database volume were left intact; after cleanup, http://localhost:5080/health still returned HTTP 200.
Checking the cached NuGet restore layer
The Dockerfile copies the solution's central build/package files and the four .csproj files before the dotnet restore instruction. It copies the .cs sources only after restoring. Rebuilding following source changes reused the restore cache:
[build 10/12] RUN dotnet restore apps/api/src/SupportDesk.Presentation/SupportDesk.Presentation.csproj
CACHED
To repeat the test, make a harmless .cs change without modifying .csproj or central package configuration, then execute:
docker compose --progress plain build api
A build may still query registries for metadata or download missing packages, particularly on a new machine.
Image size comparison
Observed locally on 15 October 2026, using docker image ls -a and docker buildx du --verbose:
Image	Size	Measurement
Northwind Support Desk final API image	455 MB	Docker-reported disk usage (docker image ls)
.NET 10 SDK base image	approximately 1.22 GB	Estimate from BuildKit cached base-image layers
ASP.NET Core 10 runtime base image	approximately 340 MB	Estimate from BuildKit cached base-image layers


### Repeating the isolated database verification

To test initialization without deleting the main SQL Server
database, use the separate verification Compose file:

```powershell
docker compose -p supportdesk-verify -f docker-compose.verify.yml up -d --no-build --pull never
```

Verify the test API:

```powershell
curl.exe -i http://localhost:5081/health
docker compose -p supportdesk-verify -f docker-compose.verify.yml logs api --tail 40
```

Remove the temporary environment afterwards:

```powershell
docker compose -p supportdesk-verify -f docker-compose.verify.yml down -v
```

**Warning:** The final command deletes the temporary
`supportdesk-verify` database volume, not the main project volume.








The Docker CLI displayed 125 MB content size for the API image; this is not the same measure as its 455 MB disk usage. The SDK and runtime base images were stored as BuildKit references and were not shown under their original tags by docker image ls. Their layer-derived estimates should not be presented as direct docker image ls measurements or assumed to be exact. The important architectural point is that the final application image uses the ASP.NET Core runtime and does not include the SDK build toolchain.
Task 4 verification checklist
- [x] docker compose up starts SQL Server and API
- [x] SQL Server reports healthy before the API starts
- [x] API is published on localhost port 5080
- [x] GET /health returns HTTP 200 without a JWT
- [x] GET /api/tickets returns HTTP 401 without a JWT
- [x] Development login and authenticated ticket listing work
- [x] Fresh database volume is migrated and seeded (isolated test)
- [x] NuGet restore layer was reused after changing source
- [x] API runtime user is app, not root
- [x] Runtime-image size and approximate SDK base-image size recorded
- [x] Locally run React frontend works with VITE_API_PROXY_TARGET
- [x] Temporary test volume removed; existing database unaffected
Design decisions and limitations
- Chosen: Multi-stage Docker build and runtime-only deployment image. Rejected: shipping the SDK in the production/runtime container because it increases size and attack surface.
- Chosen: Docker health-gated database startup. Rejected: relying only on startup order, which does not prove SQL Server is accepting queries.
- Chosen: Custom EF Core IHealthCheck using CanConnectAsync. Rejected: adding a separate EF Core health-check package solely for one endpoint; the custom check reuses already available dependencies.
- Chosen: Runtime environment variables and a Git-ignored local .env. Rejected: putting a JWT key, database password, or certificate inside the Docker image.
- Chosen: Keep Vite local. Rejected: Dockerising React, because Task 4 explicitly excludes it.
- Chosen: Isolated temporary volume for empty-database verification. Rejected: deleting the active development volume using docker compose down -v.
Known limitations: This is a local assessment/development deployment, not a production-hardened system. Data Protection keys in the API container are not configured for durable protected storage; production environments would also need suitable TLS termination, credential management, monitoring, backup/recovery procedures, and separately managed migrations. Registration, password reset, role-based access, OAuth/SSO, refresh tokens, MFA, notifications, and a full CI/CD pipeline are not implemented.
Troubleshooting
Symptom	What to check
API cannot reach SQL Server	docker compose ps and docker compose logs api --tail 60; check SQL health and .env database password
/health returns 503	SQL Server connectivity, health status, and connection configuration
GET /api/tickets returns 401	Expected without a Bearer JWT; sign in first
Browser login or tickets fail	Start Vite with VITE_API_PROXY_TARGET=http://localhost:5080, and confirm API is listening on port 5080
Port 1433 or 5080 already in use	Stop the conflicting local service/container before starting Compose
Development HTTPS certificate warning (host-run API)	Use dotnet dev-certs https --trust or use the Vite proxy
SQL Server performance problems	Allow enough Docker Desktop memory, CPU, and disk capacity


AI assistance and submission
AI assistance was used for planning, implementation guidance, reviewing code/configuration, and preparing documentation. Builds, test results, and Docker behavior reported above were verified with actual local commands. Reviewers should assess the repository and commit history as the source of truth.
An accompanying APPROACH.md should explain the work, alternatives, and engineering decisions for Tasks 1–4. The assignment asks for separate commits, the fork URL, development login credentials, and a rough estimate of time spent; actual hours should be supplied by the candidate rather than invented in this README.