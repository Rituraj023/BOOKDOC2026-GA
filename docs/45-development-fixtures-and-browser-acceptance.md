# 45 — Development Fixtures and Browser Acceptance

Owner: Portal + shared UI + Queue + Identity + QA  
Status: Verified local browser reference journey  
Reviewed: 2026-08-23

## Outcome

This slice proves the first complete role-separated Portal journey against the real local API and SQL Server database:

```text
Reception session
  -> permission-composed dashboard
  -> branch-scoped patient search
  -> imaging destination selection
  -> idempotent queue check-in
  -> privacy-safe token

Technician session A
  -> permission-composed dashboard
  -> worklist
  -> Call -> Preparation -> In Service -> Completed

Technician session B
  -> SignalR invalidation
  -> authoritative API refresh after every transition
  -> safe display containing token/status only
```

The work uses synthetic development data. No clinic, production or patient data was used or retained.

## Explicit development fixture command

`BookDoc2026.DevelopmentFixtures` is a command project, not application-startup seeding. It creates one fixed local test tenant, organization, branch, X-ray service point, synthetic patient and two narrowly permissioned users:

- reception: `Patients.Search` and `Queues.CheckIn`;
- technician: `Queues.View`, `Queues.Call`, `Queues.Progress`, `Queues.Cancel` and `Queues.Display.View`.

The command fails closed unless all of these conditions hold:

1. `DOTNET_ENVIRONMENT` is `Development`;
2. the exact confirmation phrase is supplied;
3. `Fixture:Action` is explicitly `create` or `remove`;
4. create receives a temporary password from external configuration;
5. the configured database name ends in `_Dev`;
6. the database is reachable and has no pending migration.

Create refuses to overwrite an existing fixture. Removal targets only the fixed fixture slug, emails and role names, deletes dependent tenant and Identity rows in one retry-aware transaction, and is idempotent. The temporary users, roles, queue history and synthetic tenant were removed after validation; a second removal completed successfully with nothing present.

No fixture password, access token, signing key, provider credential or real endpoint secret is committed. Operators must provide the temporary password through local external configuration and run removal after validation.

The same guarded command was later extended with assigned/unassigned clinician identities, Practitioner/credential/assignment truth and one resource-allocated clinical Booking. Its clinical create/use/removal evidence is recorded separately in [DOC-054](54-clinical-development-fixtures-and-browser-acceptance.md); the queue evidence and counts below remain the historical DOC-045 checkpoint.

The current technician fixture permission composition is updated in [DOC-056](56-radiology-technician-ordered-worklist.md): `Investigations.Worklist.View` replaces the generic `Queues.View` requirement for the technician Portal projection, while call/progress/cancel/display capabilities remain independently granted. [DOC-059](59-portal-radiology-execution-quality-workspace.md) further adds Study view/start/acquisition permissions to the eligible operator, a separate quality-review-only role/Practitioner chain, matching X-ray equipment and deliberately non-matching CT equipment. The earlier list above remains the historical permissions used for this DOC-045 browser run.

## Browser defects discovered and corrected

Real browser execution found defects that controller and state-model tests could not reveal:

1. The fixture host composed Infrastructure without the shared Messaging dependency required by validated outbox handlers. The command now composes Messaging normally.
2. SQL retry execution rejected a manually opened transaction. Fixture create/remove now execute their complete transaction through EF Core's configured execution strategy.
3. Portal's scoped in-memory token store was not the same instance used by the `HttpClientFactory` authorization handler. The WebAssembly host now uses one per-tab singleton session/token store, still held only in memory.
4. `BookDoc2026.Blazor.UI` omitted `Microsoft.AspNetCore.Components.Web`, causing event directives to render as inert HTML. The shared library now imports the event namespace, and queue actions use unambiguous component methods.

The login inputs also bind on `oninput`, ensuring keyboard, automation and password-manager input reaches the Blazor model before submit.

## Browser acceptance evidence

Validated through isolated in-app browser tabs on 2026-08-23:

- reception dashboard exposed Reception and did not expose technician navigation;
- technician dashboard exposed Radiology and did not expose reception navigation;
- reception found only the synthetic branch-scoped patient projection, with masked mobile output;
- reception selected the synthetic X-ray service point and received one Waiting token;
- two independent technician tabs reported `Live updates connected`;
- session A progressed the token through every implemented stage;
- session B changed without pressing Refresh, proving SignalR status invalidation plus API reconciliation;
- both technician worklist and safe-display projection used the token/status and never rendered the patient's name;
- completion removed the token from the Now Calling display;
- semantic labels, headings, tables, buttons and keyboard-capable controls were present in the accessibility snapshot;
- a full reload cleared the in-memory Portal session as designed.

Existing integration tests continue to prove exact replay returns the original queue ticket, altered replay is rejected, urgent priority is permission-gated, cross-tenant access fails, the Hub requires authentication and display DTOs exclude patient identity. The browser run does not duplicate those durable API assertions.

## Verification

Verified after cleanup on 2026-08-23:

```text
Fixture create lifecycle:                   passed
Fixture end-to-end browser use:             passed
Fixture transactional removal:              passed
Fixture idempotent second removal:           passed
Full solution build:                        passed, 0 warnings / 0 errors
Unit tests:                                 57 passed
Architecture tests:                          9 passed
Integration tests:                          32 passed
Total automated tests:                      98 passed
Temporary fixture tenant/users/roles:       removed
API, Portal and temporary browser tabs:     stopped/closed
```

## Completion percentage

The following DOC-033 rows advance:

- row 7, Portal: **60% → 70%**;
- row 9, shared UI: **60% → 70%**;
- row 16, dashboards/Queue/realtime: **75% → 85%**;
- row 20, quality evidence: **70% → 75%**.

The twenty-row score gains 35 points: `1410 / 20 = 70.50%`.

- exact architecture-achievement completion: **70.50%**;
- rounded headline completion: **71%**;
- production-ready clinic MVP completion: **not represented by this percentage**.

No score is awarded for production SignalR scale/reconnect, clinic acceptance, broad accessibility, CI browser automation or production fixture operations; those remain open.

## Recommended next goal

Move to the lowest-scoring legacy-critical domain: freeze the Contract/Package entitlement rule matrix with owner evidence, then implement the first generalized entitlement reference slice without doctor-only assumptions.

The slice should cover contract/package identity, effective dates, branch applicability, service/resource-category entitlement, quantity/visit consumption, reservation versus final consumption, cancellation/reversal, concurrency, audit, protected public IDs and focused tests. Payment allocation, refunds and daily close should remain the following Billing slice rather than being mixed into the entitlement aggregate.
