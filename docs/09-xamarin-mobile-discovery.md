# 09 — Legacy Xamarin Mobile Discovery

Status: evidence baseline; requires workflow validation on running apps  
Reviewed: 2026-08-09  
Sources:

- Staff app: `C:\Users\Rituraj\source\repos\Rituraj023\BookDocApp`
- Patient app: `C:\Users\Rituraj\source\repos\Rituraj023\BookDocCustApp`

## Purpose

Capture mobile-specific workflows and technical lessons that were not fully visible in the legacy web application. These apps use the same BookDoc backend/database, so their screens and request models are important requirements evidence. They are not production-safe source code to port directly.

## Solution inventory

Both solutions use the same Xamarin-era shape:

- Xamarin.Forms 5 shared project plus Android, iOS, and UWP heads.
- Android targets API/framework level 11.0 in the project files.
- Xamarin.Essentials, `Acr.UserDialogs`, Newtonsoft.Json, permission plugins, custom renderers/effects, camera/gallery, image resizing, phone calling, and orientation adapters.
- `MasterDetailPage` navigation, static application state, view models used as both UI state and HTTP payload/response models, and one large `DataHelper` per app.
- No automated test project was found in either solution.

Measured source inventory:

| App | C# files | XAML files | View XAML files | Primary shared ViewModels |
|---|---:|---:|---:|---:|
| Staff `BookDocApp` | 241 | 117 | 114 | 56 |
| Patient `BookDocCustApp` | 145 | 62 | 58 | 33 |

The apps also contain 109 and 99 PNG files respectively. These are candidate visual references only; confirm ownership/license, eliminate duplicates, check resolution/accessibility, and rebuild the new icon set as MAUI assets where appropriate.

## Staff app capability map

The staff menu exposes 13 primary entries:

1. Doctor Schedule.
2. Payment Collections.
3. Create an Appointment.
4. Create a Reminder.
5. Search Existing Patient.
6. Clinical Notes.
7. Patient Treatment.
8. Patient Progression.
9. Vital Signs.
10. Reminder Overview.
11. Payment Overview.
12. Appointment Overview.
13. Change Password.

Additional screens/actions found in navigation and view models:

| Workflow | Legacy evidence | Target interpretation |
|---|---|---|
| Staff authentication | username/password, device login, logout, password change, user profile/image | Rebuild with secure token/session and device registration |
| Provider schedule | accommodation, activity, practitioner/object schedules, booking detail | Practitioner day agenda with branch/service filters |
| Appointment operations | multi-step create/edit, follow-up, status update, cancel | Use scheduling state commands, version checks and idempotency |
| Patient search | multi-step search and selection | Permission-aware patient search with masking and audit |
| Patient profile | demographic edit, phone/SMS, profile image | Patient registry projection; sensitive contact permissions |
| Patient history | bookings, payments, images/documents | Patient timeline grouped by permission and module |
| Clinical note | assessment read/create with diagnosis/complaint visibility | Encounter draft/sign/amend workflow, not generic assessment CRUD |
| Treatment/progression | create/read treatment and progression | Specialty-template sections inside encounter or care plan |
| Vitals | create/read vitals through multi-step form | Structured observations with units, source and capture time |
| Reminders | create/edit/delete/list day events | Personal/team task/reminder capability; distinguish from patient notification |
| Payment collection | department/doctor/summary/detail views | Cashier/manager mobile projection; high-risk writes may remain web-first |
| Documents/images | camera/gallery, resize, upload/update/compare | Shared document service, category, hash, scan/quarantine, audit |

The staff app contains explicit logic to hide a patient's phone number for some doctor/view-only roles. This proves a real privacy requirement, but the rule must move to server-authorized field projection. UI hiding alone is not access control.

## Patient app capability map

The authenticated patient menu exposes nine primary entries:

1. Current Appointments.
2. Appointments History.
3. Payment History.
4. Treatment Packages.
5. Find a Doctor.
6. Available Appointment Slot.
7. Quick Appointment Request.
8. Change Password.
9. Contact Us.

Other flows found in pages/view models:

| Workflow | Legacy evidence | Target interpretation |
|---|---|---|
| Account lifecycle | activate account, confirm, login, device login, forgot/change password, logout | Modern patient identity, verified contact, recovery and revocable sessions |
| Guest discovery | guest doctor/treatment search and guest appointment request | Public catalog/slot search with rate limiting; verified conversion before confirmed booking |
| Doctor search | three-step search including guest path | Practitioner directory by clinic, specialty/service and date |
| Treatment search | three-step treatment/service search including guest path | Service-first booking journey |
| Slot choice | available slot cells, authenticated/guest selection | Slot search + short hold + idempotent confirmation |
| Appointment management | current/history/detail/edit/cancel/copy request | Book/reschedule/cancel/rebook with policy and status lineage |
| Quick request | direct appointment request | Requested state or callback request; do not present as confirmed slot |
| Packages/contracts | contract list/detail/bookings/payments | Optional entitlement/package module after current business validation |
| Payment history | payment list/detail | Patient-safe billing projection and receipt access |
| Profile | demographics, photo, contact/mobile change request | Patient self-service with verified high-risk contact change |
| Clinic access | contact list, phone action, directions | Branch contact/directions using approved map deep link |

## Legacy backend contract evidence

Both apps call a WCF-style `DeviceService.svc` over hand-built HTTP and parse the JSON `d` envelope. Important operation families include:

- Staff: login/device login/logout, roles, locations/services/providers, schedules, patient search/profile, appointment status, assessment/vitals/treatment/progression, reminders, payments, document/image upload.
- Patient: account activation/reset/login/device login/logout, clinic/service/provider search, available bookings, guest/authenticated appointment requests, edit/cancel request, contracts/packages, payments, profile/mobile change, images, contacts.

These operation names are useful for traceability but must not become the new API design. Create versioned resource/use-case contracts in `BookDoc2026.Contracts` and typed methods in `BookDoc2026.Client`.

## Critical technical and security findings

| Finding | Impact | Required target response |
|---|---|---|
| Plain HTTP endpoints | Credentials and health data can be intercepted/modified | HTTPS only, valid certificates, no transport downgrade |
| Shared secret embedded in both binaries | Anyone can extract and replay it | Revoke/rotate it; never treat an app secret as authentication |
| Device identifier can drive automatic login | Device ID is not a secure authenticator | Rotating refresh sessions in secure storage, server revocation, optional biometric local unlock |
| Password appears in a staff session model and reset is assembled into a query string | Password exposure in memory, logs, proxies/history | Passwords only in request bodies over TLS; never return/store them; secure recovery tokens |
| Fixed “internet user” identity used for patient writes | Weak attribution/audit | Authenticated subject or restricted guest workflow identity with correlation and verification |
| Static IPs and endless connection retry loop | App can hang and environment configuration is embedded | Environment-based HTTPS base URL, bounded retry/backoff, cancellation and offline state |
| A new `HttpClient` is created repeatedly | Socket/performance and inconsistent policy problems | DI-managed typed `HttpClient`, resilience handlers and uniform error mapping |
| ViewModels are wire contracts | UI and API evolve together; over-posting risk | Separate versioned DTOs, domain-independent client models and screen state |
| Static global session/cache state | Lifecycle, stale-scope and memory risks | Scoped session service, observable stores, cache expiry and logout purge |
| Broad exception catches often return message/null | Ambiguous failure and possible internal detail disclosure | Stable error codes, safe messages, telemetry correlation, cancellation support |
| `async void` commands and direct navigation | Unhandled failures and hard-to-test flows | Async command abstraction, navigation service and cancellation/lifecycle tests |
| Client-only role/contact hiding | Data may still be returned to unauthorized devices | Server field-level projection and authorization tests |
| Raw byte image upload and URL-based image access | Type/content/access risk | Authorized document endpoint, limits, signature checks, hashing, scanning and opaque storage |
| No test projects | Existing behavior is unverified | Characterization tests from captured scenarios plus new unit/integration/device suites |

All embedded secrets and any related server credentials/tokens must be treated as exposed and rotated. Do not copy their values into BOOKDOC2026 documentation, configuration, source, or test fixtures.

## What can be reused

### Reuse as requirements

- Staff and patient persona separation.
- Menu vocabulary and task grouping, after user interviews.
- Multi-step appointment, assessment, treatment, progression and vitals workflows.
- Guest doctor/service discovery and appointment request distinction.
- Appointment current/history/detail/edit/cancel/rebook behavior.
- Patient package/payment history, profile/contact update, clinic contact/directions.
- Phone masking requirement, camera/gallery need, responsive portrait/landscape layouts.
- Legacy request/response fields as migration and API traceability inputs.

### Reuse selectively as assets or algorithms

- Icons/images only after rights, duplication, quality and contrast review.
- Simple converters, validation concepts, age/date display and image-resize behavior only after tests and modernization.
- Platform capability intent (camera/gallery/call/directions), implemented with supported MAUI abstractions.

### Do not port

- `DataHelper`, `ConnectionInfo`, embedded endpoints/secrets, WCF `d` envelope handling.
- Device-ID authentication, legacy session models, global `App` state.
- Xamarin custom renderers, `MasterDetailPage`, UWP heads, repeated portrait/landscape pages.
- ViewModel-as-DTO types, reflection/`Activator` navigation, `async void` business commands.
- Client-calculated authorization, raw image URLs or direct old service operation names.

## Target screen mapping

### MAUI patient Stage M1

| Target route | Legacy sources | API prerequisites |
|---|---|---|
| `/auth/*` | activation, login, forgot/change password, confirmations | patient activation/recovery/session endpoints |
| `/home` | current appointments/main detail | patient dashboard projection |
| `/book/service` | search treatment | public/patient service catalog |
| `/book/practitioner` | find doctor | practitioner directory |
| `/book/slots` | booking slot/search pages | slot search + hold |
| `/book/confirm` | add/guest appointment steps | idempotent book/request endpoint |
| `/appointments` | current/history | patient-scoped paged query |
| `/appointments/{id}` | detail/edit/cancel/copy | policy, reschedule/cancel/rebook commands |
| `/billing` | payment history/detail, package payments | patient billing projection/receipts |
| `/packages` | contracts/treatment packages | optional entitlement projection |
| `/profile` | detail/edit/image/mobile change | self-service and verified contact change |
| `/clinic/{id}` | contact/directions | public branch contact/location |

### MAUI staff Stage M2/M3

| Target route | Legacy sources | API prerequisites |
|---|---|---|
| `/agenda` | doctor schedule/appointment overview | staff agenda projection and scope |
| `/queue` | schedule and appointment statuses | check-in/queue commands |
| `/patients/search` | search existing patient | masked scoped search |
| `/patients/{id}` | detail/bookings/payments/documents/images | permission-shaped timeline |
| `/appointments/new` | create appointment steps | staff slot/overbook policy and idempotency |
| `/encounters/{id}` | clinical notes, treatment, progression, vitals | draft/version/sign contracts |
| `/tasks` | reminder create/overview/edit/delete | scoped task/reminder module |
| `/collections` | payment collection/overview | cashier projection and approval rules |
| `/profile` | staff user detail/image/password | account/session self-service |

## Validation backlog

Before accepting these workflows into the new product backlog:

1. Run both legacy apps against a safe environment or capture videos/screenshots from active users.
2. Mark each menu/page as used, unused, broken, duplicate, or missing.
3. Record exact booking/request/status rules and which app action writes which legacy table.
4. Confirm whether treatment packages/contracts and mobile payment collection are still active business needs.
5. Confirm staff personas and whether doctors, receptionists, therapists and cashiers share one app or need policy-shaped deployments.
6. Confirm patient guest-booking abuse controls, identity verification and duplicate-patient handling.
7. Create golden mobile journeys with expected server results, messages and exception paths.
8. Inventory telemetry/crash, push notification, store identities/signing, privacy declarations and currently supported devices if available.

## Discovery conclusion

The Xamarin apps materially reduce product-discovery uncertainty: they prove that both staff mobility and patient self-service were intended and expose mature workflow vocabulary. They do not reduce the security or architecture work. The new MAUI app should preserve validated user journeys while replacing every transport, authentication, authorization, state-management and API coupling pattern described above.
