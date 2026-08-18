# 11 — Backend Workers, Reporting and Printing

Status: API-hosted Worker plus Messaging/Templates/DocumentService foundations implemented; reporting/printing implementation deferred  
Reviewed: 2026-08-18  
Scope: architecture and migration control

## Purpose

Define how BOOKDOC2026 replaces the legacy Windows messaging service and WinForms/RDLC reporting application. The API-hosted Worker, Messaging, Templates and DocumentService library foundations now exist; provider adapters, persisted template administration, schedules, report engines, printing and production deployment remain governed by the migration controls and acceptance gates in this document.

## Legacy evidence

### Windows messaging service

The legacy `BookDocMessagingService` is a timer-driven Windows Service that:

- polls message records directly from the BookDoc database;
- loads file-based templates and database bookmark values;
- sends email through SMTP;
- sends SMS through configured provider URLs;
- sends WhatsApp messages with a bearer-token provider call;
- executes repeat actions represented by stored-procedure/SQL strings;
- changes message state for sent, missing, invalid or retryable work;
- reschedules daily or failed messages;
- checks network/provider availability;
- cleans archived log files.

Useful requirements are durable queueing, scheduled execution, template variables, multiple channels, provider status, retry and operational visibility. The Windows Service implementation, direct database access, file-template coupling, arbitrary SQL execution, timer concurrency, embedded provider configuration and local log cleanup must not be ported.

### WinForms/RDLC reporting

The legacy reporting solution contains:

- 88 RDLC files: 45 primary layouts and 43 export duplicates or variants;
- Microsoft ReportViewer preview and local rendering;
- PDF and Excel export;
- WinForms filter/report screens for appointments, cancellations, customers, visits, events, monthly summaries, payments and requests;
- invoice, prescription, receipt and operational report layouts;
- report data supplied through repositories and stored-procedure result models.

Primary layout count by family:

| Family | Primary layouts |
|---|---:|
| Appointment | 7 |
| Cancellation | 6 |
| Category/payment overview | 5 |
| Customer/visit | 6 |
| Event/reminder | 5 |
| Monthly | 6 |
| Payment | 4 |
| Request | 4 |
| Invoice | 1 |
| Prescription | 1 |
| **Total** | **45** |

The counts show implementation inventory, not a commitment to rebuild every layout. Current users must validate report purpose, filters, formulas, output and frequency.

## Target backend-processing boundary

No traditional Windows business service remains in the target architecture.

### API responsibilities

The API owns:

- authenticated, versioned commands and queries;
- permission and durable tenant/organization/branch validation;
- enqueueing messages, report executions, report schedules and print jobs;
- Admin endpoints for monitoring, retry, cancellation, dead-letter handling and run-now;
- authorized download/status projections;
- SignalR publication of minimal status invalidations.

Controllers do not render large reports, contact delivery providers, execute scheduled work or poll database queues.

### Worker-library responsibilities

The `BookDoc2026.Worker` class library supplies API-hosted services for:

- message dispatch for in-app, email, SMS, WhatsApp and push;
- scheduled/recurring job execution;
- report rendering and export;
- scheduled-report delivery;
- expired artifact, orphan and retention cleanup;
- provider webhook follow-up or reconciliation jobs where needed;
- durable retry, lease, backoff and dead-letter behavior.

The Worker library references Application and Infrastructure. The API references the Worker library, registers its hosted services and is the only runtime process. Background services do not call API endpoints over HTTP to execute internal use cases.

### Messaging, template and document-service libraries

- `BookDoc2026.Templates` owns tenant/organization/branch-aware selection contracts, channel/culture/version identity and strict placeholder rendering. HTML values are encoded by default and missing required values fail before delivery.
- `BookDoc2026.Messaging` references Templates and owns message requests, idempotency propagation, rendered envelopes, provider contracts and channel selection for email, SMS, WhatsApp and push.
- `BookDoc2026.DocumentService` owns a neutral document model and exporters. The current implementations produce Open XML DOCX and PDF with safe filenames and SHA-256 metadata.
- API composes all three libraries. None is a process, controller host, database owner or replacement for Worker durability.
- Worker now invokes Messaging from the typed `Foundation.TenantApproved.v1` handler as the first reference path; future message/document handlers still require their own approved event and persistence design. SignalR is not part of this delivery path.
- Infrastructure now replaces the safe empty template-catalog default with a database-backed published-template catalog and supplies a Development/Testing email adapter. Concrete production provider adapters remain separate and must obtain secrets from approved configuration/secret storage.
- Append-only delivery attempts, stable outbox error codes and the permission-scoped status query are documented in [Durable Messaging Reference Slice](36-durable-messaging-reference-slice.md).

No legacy HTML files were copied. Their business purpose and wording must be inventoried, deduplicated, reviewed and migrated into the versioned catalog without embedded credentials or arbitrary SQL bookmarks.

### API-hosted execution decision

- Development: Aspire AppHost starts API, Admin and Portal. The API registers Worker-library services.
- Production: the API remains the only backend process and executes both immediate and scheduled durable work.
- A deployment may run multiple API instances; database leases ensure only one instance claims a given job.
- Idempotent handlers remain mandatory because process restarts, retries and multiple API instances cannot guarantee exactly-once external side effects.

### Legacy behavior disposition

| Legacy behavior | Target disposition |
|---|---|
| Database polling | Replace with durable queue/outbox claims and leases |
| SMTP email | Adapt through `IMessageDeliveryProvider` |
| SMS provider URL | Replace with authenticated provider adapter |
| WhatsApp bearer call | Replace with approved WhatsApp Business provider adapter and template IDs |
| File templates/bookmarks | Replace with versioned database/template registry and allow-listed variables |
| Repeat SQL/stored-procedure string | Replace with named typed job handler; arbitrary execution prohibited |
| Timer retry | Replace with persisted attempts, next-attempt time and exponential backoff |
| Local service logs | Replace with centralized structured telemetry and retention policy |
| Network ping checks | Replace with provider health checks and actual delivery-result handling |

## Reporting ownership

Admin and Portal use one backend catalog. Visibility is based on host, report permission and durable scope; duplicating report implementation across hosts is prohibited.

Admin network/IP restriction is defense in depth, not identity. It is enforced at the trusted reverse proxy/firewall and verified by host middleware using only configured trusted-forwarder headers. Authentication, host permission, report/import/export permission and durable data scope still apply to every operation.

### Admin

Admin contains:

- an ERP-style shell available only through approved network/IP routes in addition to authentication and permissions;
- report catalog and management center;
- management, finance, audit, security and cross-branch reports when permitted;
- execution history, failures, schedules and retained snapshots;
- Admin-only approved report scheduling;
- report/template version information;
- print-agent pairing, printers, health and failed print jobs.
- managed import definitions, validation previews, controlled execution, rejection files and audit history.

### Portal

Portal is internet-facing after authentication and contains permission-approved operational reports such as:

- daily booking/resource schedule;
- arrival, wait-time and service-point queue reports;
- practitioner or department worklists;
- patient visit/clinical summaries where clinically permitted;
- X-ray/CT/MRI operational worklists and verified-report status;
- cashier collection and branch operational reports;
- direct invoice, receipt, prescription, label and encounter-document output.

Portal does not receive a weaker authorization path. Patient-facing output is restricted to the authenticated patient's own released records.

### Proposed permission catalog

- `Reports.ViewCatalog`
- `Reports.Run`
- `Reports.ExportPdf`
- `Reports.ExportSpreadsheet`
- `Reports.ViewSensitiveClinical`
- `Reports.ViewFinancial`
- `Reports.ViewCrossBranch`
- `Reports.Schedule`
- `Reports.ViewHistory`
- `Reports.DownloadSnapshot`
- `Printing.Submit`
- `Printing.Reprint`
- `Printing.ManageAgents`

Report permissions do not replace underlying data permissions. Both checks are required.

## Report execution and rendering

### Planned interfaces

- `IReportCatalog` resolves authorized definitions.
- `IReportDataProvider` produces typed, scope-filtered report data.
- `IReportRenderer` produces HTML preview and PDF.
- `IReportExporter` produces XLSX or CSV.
- `IReportSnapshotService` preserves immutable issued output.
- `IPrintJobDispatcher` creates and monitors controlled print jobs.

### Planned API contracts

- `ReportDefinitionDto`
- `ReportParameterDefinitionDto`
- `CreateReportExecutionRequest`
- `ReportExecutionDto`
- `ReportSnapshotDto`
- `ReportScheduleDto`
- `CreatePrintJobRequest`
- `PrintJobDto`
- `PrintAgentDto`

### Planned endpoints

- `GET /api/v1/reports`
- `GET /api/v1/reports/{code}/parameters`
- `POST /api/v1/report-executions`
- `GET /api/v1/report-executions/{id}`
- `GET /api/v1/report-executions/{id}/content`
- `POST /api/v1/report-executions/{id}/cancel`
- Admin-only CRUD/run-now under `/api/v1/report-schedules`
- `POST /api/v1/print-jobs`
- `GET /api/v1/print-jobs/{id}`
- Admin-only pairing/revocation/health under `/api/v1/print-agents`

Large and scheduled reports return an execution identifier and run through an API-hosted Worker-library service. SignalR may tell a permitted client that status changed, but the client retrieves authoritative status/content through the API.

### Rendering decision

- Server-side Razor/HTML templates provide browser preview.
- Playwright Chromium renders PDF from the same HTML/CSS layout.
- ClosedXML produces XLSX; a native exporter produces CSV.
- Report parameters are typed, allow-listed and bounded.
- Module-owned data providers prevent cross-module table queries.
- No arbitrary runtime report scripting or unrestricted user SQL is allowed.

## Report retention

Operational reports are regenerated and temporary artifacts expire automatically.

Immutable snapshots are retained for:

- signed encounter/clinical summaries;
- issued prescriptions;
- issued invoices and receipts;
- officially distributed or legally retained reports.

Each snapshot records report/template version, source aggregate version or as-of time, generation actor/time, scope, content type, size and SHA-256 hash. The shared document-storage abstraction holds the binary.

## Admin report scheduling

- Only users with Admin host access and `Reports.Schedule` may create schedules initially.
- A schedule references an approved report definition/version, validated parameters, format, scope, time zone, recipients and delivery method.
- Authorization and recipient eligibility are re-evaluated at execution time.
- Delivery is through in-app notification or a secure expiring email link; large or sensitive files are not attached by default.
- Disabled users, revoked scope, retired templates or invalid recipients stop delivery and create an actionable failure.
- Portal scheduling is deferred; Portal reports are on demand.

## Printing boundary

### Normal printing

Admin and Portal use browser preview/download/print for normal reports and documents.

### Local print agent

A future small user-level Windows agent supports silent operational printing only for:

- payment receipts;
- queue/token slips;
- patient or specimen labels.

It is a constrained printer bridge, not a replacement Windows business service. It contains no database credentials, report formulas, message delivery or clinical rules.

The agent:

- pairs with one authorized tenant/branch/workstation;
- discovers and exposes an allow-listed printer set and capabilities;
- communicates outbound through HTTPS/SignalR;
- pulls a short-lived authorized document payload;
- validates document hash, job ID, copy count and media requirements;
- acknowledges claim, print result and error idempotently;
- removes protected temporary content after completion/expiry;
- supports explicit permission-controlled reprint rather than accidental replay.

Admin shows paired agents, printers, last seen, branch, revocation, queued/failed jobs and error reasons.

## Planned database concepts

| Schema/table | Purpose |
|---|---|
| `reporting.report_definition` | code, version, owner module, hosts, parameters, formats and required permissions |
| `reporting.report_execution` | request, scope, format, status, timing, error and temporary output reference |
| `reporting.report_schedule` | Admin-owned schedule, parameters, recipients, time zone and state |
| `reporting.report_snapshot` | immutable retained artifact metadata and hash |
| `printing.agent` | paired workstation identity, branch, status and last seen |
| `printing.printer` | agent printer name/capabilities and allow-list state |
| `printing.print_job` | document reference, target, copies, media, state and idempotency key |
| `printing.print_job_event` | append-only claim/print/failure/reprint history |

Sensitive report parameters, destinations and document contents are not written to general application logs.

## Migration sequence

1. Inventory legacy Windows-service message types, templates, providers, retry states and repeat tasks.
2. Map each repeat task to a named target job or approved retirement.
3. Build a report catalog with owner, users, inputs, formula, output, frequency and access class for all 45 primary layouts and link all 43 export variants.
4. Classify each report as `Rebuild`, `Merge`, `Replace` or `Retire`; record owner approval.
5. Create golden anonymized datasets and expected totals/layout evidence.
6. Plan migration order: issued documents; booking/cancellation/request; payment/visit; queue/imaging/resource; monthly/management/audit.
7. Run legacy and target dispatch/reporting in controlled parallel with duplicate delivery disabled.
8. Prove message, report and operational print parity.
9. Make the WinForms report application and Windows messaging service read-only/disabled through an approved runbook.
10. Retain legacy database/report access according to audit and retention policy.

## Testing and acceptance plan

### API-hosted background jobs

- lease and claim concurrency across multiple instances;
- restart during provider call or report rendering;
- idempotent retry and duplicate webhook/result handling;
- exponential backoff, maximum attempts and dead-letter replay;
- API startup registration and multi-instance claim behavior;
- provider outage and recovery;
- secret and message-body log redaction.

### Reporting

- Admin/Portal/role/scope authorization matrix;
- forged report code, branch, patient ID and filter rejection;
- parameter date/page/row/complexity limits;
- golden row count, formula, money and time-zone comparisons;
- PDF visual regression and accessibility checks;
- XLSX/CSV values and spreadsheet-formula injection protection;
- snapshot reproducibility after source/template change;
- schedule reauthorization and secure download expiry.

### Printing

- agent pairing, expiration and revocation;
- wrong-tenant/branch/printer rejection;
- offline agent, printer unavailable, paper/media error and recovery;
- duplicate delivery suppression and explicit reprint permission;
- document hash mismatch and expired payload rejection;
- protected temporary-file cleanup and audit evidence.

### Retirement gates

- every active service message type has a successful target path;
- every active report has accepted data/formula/output evidence;
- required receipt/slip/label workflows pass at pilot printers;
- two controlled parallel runs complete without duplicate external delivery;
- support, monitoring, rollback/forward-fix and retention runbooks are approved.

## Principal risks and controls

| Risk | Planned control |
|---|---|
| Hidden repeat-job SQL contains unrecorded business rules | live task inventory, named owner, typed-handler specification and shadow-result comparison |
| Legacy and target workers both deliver the same message | mutually exclusive task/channel ownership, idempotency keys and reconciled parallel runs |
| A visually similar report changes totals or row membership | fixed golden datasets, formula/rounding/time-zone comparisons and owner acceptance |
| Unused-report assumptions cause premature retirement | usage evidence plus explicit owner approval and retention review |
| Export or schedule bypasses normal data scope | host permission + report permission + durable scope checks at request and execution time |
| Sensitive scheduled output reaches an invalid recipient | execution-time reauthorization, secure expiring links and no large/sensitive attachment by default |
| Retained artifacts grow without control | selective immutable snapshots, temporary-artifact expiry and documented retention jobs |
| Local agent prints to the wrong branch or repeats output | branch-bound pairing, allow-listed printers, short claims, payload hashes and idempotent acknowledgements |
| SignalR is mistaken for a work queue | durable database state is authoritative; SignalR carries status invalidations only |
| Multiple API instances claim the same job | transactional leases, concurrency tokens, idempotent handlers and integration checks |

## Effort impact

| Epic | Estimate |
|---|---:|
| API-hosted Worker library and Windows-service replacement | 100–160 hours |
| Reporting platform and Admin/Portal report centers | 180–300 hours |
| Active legacy-report reconstruction | 360–675 hours provisional |
| Secured local print agent | 140–240 hours |
| **Total additional planning ROM** | **780–1,375 hours** |

The report reconstruction range assumes active-report validation and duplicate merging. Re-estimate after classification. Employee/payroll reports and full hospital reporting are excluded until their source rules are supplied.

## Explicit exclusions

- No code scaffolding, report conversion, database migration or package selection occurs through this plan.
- No runtime report designer or arbitrary SQL report builder is planned.
- The print agent does not send messages, render reports, query the database or execute business jobs.
- Portal report scheduling is deferred.
- Employee/payroll reporting is deferred until its database is reviewed.
