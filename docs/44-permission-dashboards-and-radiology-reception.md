# 44 — Permission Dashboards and Radiology Reception

Owner: Admin + Portal + shared UI + Patient + Queue + migration  
Status: Verified reception/dashboard reference slice  
Reviewed: 2026-08-23

## Outcome

This slice completes the front of the first imaging Queue journey:

- Admin and Portal home pages render shared capability cards from permissions, never hard-coded role names;
- Portal reception requires both `Patients.Search` and `Queues.CheckIn`;
- reception searches the existing branch-authorized patient projection and shows only its already-approved masked contact fields;
- active X-ray/CT service points can be selected by a queue viewer, receptionist or service-point manager without granting ticket-worklist visibility;
- optional protected Booking context is validated by the existing Queue backend against patient and branch;
- normal priority is available to reception, while urgent priority and its mandatory reason require `Queues.Priority.Manage`;
- one request ID is retained across failed/ambiguous retries and rotated only when reception explicitly starts another check-in;
- a successful or replayed response displays the privacy-safe Queue token rather than exposing the patient on the public display.

This does not add clinical orders, protocols, radiation checks, findings, signed reports, DICOM/PACS or billing behavior.

## Permission-composed dashboards

`PermissionDashboard` and `PermissionDashboardCatalog` live in `BookDoc2026.Blazor.UI`. A card declares all required permissions and/or any acceptable permission. Host catalogs contribute only routes they actually implement:

- Admin: communication management and radiology setup;
- Portal: radiology reception and technician worklist.

The model composes workspaces for any assigned role, including future custom clinic roles, without role-name checks. Direct routes still enforce their own permission gates; hiding a card is navigation behavior, not authorization.

## Reception and idempotency behavior

The reception page follows:

```text
Patient search -> patient selection -> service point + optional Booking
    -> priority authorization -> idempotent Queue check-in -> safe token
```

While a command is active, the submit button is disabled. Network/API failure retains the draft request ID so Retry invokes the server's durable idempotency rule. Exact replay returns the existing ticket with `IsReplay=true`; altered reuse remains rejected by the backend. After success, `Start another check-in` rotates the request ID and clears patient/Booking/priority state.

Service-point catalog reads now permit any of `Queues.View`, `Queues.CheckIn` or `Queues.ServicePoints.Manage`. Worklist and display endpoints remain separately protected, so reception does not gain Queue visibility merely to select a destination.

## Local migration rehearsal and defect correction

The migration chain was applied to configured local database `BookDoc2026_Dev`, not to a clinic, user, shared, staging or production database.

The first Queue migration attempt correctly stopped and rolled back migration `20260823100340_ImagingQueueAndRealtime` because SQL Server filtered indexes do not support the generated `BETWEEN` predicate used there. The unapplied migration and model metadata were corrected to the equivalent supported predicate:

```sql
[booking_id] IS NOT NULL AND [status] >= 1 AND [status] <= 4
```

The second attempt applied successfully. `dotnet ef migrations list` reports all ten migrations applied locally, and the pending-model-change check reports none. This is useful development rehearsal evidence, but it is not either of the two representative migration rehearsals required for production cutover.

## Verification

Verified on 2026-08-23:

```text
Full solution build:                      passed, 0 warnings / 0 errors
Unit tests:                               57 passed
Architecture tests:                        9 passed
Integration tests:                        32 passed
Total automated tests:                    98 passed
Local development migration update:       passed after predicate correction
EF pending-model-change check:             passed (none pending)
```

Evidence includes all/any dashboard-card filtering, empty capability behavior, check-in draft identity retention/rotation, receptionist service-point catalog authorization, manager/viewer catalog access and existing Queue idempotency/altered-replay/priority/tenant tests.

No browser automation, rendered-component assertion, controlled role fixture, two-session SignalR test, accessibility audit, clinic data or user acceptance is claimed.

## Completion percentage

The following DOC-033 rows advance:

- row 6, Admin: **60% → 65%**;
- row 7, Portal: **50% → 60%**;
- row 9, shared UI: **50% → 60%**;
- row 16, dashboards/Queue/realtime: **65% → 75%**;
- row 19, database/migration: **45% → 50%**.

The twenty-row score gains 40 points: `1375 / 20 = 68.75%`.

- exact architecture-achievement completion: **68.75%**;
- rounded headline completion: **69%**;
- production-ready clinic MVP completion: **not represented by this percentage**.

Row 20 remains 70% because the additional tests and local database pass do not replace CI, rendered browser, device, security, load, recovery or UAT evidence.

## Recommended next goal

Provision repeatable development-only role fixtures through an explicit command—not application startup seeding—and automate the complete browser journey:

1. reception role: dashboard → patient search → imaging check-in;
2. technician role in a second session: live worklist → call → prepare → start → complete;
3. safe display: token/status only;
4. disconnect/reconnect and duplicate-submit recovery;
5. negative navigation/API cases for missing permissions;
6. WCAG keyboard/focus/label checks.

Keep fixtures local/testing-only, secret-backed and removable. Do not weaken production JWT, branch scope, CORS or network restrictions to make browser tests easier.
