# 48 — Practitioner Credential and Assignment Foundation

Owner: Workforce + Clinical + Identity + Resources + security + migration  
Status: Verified policy-neutral backend reference slice  
Reviewed: 2026-08-23

## Outcome

BOOKDOC2026 now has a tenant-wide Practitioner role that references one active Person Stakeholder and one active SDTS-style Identity subject without duplicating person, contact, address, identifier or document data. Practitioner credentials and effective-dated branch/service assignments are Workforce-owned and remain explicitly separate from Employee, employment contract, attendance, compensation and Payroll.

Encounter draft authorship remains permission-controlled and policy-neutral. Signing a draft or creating a signed amendment now additionally proves that the current Identity subject is an active Practitioner with a current verified credential and active assignment for the Encounter branch and service on the branch-local signing date. `Encounters.Sign` or `Encounters.Amend` alone is deliberately insufficient.

## Ownership and identity boundaries

- Stakeholder owns the person's shared name, demographics, contacts, addresses, identifiers and document references.
- Identity owns authentication state and the `uint` user subject. Workforce references that subject; it does not create a second login model.
- Workforce owns `PractitionerProfile`, professional credential decisions and branch/service eligibility.
- Catalog owns service definitions. Resources owns optional practitioner bookable resources and service capabilities.
- Clinical consumes the Workforce eligibility decision at signing; it does not read credential tables directly.
- Employee and Payroll remain deferred until their source database and approved rules are supplied.

A practitioner resource is still a scheduling object, not the professional record. An assignment may link an active `ResourceKind.Practitioner` resource only when that resource belongs to the route branch and has an active capability for the assigned service. The resource link is optional because clinical eligibility and scheduling representation have different lifecycles.

## Implemented lifecycle

```text
Person Stakeholder + active Identity subject
  -> Pending Practitioner profile
  -> one or more Pending credentials
  -> credential Verified or Rejected (decision is final)
  -> effective-dated branch/service assignment
  -> Practitioner Activated only with a current verified credential
  -> eligible signed clinical work while profile, Identity, Stakeholder,
     credential and assignment are all current
  -> profile Suspended/Inactive or assignment Suspended/Ended blocks signing
```

Credential validity uses inclusive `ValidFrom`/`ValidTo`. A rejected or verified decision is not overwritten; changed evidence requires a new credential record. Assignment overlap for the same practitioner, branch and service is rejected in the application boundary. Optimistic versions protect every mutable profile, credential-decision and assignment transition.

## Permissioned API

| Operation | Endpoint | Permission |
|---|---|---|
| Create pending Practitioner | `POST /api/v1/branches/{branchId}/practitioners` | `Practitioners.Manage` |
| Read Practitioner | `GET /api/v1/branches/{branchId}/practitioners/{practitionerId}` | `Practitioners.View` |
| Add credential evidence | `POST /api/v1/branches/{branchId}/practitioners/{practitionerId}/credentials` | `Practitioners.Manage` |
| Verify/reject credential | `POST .../credentials/{credentialId}/verify|reject` | `Practitioners.Credentials.Verify` |
| Add branch/service assignment | `POST .../{practitionerId}/assignments` | `Practitioners.Assignments.Manage` |
| Suspend/end assignment | `POST .../assignments/{assignmentId}/suspend|end` | `Practitioners.Assignments.Manage` |
| Activate/suspend/deactivate profile | `POST .../{practitionerId}/activate|suspend|deactivate` | `Practitioners.Manage` |

All four permissions are branch-assignable and are enforced by both ASP.NET policies and the application service. IDs use purpose- and tenant-bound protected strings; the Identity subject uses its global protected purpose. Assignment output is filtered to branches in the current actor's durable scope.

Audits record lifecycle action, internal relationship IDs, codes, validity dates, state and version. They do not copy names, contact details, registration numbers, clinical narrative or credentials into log metadata.

## Persistence

Migration `20260823112649_PractitionerWorkforceFoundation` creates the `workforce` schema and:

- `workforce.practitioner` with unique tenant code, Person Stakeholder and Identity-subject relationships;
- `workforce.practitioner_credential` with unique issuing identity, inclusive validity and final verification evidence;
- `workforce.practitioner_assignment` with tenant-qualified Practitioner, branch, service and optional branch-resource foreign keys;
- eight append-only seeded role claims for the four platform and four clinic permissions.

Internal business keys remain positive `long`/SQL `bigint`. Identity remains `uint` in CLR and maps to SQL `bigint`, consistent with DOC-031. Query filters provide the secondary tenant boundary, while composite foreign keys prove tenant and branch ownership. The migration is applied to local `BookDoc2026_Dev`.

## Automated evidence

Four domain tests prove activation prerequisites, credential decision finality and date validity, assignment effectiveness/suspension, and stale-version rejection.

The existing full Encounter API integration journey now also proves:

- platform-created Identity subject and centrally owned Person Stakeholder linkage;
- protected Practitioner, credential and assignment API flow;
- verified credential, effective service assignment and explicit activation;
- `Encounters.Sign` and `Encounters.Amend` fail with HTTP 403 for a permission-bearing but ineligible actor;
- the eligible practitioner can sign and amend while immutable Encounter history remains intact.

The complete suite is **112 tests**: 69 unit, 9 architecture and 34 integration tests.

## Deliberately deferred

- Employee, employer relationship, contract, attendance, leave, rota, compensation and Payroll;
- practitioner specialty dictionaries and Delhi council/authority master data;
- document-backed credential evidence, verification integrations, renewal reminders and expiry jobs;
- supervisor, countersigner, temporary coverage, delegation and emergency-access policy;
- reinstatement and assignment-resume commands;
- Admin workforce screens, Portal practitioner profile/agenda and MAUI journey;
- practitioner search, bulk import, credential dashboards and expiry reporting;
- migration from live doctor/user records and named clinic/clinician acceptance;
- database-enforced temporal overlap prevention and high-volume eligibility benchmarks.

## Achievement effect and next recommendation

This slice raises DOC-033 row 13 from 70% to 75% because a second real role now reuses the central Person Stakeholder rather than duplicating party data. The exact twenty-row architecture score becomes **71.50%**. No UI, production, clinical-acceptance or Payroll credit is claimed.

The recommended next independent slice is Billing invoice/allocation/receipt foundation built on confirmed Booking and existing Contract entitlement evidence, while keeping owner-unapproved automatic cancellation/refund coupling out. In parallel, the clinical owner still needs to approve DOC-024 specialty and signing/countersigning decisions before Physiotherapy or Orthopaedic content is specialized.
