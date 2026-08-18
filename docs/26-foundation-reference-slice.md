# 26 — Foundation Reference Slice

Owner: Architecture + backend  
Status: Implemented reference slice; review required  
Last reviewed: 2026-08-17

## Outcome

The first BOOKDOC2026 code slice establishes the target solution shape and proves the most important multi-tenant foundation controls before patient or clinical modules begin. It is a backend reference implementation, not a pilot-ready release.

Implemented behavior:

- public clinic application and platform-direct registration;
- platform-only application approval;
- atomic provisioning of tenant, organization, first branch and default branch configuration;
- ASP.NET Core Identity users/roles, role-claim permissions and durable tenant/organization/branch user scopes;
- branch administrator assignment through the seeded `ClinicAdministrator` role and a branch-bound durable scope;
- email/password login, scoped JWT access tokens, hashed rotating refresh tokens, revocation and replay rejection;
- tenant and branch scoped configuration reads and updates;
- optimistic version checks for approval and branch configuration;
- append-only audit events for security-relevant mutations;
- transactional outbox creation and leased, retryable Worker processing;
- API-hosted durable execution supplied by the `BookDoc2026.Worker` class library;
- SQL Server EF Core model, reviewed numeric-key baseline and `SdtsIdentityArchitecture` migrations;
- SDTS-style AppHost, ServiceDefaults, typed Client, shared Blazor and MAUI UI, Admin, Portal and MAUI project boundaries;
- standardized API success/error envelopes, health endpoint and development OpenAPI document;
- unit, architecture and integration tests.

## Project map

| Project | Responsibility |
|---|---|
| `BookDoc2026.Shared.Kernel` | Small shared claim, role and trusted platform vocabulary |
| `BookDoc2026.Domain` | Entities, state transitions, invariants and domain exceptions |
| `BookDoc2026.Contracts` | Version-neutral request/response records and permission constants |
| `BookDoc2026.Application` | Use-case orchestration and infrastructure ports |
| `BookDoc2026.Infrastructure` | EF Core, SQL Server/Identity stores, JWT sessions, audit persistence and outbox execution |
| `BookDoc2026.Client` | Typed authentication and API clients for every UI host |
| `BookDoc2026.Blazor.UI` | Shared UI primitives without backend-layer dependencies |
| `BookDoc2026.Api` | HTTP composition, bearer authentication, authorization, exception translation and health |
| `BookDoc2026.Worker` | Class library that registers immediate/scheduled durable services inside API |
| `BookDoc2026.Templates` | Scoped/versioned template contracts and strict HTML-safe rendering |
| `BookDoc2026.Messaging` | Template-driven channel dispatch and provider abstractions for API/Worker use |
| `BookDoc2026.DocumentService` | Neutral document generation with Open XML DOCX and PDF exporters |
| `BookDoc2026.ErrorHandling` | Platform-neutral error codes/categories, exception normalization, remote occurrences and recovery guidance |
| `BookDoc2026.Bootstrap` | Guarded one-time command for the first platform operator; credentials come from secrets/environment |
| `BookDoc2026.Admin` | Platform/tenant/Identity/role/report administration host skeleton |
| `BookDoc2026.Portal` | Permission-composed operational portal host skeleton |
| `BookDoc2026.Mobile` | MAUI Blazor Hybrid Portal-oriented mobile shell with shared Client and Blazor UI references |
| `BookDoc2026.Maui.UI` | Native reusable MAUI boundary and platform secure token store |
| `BookDoc2026.AppHost` | Development-only composition for API, Admin and Portal plus explicit-start MAUI Windows orchestration |
| `BookDoc2026.ServiceDefaults` | Shared health and service defaults |
| `BookDoc2026.UnitTests` | Domain invariant and state-transition tests |
| `BookDoc2026.ArchitectureTests` | Dependency-direction and host-separation tests |
| `BookDoc2026.IntegrationTests` | Persistence, authorization, isolation, API and Worker behavior tests |

The dependency direction is `Domain <- Application <- Infrastructure`, with Contracts and Shared Kernel at controlled boundaries. API is the backend composition and execution host. Admin, Portal and MAUI use Client/Contracts and do not reference backend layers. Worker is a library referenced by API and deliberately contains no API reference.

## Foundation endpoints

| Method and route | Authorization | Purpose |
|---|---|---|
| `POST /api/v1/tenant-applications` | Anonymous | Submit a clinic application for platform approval |
| `POST /api/v1/auth/login` | Anonymous | Validate credentials and select the default or requested durable clinic scope |
| `POST /api/v1/auth/refresh` | Refresh token | Rotate the refresh token and issue a new scope-bound session |
| `POST /api/v1/auth/revoke` | Authenticated | Revoke the authenticated user's refresh session |
| `POST /api/v1/platform/identity-users` | Platform operator + `Users.Manage` | Create an Identity user before assigning clinic access |
| `POST /api/v1/platform/tenant-applications` | Platform operator + `Tenants.Register` | Register an application directly from the control plane |
| `POST /api/v1/platform/tenant-applications/{id}/approve` | Platform operator + `Tenants.Approve` | Approve once and provision the initial tenant graph |
| `POST /api/v1/branches/{id}/administrators` | Branch scope + `Users.BranchAdministrators.Manage` | Add a branch administrator with allow-listed grants |
| `GET /api/v1/branches/{id}/configuration` | Branch scope + `Branches.View` | Read branch branding, numbering and communication identity |
| `PUT /api/v1/branches/{id}/configuration` | Branch scope + `Branches.Configuration.Manage` | Update configuration using the expected version |
| `GET /health` | Current development baseline | Process health probe |

Tenant identity comes from the trusted authenticated principal, never from a request body. Branch access requires both permission and durable branch scope. Repository queries and EF global filters provide additional tenant isolation; a forged branch claim from another tenant resolves as not found.

## Authentication boundary

The runtime foundation now uses ASP.NET Core Identity and bearer JWT. A login resolves an active user, roles and role-claim permissions, then selects an active durable `ApplicationUserScope` for a non-platform user. Access tokens contain the chosen Tenant/Organization/Branch scope. Random refresh tokens are stored as SHA-256 hashes, bound to that scope, rotated on every refresh and rejected after replay or expiry.

The header authentication handler remains only for local development and automated integration testing. It refuses authentication outside `Development` and `Testing`; deployed clients must use bearer sessions.

JWT issuer, audience and a signing key of at least 32 bytes must come from approved deployment configuration/secret management. No signing key or initial password is committed. Before a shared deployment, approve a one-time platform-operator bootstrap process, key rotation/revocation operations, account recovery, MFA/provider direction and reasoned support-elevation behavior.

## Database foundation

The `NumericKeyBaseline` migration creates schema-owned tenant applications and tenants; organizations, branches and branch configurations; append-only audit events; and outbox messages with attempts, leases and dead-letter state. The `SdtsIdentityArchitecture` migration removes the temporary `branch_user` and `branch_permission_grant` tables and creates Identity user, role, claims, login, role assignment, token and durable user-scope tables. It seeds the two foundation roles and their current permission claims.

Identity user/role properties use `uint` in CLR. SQL Server/EF stores unsigned values as `bigint`; tenant and business aggregates use application-generated non-identity `bigint`. Public contracts use protected strings as specified by [DOC-031](31-numeric-and-protected-identifier-reference.md).

Physical table and column names use snake case. Composite foreign keys preserve tenant and organization ownership down the organization/branch/user-scope graph. Deletes are restricted. The application additionally prevents writes for a tenant other than the current execution context.

Both migrations are source-controlled and have been applied successfully to the local development SQL Server database. EF reports no pending model changes. Applying them to any shared or production database still requires a reviewed target connection string and the migration approval process from document 08.

## Durable Worker behavior

Tenant approval writes its business data, audit record and `TenantApproved` outbox message in one unit of work. The Worker leases available messages, dispatches typed handlers, records attempts, marks success exactly once, schedules bounded retries, and dead-letters after the configured maximum. SignalR is not part of this durable execution path.

The first handler is intentionally provider-neutral. Email, WhatsApp, push, report delivery and other providers will be added behind typed handlers only after their vendor, consent, template, idempotency and callback decisions are approved.

## Local execution

Prerequisites are the .NET SDK pinned by `global.json` and SQL Server LocalDB or an approved SQL Server instance.

```powershell
dotnet restore BookDoc2026.slnx
dotnet ef database update --project src/BookDoc2026.Infrastructure --startup-project src/BookDoc2026.Api
dotnet run --project src/BookDoc2026.AppHost
```

Use `src/BookDoc2026.Api/BookDoc2026.Api.http` as a local request starting point. Do not copy development identity headers into deployed clients.

After migrations are applied, the first platform operator can be created once with configuration supplied through the environment. PowerShell maps double underscores to nested configuration keys:

```powershell
$env:Bootstrap__Confirmation = "CREATE_FIRST_PLATFORM_OPERATOR"
$env:Bootstrap__Email = "operator@example.invalid"
$env:Bootstrap__DisplayName = "Platform Operator"
$bootstrapPassword = Read-Host "Temporary password" -AsSecureString
$env:Bootstrap__Password = [System.Net.NetworkCredential]::new("", $bootstrapPassword).Password
dotnet run --project src/BookDoc2026.Bootstrap
Remove-Item Env:Bootstrap__Confirmation, Env:Bootstrap__Email, Env:Bootstrap__DisplayName, Env:Bootstrap__Password
$bootstrapPassword = $null
```

Use an operator-controlled address in a real environment, not the example value. The command refuses pending migrations, a second operator and elevation of an existing non-platform user. Remove the bootstrap configuration immediately after success and change/rotate the temporary credential through the approved account workflow.

## Verification evidence

After the SDTS architecture/Identity realignment, the solution baseline passes 50 automated tests:

- 20 unit tests for Foundation, Stakeholder/Patient, Catalog and numeric-ID invariants;
- 7 architecture tests for backend dependency direction, API-to-Worker-library composition and Client/shared-UI isolation;
- 23 integration tests for Foundation, Stakeholder/Patient, Catalog, Scheduling, protected IDs, Worker behavior and JWT sessions, including opt-in real SQL Server transaction tests.

Run the repeatable checks with:

```powershell
dotnet build BookDoc2026.slnx -c Release
dotnet test BookDoc2026.slnx -c Release --no-build
dotnet format BookDoc2026.slnx --verify-no-changes
dotnet list BookDoc2026.slnx package --vulnerable --include-transitive
```

## Deliberately deferred

This slice does not implement a production bootstrap/MFA or external identity-provider procedure, complete Admin/Portal/mobile experiences, mobile sign-in or offline behavior, staff employment/payroll, clinical records, confirmed appointments, queues, SignalR, provider messaging, reports, print agent, file storage, deployment automation, production observability or legacy data migration. Patient Registry, Resource Catalog and Scheduling hold/reference slices now exist separately; they are not pilot-complete workflows.

The next recommended business work is the Contract/Package rule matrix and aggregate foundation, followed by conversion of Scheduling holds into confirmed multi-resource Bookings. The legacy behavior and implementation order are controlled by [DOC-032](32-sdts-architecture-realignment-and-legacy-business-preservation.md). Clinical, billing, queue, communication, reporting and printing work remain governed by their owning plans.

## Acceptance gate for this slice

Foundation may be marked accepted when:

1. architecture and security owners review the project boundaries and migration;
2. product owners confirm clinic application, approval and branch-override behavior;
3. the Identity/JWT ADR, secret-backed platform-operator bootstrap, account recovery and production provider/MFA direction are approved;
4. tenant-isolation tests are repeated against SQL Server, not only the fast in-memory integration fixture;
5. deployment, secret management, backup/restore and monitoring designs are approved;
6. CI repeats formatting, build, tests, package advisory scanning and migration-script generation.

Until these items pass, the code is suitable for continued development and review but not real clinic or patient data.
