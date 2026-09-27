# 19 — UI/UX Navigation and Screen Inventory

## Experience model

Navigation is permission-composed. A role sees only authorized modules and dashboard cards, while server authorization remains authoritative. Admin is an ERP-style experience for setup, governance, monitoring, reporting and controlled imports/exports, available only through an approved network/IP route. Portal is an authenticated internet-facing experience for daily role-scoped operations and patient self-service. MAUI applications are focused mobile versions of Portal workflows.

## Admin navigation candidates

- Tenant/organization/branch setup, branding and numbering
- Clinic application review with dynamic document checklist, secure preview, verification/expiry state and approve/reject history
- Platform-only document-type catalog/version management and provider-verification exception monitoring
- Users, roles, permissions, scopes, access reviews and audit
- Employees, practitioners, credentials and assignments
- Services, prices, taxes, resources, schedules and queue configuration
- Communication templates/providers, consent configuration and delivery monitoring
- Report catalog, schedules, execution history and snapshots
- Print agents/printers/jobs
- Integration, feature, retention and operational settings
- Tenant-default and branch-override branding, document sequence and communication-identity configuration
- Clinic-facing onboarding form and permission-controlled branch configuration editor showing inherited/effective values, version history, audit evidence and provider-verification state
- Management dashboards and permitted cross-branch reports
- Import definitions, validation preview, controlled execution, rejection files and import audit
- Export definitions, execution history, secure downloads and retention

## Portal navigation candidates

- Clinical agenda: effective Practitioner-assignment worklist, common Encounter draft/revision/sign workflow, immutable history and authorized specialty handoff. DOC-053/054 implement and render the policy-neutral reference screen; DOC-055 adds configured X-ray/CT Order request plus modality-filtered Queue handoff while keeping Order/Result/Queue states visible and separate. Clinical UAT remains open.
- Physiotherapist workspace: bounded branch care-plan worklist, status/search filters, immutable clinical history, separately permissioned lifecycle/session/outcome commands and dynamic like-for-like outcome series. DOC-052 implements the policy-neutral reference screen; scheduled Encounter handoff, approved templates and clinician UAT remain open.

- Role dashboard and assigned tasks
- Patient search/registration/profile
- Calendar, appointments, resource board and waitlist
- Arrivals and OPD/department queues
- Practitioner agenda and encounter workspace
- Physiotherapy assessment, care plan, session note, outcome trend and exercise-program workspace
- Orthopaedic examination, imaging review/status, procedure, prescription and follow-up workspace
- Investigation order and modality worklists: signed-Encounter request and Queue handoff are implemented in DOC-055; DOC-056 adds a separately permissioned technician list with explicit bounded Patient/Encounter/Service/indication disclosure and independent Queue commands. [DOC-059](59-portal-radiology-execution-quality-workspace.md) adds the shared Study execution workspace, matching-equipment selection, immutable attempt/review history and separate eligible operator/quality-reviewer command sets. Queue, Study and future Result status remain visibly independent. Approved clinical policy, Study realtime invalidation and all interpretation/release work remain open.
- Invoice, collections, cashier close and permitted refunds
- Permission-approved operational reports and document output

## Patient navigation candidates

- Clinic/service/practitioner discovery
- Booking/reschedule/cancel and queue status
- Appointments/history and released documents/results
- Prescriptions, invoices and receipts
- Profile, representatives, consent and communication preferences
- Support, privacy notice, requests and grievance route

## Required design artifacts

1. sitemap by host and role;
2. dashboard-card catalog with permission/query/action/refresh behavior;
3. low-fidelity flows for the workflows in document 14;
4. design tokens, typography, spacing, color, validation and status language;
5. reusable patterns for search, grids, timeline, wizard, clinical form, money, report filters and print preview;
6. responsive behavior at supported widths plus accessibility annotations;
7. loading, empty, stale, offline, unauthorized, conflict, partial-failure and success states;
8. content and localization glossary for English plus approved Indian languages.

## UX acceptance rules

Reusable Admin/Portal Blazor presentation belongs in `BookDoc2026.Blazor.UI`; reusable mobile presentation belongs in `BookDoc2026.Maui.UI`. Neither library may contain backend business rules or authorization decisions.

- Patient/branch/context is visible before high-risk clinical or financial action.
- Switching tenant/branch clears incompatible state and subscriptions.
- Dangerous actions name the consequence and require reason/approval where defined.
- Realtime changes are indicated without silently replacing user edits.
- Large exports, scheduled reports and print jobs show durable status rather than a blocking spinner.
- Clinical and financial accessibility is tested with keyboard, screen reader, large text and contrast—not inferred from component choice.
