# 07 — .NET MAUI Mobile Plan

## Current state and goal

The SDTS repository has `Mobile`, `MobileUI`, and `MobileTest` directories but no implementation. BOOKDOC2026 therefore needs a deliberate MAUI architecture, not a folder copy. Two Xamarin.Forms applications provide workflow evidence: `BookDocApp` for clinic staff and `BookDocCustApp` for patients. Their detailed inventory and risk assessment are in [Legacy Xamarin Mobile Discovery](09-xamarin-mobile-discovery.md).

The initial .NET 10 foundation is now scaffolded as `src/BookDoc2026.Mobile` and `src/BookDoc2026.Maui.UI`. `BookDoc2026.Mobile` is a MAUI Blazor Hybrid shell for Android, iOS, Mac Catalyst and Windows. It consumes the shared typed client and shared Blazor presentation primitives; `BookDoc2026.Maui.UI` owns native cross-platform services and future native controls. The Windows target is registered in AppHost as an explicit-start development resource; Android and iOS still launch through their normal emulator/device tooling.

Build feature journeys only on stable, versioned APIs and the shared typed client. Start with a narrow patient experience; add staff workflows after field observation proves mobile value.

The Xamarin XAML, ViewModels and `DataHelper` classes are not directly portable. Reuse their validated journey order and terminology; rebuild their transport, authentication, session state, navigation, authorization and document access.

## Implemented project boundary and planned test suites

```text
src/BookDoc2026.Mobile
  App shell, DI/composition, navigation, platform permissions,
  secure storage, connectivity, notifications, deep links

src/BookDoc2026.Maui.UI          reusable component library for all mobile hosts
  Design tokens, controls, view states, validation, accessibility

MobileTest/BookDoc2026.Mobile.UnitTests
MobileTest/BookDoc2026.Mobile.IntegrationTests
MobileTest/BookDoc2026.Mobile.DeviceTests
```

`BookDoc2026.Mobile` is a mobile version of approved Portal workflows and references `BookDoc2026.Client`, `BookDoc2026.Blazor.UI` and `BookDoc2026.Maui.UI`; Contracts arrive through the typed Client boundary. It does not reference Domain, Application, Infrastructure or EF entities. Reusable Razor presentation primitives belong in `BookDoc2026.Blazor.UI`; native MAUI controls, secure device services, design tokens, platform view states and accessibility behavior belong in `BookDoc2026.Maui.UI`. Business rules and authorization remain server-side.

The foundation currently proves composition, a shared Blazor component, API endpoint injection and Keychain/Keystore/Windows secure-storage token persistence. It does not yet prove sign-in, refresh/revocation purge, navigation, offline data, push, SignalR, deep links or any clinical workflow.

Use MVVM with explicit feature folders. Avoid a large shared view-model base class. Business invariants stay on the server; the client duplicates only friendly validation and workflow state needed for UX.

## Personas and staged scope

### Stage M1 — Patient

- Activate/sign in, logout all devices, recover account, optional biometric re-entry to local token.
- Select clinic/service/practitioner/date, view slots, hold/book, reschedule/cancel.
- View upcoming/history and appointment/queue status.
- Show privacy-safe live queue position/status through SignalR while foregrounded and push when backgrounded, subject to policy.
- Receive push/deep-link notifications with privacy-safe lock-screen content.
- View only documents/results explicitly released to the patient.
- View invoices/receipts and start payment when a provider is approved.
- Manage contact and communication preferences; show consent text/version.

### Stage M2 — Practitioner

- Today’s scoped schedule, patient arrival state, limited patient summary and alerts.
- Receive authorized live agenda, queue and result-status invalidations without embedding clinical text in SignalR payloads.
- Start encounter, capture draft note/vitals, view history allowed for care.
- Sign only with online server confirmation initially.
- No broad patient export or unrestricted background download.
- Include the validated legacy agenda, follow-up, patient history, clinical note, treatment/progression and vitals intent, redesigned around the Encounter API.

### Stage M3 — Front desk/nurse

- Check-in/queue actions, patient lookup with masked results, selected vitals/tasks.
- Role-specific deployment may be preferable to exposing mixed functions.
- Evaluate the legacy staff appointment creation, reminders/tasks and payment-collection views. Keep refund, adjustment, close and other high-risk money actions web-first until mobile controls are proven.

### Legacy parity rule

Every old mobile screen is assigned one status in the feature traceability register: `preserve`, `combine`, `replace`, `web-only`, `defer`, or `retire`. Parity is measured by accepted user outcomes and server state, not matching page count or appearance.

## Online and offline rules

Classify every operation:

| Class | Behavior | Examples |
|---|---|---|
| Online required | Never queued locally | slot booking, payment, signing prescription/note, refund, patient merge |
| Offline read cache | Encrypted, short-lived, scope-bound | own appointments; practitioner’s current-day list if policy allows |
| Offline draft | Encrypted local draft with explicit sync/conflict UI | unsigned clinical note only after risk approval |
| Never cached | Display transiently | secrets, broad search results, high-sensitivity notes, document bytes unless user explicitly downloads |

Start M1 online-first. Add offline clinical drafts only after threat modeling, remote wipe/session revocation behavior, conflict resolution, device policy, and user training are approved.

## Authentication and device security

- OAuth/OIDC-style short-lived access token and rotated refresh token through approved identity endpoints; never embed client secrets.
- Revoke the shared secret embedded in both old binaries and retire device-ID login. A device identifier is metadata, never proof of identity.
- Store refresh material in platform secure storage/Keychain/Keystore; keep access tokens memory-first.
- Biometric unlock protects locally stored refresh capability but does not replace server authentication.
- Support server-side device/session inventory and revocation.
- Clear cached protected data on logout, revocation, user/scope change, and policy expiry.
- Certificate validation is mandatory; pinning requires an operational rotation plan before use.
- Prevent tokens, patient values, document URLs, notification payloads, screenshots/crash reports, and analytics from leaking into logs.
- Detect rooted/jailbroken devices only as a risk signal according to policy; do not claim it provides absolute protection.

## Data and sync model

- Every cached record has server ID, server version/ETag, scope, fetched/expiry timestamps, and sensitivity class.
- Commands have a client operation ID/idempotency key.
- Sync never silently overwrites a newer server version.
- Local database is encrypted where supported and contains the minimum fields.
- Background sync respects OS restrictions and does not assume exact timing.
- Push payload contains an opaque event ID and generic message; app fetches authorized details after authentication.
- Patient phone/contact masking and all other field-level restrictions are applied by server projections; hiding a Xamarin control is not authorization.
- SignalR is active only while useful/allowed, rejoins server-assigned groups after token refresh, and always refreshes authoritative data after reconnect.
- Mobile may display an authorized report or durable-job status returned by the API, but it does not schedule management reports, run background jobs, or communicate directly with the local print agent. Printing remains browser/PDF or an Admin/Portal command governed by [Backend Workers, Reporting and Printing](11-backend-workers-reporting-printing.md).

## UX and accessibility

- Design for one-handed common actions, variable text size, screen readers, contrast, clear touch targets, network interruption and slow devices.
- Every screen has loading, empty, error, offline, stale, unauthorized, and success states.
- Dangerous actions show patient/appointment context and require reason where appropriate.
- Clinical and financial conflicts require explicit refresh/review, not a generic retry spinner.

## API prerequisites

Before M1 implementation:

- Typed auth/session and patient-self endpoints.
- Stable scheduling slot/hold/booking/idempotency contracts.
- Patient-scoped document release and download authorization.
- Notification registration/unregistration and preference endpoints.
- Standard error codes and UTC/time-zone behavior.
- Development/staging environments reachable without weakening production transport security.

Before M2/M3:

- Strong branch/role scope selection and revocation.
- Patient summary/alert least-privilege projection.
- Encounter draft concurrency and signing contracts.
- High-risk read/export audit and mobile session policies.

## Testing matrix

- Android and iOS supported OS/device matrix approved before release.
- Unit tests: view models, validation, mapping, state reducers, time-zone display.
- Client integration: auth refresh, idempotency, conflict/error mapping, cancellation.
- Device tests: secure storage, biometric flow, deep links, push registration, permissions, background/resume, logout purge.
- Network tests: offline, latency, packet loss, expired token, server upgrade, duplicate tap.
- Security tests: lost device/session revoke, screenshot policy where applicable, log/crash redaction, backup exclusion.
- Accessibility and localization tests, including large text and screen reader.
- Store/distribution privacy declarations and dependency review.

## Mobile definition of done

- APIs and typed client are stable and contract-tested.
- Online/offline classification is approved for every screen/action.
- Tokens/cache/logging meet security policy and logout/revocation purge is proven.
- Concurrent changes and duplicate submissions are handled visibly and safely.
- Device matrix, accessibility, privacy declarations, telemetry, support and rollback/feature-flag plan are complete.
