# 37 — Admin Communication Management Reference Slice

Owner: Admin + Communications + security + backend  
Status: Verified reference slice  
Reviewed: 2026-08-18

## Outcome

BOOKDOC2026 now has the first working Admin communication-management path on top of the durable messaging foundation:

```text
authorized Admin user + protected branch ID
  -> effective tenant/organization/branch template catalog
  -> create immutable next branch draft version
  -> strict placeholder preview
  -> separately authorized publish
  -> revision-protected retire
  -> audit evidence without template content
```

The same Admin screen reads the safe delivery-attempt query implemented in [DOC-036](36-durable-messaging-reference-slice.md). This is an implemented reference slice, not a production-ready communications console.

## Scope and override rule

- The branch route lists templates effective for the selected branch: tenant defaults, matching organization defaults and branch overrides.
- Clinic administrators create **branch-scoped overrides only** through this route.
- Inherited tenant or organization templates are visible and previewable but cannot be published or retired through a branch route.
- A new draft receives the next version for the exact branch/key/channel/culture identity; published content is never edited in place.
- Template IDs remain tenant-bound protected strings in HTTP and Client contracts.
- The API checks both the host authorization policy and the actor's durable tenant/branch assignment.

Platform-level tenant/organization template management remains a later control-plane slice. This prevents a branch administrator from changing defaults used by other branches.

## Permissions

| Permission | Current behavior |
|---|---|
| `Messaging.Templates.View` | list the effective catalog and render a strict preview |
| `Messaging.Templates.Manage` | create a next branch draft version and retire a published branch version |
| `Messaging.Templates.Publish` | publish a branch draft with the expected revision |
| `Messaging.Deliveries.View` | list recent safe delivery-attempt metadata |

All four permissions are available to the seeded clinic-administrator role and remain independently enforceable. `Manage` does not imply `Publish` in the API.

## API and typed Client

Branch-scoped endpoints:

```text
GET  /api/v1/branches/{branchId}/communications/templates
POST /api/v1/branches/{branchId}/communications/templates/versions
POST /api/v1/branches/{branchId}/communications/templates/{templateId}/preview
POST /api/v1/branches/{branchId}/communications/templates/{templateId}/publish
POST /api/v1/branches/{branchId}/communications/templates/{templateId}/retire
GET  /api/v1/branches/{branchId}/communications/delivery-attempts
```

`BookDocApiClient` exposes typed methods for every operation. Admin therefore depends only on Client, Contracts and shared UI—not Application, Domain, Infrastructure, Messaging or Templates.

## Preview and safety

- Preview uses the shared `StrictTemplateRenderer`; management does not introduce a second rendering implementation.
- Missing placeholders return a domain validation error.
- HTML body placeholder values are encoded by default.
- Preview is non-durable and sends no provider message.
- Audit JSON records key, version, channel, culture, scope and revision only; subject, body and preview values are excluded.
- Delivery monitoring continues to expose masked destination hints and stable error codes, never raw content or destinations.

## Admin experience

`BookDoc2026.Admin/Components/Pages/Communications.razor` supplies:

- protected branch selection;
- an effective template table with scope/status/version;
- branch-draft creation for email, SMS, WhatsApp and push;
- placeholder-value preview;
- publish/retire actions shown only for eligible branch versions;
- recent delivery attempts using safe operational metadata;
- responsive ERP-style layout and explicit empty/loading/error feedback.

This screen originally assumed that the Admin authentication/session flow had populated the shared token store. [DOC-039](39-admin-security-foundation-reference-slice.md) now supplies circuit-scoped sign-in/refresh/revoke, permission-aware rendering and fail-closed approved-network enforcement. A durable privileged-browser session decision and polished design-system components remain separate work.

## Persistence and migration

No new communication table was required because DOC-036 already introduced version, status, scope and revision columns. Migration `AdminCommunicationManagement` updates the seeded role-claim model for the three template permissions. It was generated and reviewed but was not applied to a user or production database.

The repository catches EF concurrency conflicts. The domain also compares `ExpectedRevision` before publish/retire, so a stale Admin page receives conflict rather than overwriting a newer state.

## Verification evidence

Verified on 2026-08-18:

```text
Unit tests:                         30 passed
Architecture tests:                  9 passed
Integration tests:                  28 passed
Total automated tests:              67 passed
Admin build:                        passed, 0 warnings / 0 errors
API build:                          passed, 0 warnings / 0 errors
EF pending model changes:            none
```

The new integration scenario proves:

- effective inherited-template listing;
- view-only denial of draft creation;
- branch draft creation and protected response ID;
- strict HTML preview encoding;
- `Manage` without `Publish` receives `403`;
- authorized publication advances the revision;
- stale revision retirement receives `409`;
- current revision retirement succeeds.

The local EF tool is version 10.0.8 while the runtime is 10.0.10; generation succeeded with that warning. No migration was applied during verification.

## Achievement impact

This slice advances:

- DOC-033 row 6, Admin, from 25% to 35% because one real management screen and typed workflow now exist;
- DOC-033 row 17, Communications, from 45% to 55% because branch template versioning, preview, audit and separated publication authorization now work;
- the twenty-row simple average from 59% to **60%**.

The score remains deliberately conservative: authentication UX, network restriction, consent, real providers, callback reconciliation, operational administration and user acceptance are incomplete.

## Next recommended slice

[DOC-038](38-communication-preferences-and-callback-inbox-reference-slice.md) implements the append-only stakeholder preference model and signed fake-provider callback inbox. [DOC-039](39-admin-security-foundation-reference-slice.md) completes the recommended Admin security foundation. The next slice is confirmed generalized Booking plus its first stakeholder-addressed transactional notification. Real email, WhatsApp, SMS and push adapters remain blocked on provider, credential, legal/consent and operational approval in DOC-017/DOC-020.
