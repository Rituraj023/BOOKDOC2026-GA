# 21 — Deployment and Operational Readiness

## Environment model

- Local development with synthetic data through Aspire AppHost, which starts API, Admin and Portal; API hosts Worker-library services. AppHost also offers MAUI Windows as an explicit-start resource, while Android/iOS use normal emulator or device tooling.
- Shared integration environment for contracts, database migrations and vendor sandboxes.
- Staging mirroring production topology and security without production patient data.
- Production with API, Admin and Portal deployments; API instances execute immediate and scheduled Worker-library services.
- Optional training/demo environment with explicitly synthetic data and isolated messaging/payment providers.

Every environment has separate identity clients, databases, storage, encryption keys, provider credentials, domains, telemetry and feature settings. Production secrets or data never seed lower environments.

## Frontend exposure boundary

- Admin is an ERP-style restricted application. Production access requires an approved IPv4/IPv6 CIDR or private-network route at the firewall/reverse proxy and a matching fail-closed host policy.
- Only trusted proxies may supply forwarded-client-IP headers. The application must reject untrusted forwarding headers and record privacy-safe allow/deny audit evidence.
- The initial Admin allow-list, change approver, emergency access path, expiry/review cadence and VPN/private-access decision remain deployment inputs; no IP address is hard-coded in source.
- IP restriction never replaces JWT authentication, `Hosts.Admin.Access`, operation permission, durable scope, reauthentication or audit.
- Portal is internet-facing but requires authentication for protected data. Every API result remains permission-, tenant-, branch- and record-scope filtered.
- Mobile applications use the Portal/API exposure boundary and never connect to Admin-only routes merely because they share components.
- Admin imports use quarantine/staging, schema validation, preview, explicit approval, idempotency and rejection evidence before data reaches owning modules. Exports require permission, row/size bounds, safe spreadsheet encoding, secure expiry and audit.

## Multi-tenant operational controls

- automated tenant provisioning with unique immutable ID and approved region/plan;
- tenant lifecycle states: Provisioning, Active, Suspended, Closing, Archived;
- safe suspension that preserves clinical/emergency access policy and prevents unauthorized new activity;
- per-tenant quotas/limits that cannot cause cross-tenant data leakage;
- tenant-aware observability without patient data in metrics/logs;
- tested export/contract termination, retention, legal hold and deletion evidence;
- noisy-neighbour load and Worker fairness controls.

Initial capacity planning assumes one shared production deployment, 10 independent clinic tenants in year one and 50 by year three, normally fewer than 10 branches within a tenant and 1–20 staff users in a typical clinic. A normal clinic tenant may contain up to 100,000 registered patients and process about 100 appointments per day. The owner's multi-clinic tenant may contain roughly 20 times the normal data volume and process about 2,000 appointments per day across branches. Partitioning/indexing, migration, backup/restore, reporting and Worker-fairness tests must include both profiles. Concurrent peaks, messages, documents and report bursts remain required inputs; tests must exceed the approved forecast and include the disproportionately busy tenant.

Clinics may submit an onboarding form and platform operators may directly register an application, but activation is always platform-controlled. Application source, platform-managed dynamic document requirements, secured uploads, verification, approval/rejection, provisioning, first-owner activation, suspension and closure are distinct audited states. Approval evidence must not be placed in general logs, and a platform operator cannot use onboarding authority as clinical-data access.

Authorized branch administrators may activate allow-listed branding and numbering changes without BOOKDOC platform approval. Changes require durable branch scope, optimistic concurrency, immutable audit evidence and effective-version history. Provider verification or external approval may still be required before a domain, email, WhatsApp or SMS identity becomes usable; operations monitors verification failures and callbacks rather than approving routine branch configuration.

## Production readiness checklist

- infrastructure ownership, diagrams and approved capacity;
- CI/CD with reviewed immutable artifacts and migration compatibility gates;
- secrets/key/certificate rotation and emergency revocation;
- backups, point-in-time recovery and documented RPO/RTO restore tests;
- health checks, SLOs, alerts, on-call/escalation and status communication;
- multi-instance API duplicate-claim prevention and dead-letter runbooks;
- report/snapshot/storage capacity and retention jobs;
- incident, breach, downtime, vendor-outage and disaster-recovery exercises;
- support elevation and tenant communication workflow;
- rollback/forward-fix plan preserving clinical and financial writes;
- MAUI store/release, minimum-version and forced-update policy when mobile ships;
- local print-agent signing, update, revocation and unsupported-version policy.
- privacy-safe trace/metric/log correlation across API, Worker and provider callbacks, with owned alerts and runbooks;
- separate retention and integrity controls for diagnostic telemetry, security/operation audit and domain history;
- typed scoped configuration, managed secret references, key-rotation overlap and expired-feature-flag cleanup;
- durable provider-callback inbox, replay protection and reconciliation for ambiguous external results;
- software bill of materials, dependency/vulnerability evidence and signed immutable release artifacts;
- tested degraded behavior for database, identity, storage, messaging/payment provider, SignalR and printer outages.

The detailed ownership and acceptance rules for these additions are in [Cross-Cutting Platform Hardening](34-cross-cutting-platform-hardening.md).

## Release evidence

Each release records artifact/configuration hashes, migrations, feature flags, tests, known risks, data/report reconciliation, security/privacy approval, backup/restore status, rollback criteria, owners and go/no-go decision. Pilot releases also require clinic training, support coverage, legacy coexistence/cutover and daily reconciliation.
