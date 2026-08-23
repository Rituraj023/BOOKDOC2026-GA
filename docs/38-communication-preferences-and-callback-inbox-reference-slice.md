# 38 — Communication Preferences and Callback Inbox Reference Slice

Owner: Communications + Stakeholder + Worker + Admin + security  
Status: Verified reference slice  
Reviewed: 2026-08-18

## Outcome

BOOKDOC2026 now proves two complementary communication controls:

```text
authorized branch user
  -> stakeholder-owned append-only preference evidence
  -> purpose + class + channel + decision + quiet hours + source

signed provider callback
  -> development verifier
  -> accepted delivery match
  -> durable normalized inbox + outbox work item
  -> API-hosted Worker
  -> append-only delivery-status event
  -> permission-scoped Admin monitoring
```

This uses the development email provider and a synthetic test signing key only. No external provider, credential, message or patient data was used.

## Stakeholder-owned preference evidence

`CommunicationPreferenceEvent` belongs to the tenant Stakeholder rather than Patient, appointment or invoice. A person or corporate stakeholder can therefore retain one communication evidence history across future roles.

Each event records:

- contextual branch and stakeholder;
- normalized purpose code;
- message class: `Transactional`, `Clinical` or `Marketing`;
- channel: email, SMS, WhatsApp or push;
- decision: `Allowed`, `Denied` or `Withdrawn`;
- optional paired quiet-hours start/end and time-zone identifier;
- evidence source and a bounded evidence reference;
- recording actor and immutable timestamp.

Events are append-only. A later change creates a new event; it does not overwrite the evidence that existed when an earlier communication decision was made.

This slice deliberately does **not** assert that Indian law permits a required message to override a denial, or that every transactional message is exempt from quiet hours. Those policies require privacy/legal approval in DOC-017. Dispatch enforcement also waits for a real stakeholder-addressed message handler; the tenant-approval development message is not a patient/stakeholder notification.

## Callback trust and durability

`DevelopmentCallbackVerifier` is composed only in Development and Testing. It requires an uncommitted configuration value at `Messaging:DevelopmentCallbackSigningKey` and verifies `X-BookDoc-Signature` as HMAC-SHA256 over the exact request bytes.

After verification:

1. payload fields are parsed and bounded;
2. provider message ID must match an accepted delivery attempt;
3. provider + external event ID is the durable deduplication key;
4. the normalized callback inbox record and outbox work item are committed together;
5. Worker creates one append-only delivery-status event and marks the inbox processed.

Identical replay returns the existing protected inbox ID. Reuse of an external event ID with different signed content returns conflict. A bad signature returns unauthorized and is not trusted into tenant data.

The raw callback body is limited to 64 KiB, hashed with SHA-256 and discarded after verification. The primary clinical database stores normalized identifiers/status plus the hash—not the raw provider body. A future approved provider requiring forensic raw retention must use a separately access-controlled encrypted quarantine store with an explicit retention period.

## Permissions and routes

| Permission | Behavior |
|---|---|
| `Messaging.Preferences.View` | list a stakeholder's preference evidence history in an assigned branch context |
| `Messaging.Preferences.Manage` | append a new preference evidence event |
| `Messaging.Callbacks.View` | list verified callback inbox status for an assigned branch |

Authenticated branch routes:

```text
GET  /api/v1/branches/{branchId}/communications/stakeholders/{stakeholderId}/preferences
POST /api/v1/branches/{branchId}/communications/stakeholders/{stakeholderId}/preferences
GET  /api/v1/branches/{branchId}/communications/provider-callbacks?take=50
```

Provider ingress:

```text
POST /api/v1/communications/callbacks/{providerCode}
X-BookDoc-Signature: sha256={hex-hmac}
```

The callback route is intentionally anonymous at HTTP authentication level because the provider is authenticated cryptographically. Unknown providers, missing configuration and invalid signatures fail closed.

## Persistence

Migration `CommunicationPreferencesAndCallbackInbox` adds:

- `communication.communication_preference_event`;
- `communication.provider_callback_inbox`;
- `communication.message_delivery_status_event`;
- unique provider/event replay constraints;
- tenant/branch/time indexes;
- seeded permission claims for preference and callback visibility/management.

Internal keys remain positive `bigint`; public IDs remain tenant-bound protected strings. Preference and delivery-status evidence are append-only in `BookDocDbContext`. Callback inbox state is mutable only for its pending-to-processed/failed workflow.

The migration was generated and reviewed but not applied to a user or production database.

## Admin and typed Client

The shared typed Client now supports preference history/recording and callback status. The existing Admin Communications page adds:

- stakeholder preference entry and history;
- explicit purpose, message class, channel and decision;
- quiet-hours and evidence fields;
- verified callback provider/event/delivery/inbox status;
- bounded, branch-permission-filtered callback queries.

Admin sign-in/session UX, approved-network enforcement and authorization-aware navigation were completed as a reference foundation in [DOC-039](39-admin-security-foundation-reference-slice.md). Production proxy/network configuration, persistent privileged-session policy and browser acceptance remain open, so this is not yet a production control console.

## Verification evidence

Verified on 2026-08-18:

```text
Unit tests:                         33 passed
Architecture tests:                  9 passed
Integration tests:                  30 passed
Total automated tests:              72 passed
API build:                          passed, 0 warnings / 0 errors
Admin build:                        passed, 0 warnings / 0 errors
EF pending model changes:            none
```

New evidence covers:

- purpose normalization and overnight quiet hours;
- incomplete quiet-hours rejection;
- callback pending-to-processed lifecycle;
- append-only delivery-status construction;
- preference view/manage permission separation;
- protected stakeholder preference history;
- invalid callback signature rejection;
- identical callback replay deduplication;
- changed-content replay conflict;
- Worker reconciliation exactly once;
- callback monitoring permission denial, processed status and cross-tenant empty result.

The EF CLI is 10.0.8 while runtime packages are 10.0.10; generation succeeds with the existing version warning.

## Achievement impact

- Admin row 6 advances from 35% to 40% because the real management screen now covers templates, preferences, delivery attempts and callback reconciliation.
- Communications row 17 advances from 55% to 65% because consent evidence and signed durable callback reconciliation now work with a fake provider.
- The exact twenty-row average is 60.75%, reported as **61%**.

No score is awarded for a real provider, consent enforcement during patient delivery, legal approval, production secret operations or callback raw-body retention because those outcomes do not exist yet.

## Next recommended slice

Build the Admin security foundation before adding more management screens:

1. sign-in, refresh, revoke and session-expiry UX using the existing Auth Client;
2. secure server-side token handling suitable for interactive Admin;
3. authorization-aware navigation and forbidden/expired-session states;
4. fail-closed trusted-proxy and approved-IP/network policy;
5. integration tests for forwarded-header spoofing, missing allow-list configuration and permission-filtered navigation.

[DOC-039](39-admin-security-foundation-reference-slice.md) completes the Admin security work above. [DOC-040](40-generalized-booking-and-preference-notification-reference-slice.md) then connects preference evaluation to the first patient-addressed durable Booking notification. Production WhatsApp/email providers remain gated by DOC-017 and DOC-020.
