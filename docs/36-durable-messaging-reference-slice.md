# 36 — Durable Messaging Reference Slice

Owner: Communications + backend + Worker + security + operations  
Status: Verified backend reference slice  
Reviewed: 2026-08-18

## Outcome

BOOKDOC2026 now proves one durable, tenant-scoped, versioned and observable message-delivery path:

```text
tenant approval transaction
  -> published tenant email template + typed outbox record
  -> API-hosted Worker claim
  -> database template resolution
  -> strict safe rendering
  -> development email provider
  -> append-only delivery attempt
  -> permission-scoped API/typed Client status query
```

The example intentionally uses the existing `Foundation.TenantApproved.v1` event and a development-only provider. It proves the architecture without sending a real message or requiring provider credentials.

## Implemented boundaries

- `BookDoc2026.Domain/Communications` owns versioned template lifecycle and append-only delivery-attempt evidence.
- `BookDoc2026.Templates` remains the platform-neutral selection/rendering contract and strict renderer.
- `BookDoc2026.Messaging` remains the provider-neutral dispatcher; it now returns provider/template metadata and stable configuration error codes.
- `BookDoc2026.Infrastructure` implements the EF template catalog, communication query repository and development email adapter.
- `BookDoc2026.Worker` still owns the hosted polling loop and now uses injectable `TimeProvider` for scheduling.
- API remains the only backend host and explicitly composes the development provider only in Development/Testing.
- Contracts/Client expose only safe delivery metadata; they do not expose message bodies, raw destinations, operation IDs or payload JSON.

Infrastructure is allowed to reference Messaging/Templates because it implements their persistence/provider adapters. Messaging/Templates do not reference Infrastructure or API, and architecture tests enforce the direction.

## Database design

Migration `DurableMessagingFoundation` adds:

- `communication.message_template` with tenant/organization/branch scope, key, channel, culture, template version, content kind, draft/published/retired status, publication time and optimistic revision;
- filtered unique indexes for tenant-, organization- and branch-scoped template versions;
- `communication.message_delivery_attempt` as append-only evidence containing safe provider/status/error metadata and a masked recipient hint;
- outbox `operation_id`, `correlation_id` and `last_error_code` fields;
- a safe data-preserving rename/truncation of legacy `last_error`, unique operation-ID backfill and correlation backfill for existing outbox rows.

Migration `MessagingDeliveryPermission` adds `Messaging.Deliveries.View` to the seeded permission model. The EF model has no pending changes, and an idempotent migration script generates successfully.

## Durable and failure behavior

- Each outbox record receives a GUID operation/idempotency identity and validated correlation ID.
- Handlers receive an `OutboxMessageContext` rather than raw JSON alone: message/scope/type/payload/operation/correlation/attempt are explicit.
- Provider calls reuse the same operation ID across retries.
- A previously accepted operation is not sent again if the Worker crashed before completing the outbox record.
- Transient provider failures create an immutable failed attempt and use bounded outbox backoff.
- Permanent configuration/provider failures dead-letter immediately.
- Unknown exceptions store only `internal_error`; raw exception messages are no longer persisted in the outbox.
- Delivery logs use IDs, stable codes, transience and correlation only. They do not log template bodies, recipient addresses or payload JSON.
- `TimeProvider` drives the existing clock and Worker delay, enabling deterministic expiry/retry tests.

## Initial provisioned template

Tenant approval creates and publishes tenant-scoped version 1 of:

```text
Key: Tenant.Approved.Contact
Channel: Email
Culture: en-IN
Content: HTML
```

The outbox payload contains only tenant application, tenant, organization and branch numeric identities. The handler loads the contact and clinic name from authoritative data at execution time, renders through the published catalog and sends only to the selected provider in memory.

This system-provisioned template is a safe reference default. It is not a substitute for the future Admin draft/review/publish/version workflow.

## Monitoring contract

Permission:

```text
Messaging.Deliveries.View
```

Endpoint:

```http
GET /api/v1/branches/{branchId}/communications/delivery-attempts?take=50
```

Rules:

- authenticated branch permission and durable tenant/branch scope are both required;
- `take` is bounded from 1 to 100;
- delivery IDs use tenant-bound protected public strings;
- response includes channel, template identity/version, attempt, provider code, masked recipient hint, status, provider message ID, stable error code and timestamp;
- operation/idempotency identity, body, subject, raw recipient and outbox payload are excluded.

`BookDocApiClient.ListMessageDeliveryAttemptsAsync` exposes the same typed contract to Admin/Portal/Mobile without backend references.

## Verification evidence

Verified on 2026-08-18:

```text
Unit tests:                         30 passed
Architecture tests:                  9 passed
Integration tests:                  27 passed
Total automated tests:              66 passed
AppHost Release build:              passed, 0 warnings / 0 errors
EF pending model changes:            none
Idempotent migration script:         generated successfully
```

New evidence covers:

- draft/publish/retire revision rules;
- permanent outbox dead-letter with safe code;
- tenant and branch template selection without cross-tenant resolution;
- accepted development-provider delivery and masked evidence;
- permanent provider rejection;
- deterministic transient failure, backoff, same-operation retry and eventual completion;
- completed-operation replay prevention;
- permission denial and authorized delivery-status response through HTTP.

No migration was applied to a user or production database during this slice. No real provider, credential, endpoint, patient data or external message was used.

## Deliberately incomplete

- Admin template draft/create-version/preview/publish/retire and delivery-monitoring reference UI is now implemented in [DOC-037](37-admin-communication-management-reference-slice.md); platform-level defaults and production UX remain incomplete;
- explicit template author/approver audit and separation of duties;
- communication preference evidence and quiet-hour capture now exist in [DOC-038](38-communication-preferences-and-callback-inbox-reference-slice.md); approved enforcement and channel fallback remain incomplete;
- real email, WhatsApp, SMS and push providers plus secret/identity verification;
- HMAC-verified development callbacks, durable inbox and Worker reconciliation now exist in DOC-038; real-provider verification and production quarantine/operations remain incomplete;
- dead-letter/replay administration UI and Worker fairness/load evidence;
- Booking/appointment templates, because confirmed Booking is not implemented yet;
- immutable receipt/confirmation document generation and distribution;
- Windows-service task inventory, parallel run and retirement.

The development provider is never composed in Production. Production delivery cannot be considered configured until an approved adapter and managed credentials pass DOC-020/DOC-011 gates.

## Next recommended slice

[DOC-037](37-admin-communication-management-reference-slice.md) completes branch template management and [DOC-038](38-communication-preferences-and-callback-inbox-reference-slice.md) completes the preference/fake-callback reference. Next secure the Admin host and connect approved preference evaluation to a real stakeholder-addressed workflow. None of this is real-channel or legacy-service retirement evidence.
