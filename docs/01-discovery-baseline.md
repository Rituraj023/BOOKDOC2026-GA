# 01 — Discovery Baseline

## Objective

Record what is demonstrably present in the old application and in the new architecture reference. This prevents planning from confusing file names with finished, production-safe capability.

## Repository evidence

### Legacy `BookDocAppointment`

Observed solution characteristics:

- Classic .NET Framework 4.7.2, ASP.NET MVC/Web API, Entity Framework 6, ASP.NET Identity 2, WinForms reporting/utility application, Windows messaging service, and installer projects.
- 12 solution projects, including Models, Data, Service, Web, Reporting, WinApp, Messaging, setup projects, HTML parsing, and utilities.
- Approximately 87 `*Info` model files, 78 service files, 38 custom repositories, 69 web controllers, and 14 non-designer WinForms forms.
- Database-first/code-first hybrid behavior: EF mappings plus stored procedures, views, functions, triggers, direct SQL strings, and manual SQL scripts.
- `ApplicationDbContext` disables automatic initialization and maps many uppercase shared tables into one context.
- Several repositories construct SQL with concatenated identifiers or values. These behaviors must be rewritten with parameterized EF/LINQ or reviewed stored procedures.
- A database credential is present in legacy configuration. It is considered compromised and must be rotated; it is intentionally not repeated here.

Feature evidence found in models, services, controllers, views, or SQL usage:

| Area | Evidence | Confidence |
|---|---|---|
| Identity and access | users, roles, user administration, sessions/login | High that code exists; authorization completeness unknown |
| Organization/location | accommodation, rooms, wards, units, setup, company contacts | Medium; terminology is overloaded |
| Patient/customer | customer, relation/family, occupation, nationality, documents, login, medical history | High |
| Staff/provider | employee, object/provider, specialty, availability/open time, leave/day events | High |
| Scheduling | booking, statuses, procedures, associates, requests, walk-ins, packages, shifts | High and likely most mature |
| Clinical | assessment, vitals, diagnosis/complaints, prescriptions, drugs, tests, treatment, physiotherapy | Medium; record-signing and safety rules not evident |
| Finance | contract/tariff, invoices/details, payments, coupons, adjustments, pay modes, reports | High that workflows exist; accounting correctness needs reconciliation |
| Inpatient | admission, ward, room, bed, bed allocation | Low-to-medium maturity; entities are thin |
| Communications | event messages, SMS/WhatsApp/email utilities, Windows message service | Medium; security/reliability needs replacement |
| Reporting | stored-procedure reports, report viewer/printer/exporter, WinForms report screens | High for report inventory, low for portable implementation |

The Windows messaging service was inspected in more detail. It polls database message rows, renders file-based templates/bookmarks, sends SMTP email, provider SMS and WhatsApp, retries/reschedules work, executes repeat SQL/stored-procedure strings, checks connectivity and cleans local logs. Preserve the required delivery/scheduling behavior, but replace direct database polling, arbitrary SQL execution, timer concurrency, local file/config coupling and machine-local logging.

The reporting project contains 88 RDLC files: 45 primary layouts and 43 export duplicates or variants. ReportViewer/WinForms provides preview, PDF, Excel and physical print for appointment, cancellation, category, patient/visit, event, monthly, payment, request, invoice and prescription families. Detailed disposition is in [Backend Workers, Reporting and Printing](11-backend-workers-reporting-printing.md).

### Architecture reference `SDTS.ERP2026`

Observed solution characteristics:

- .NET SDK 10.0.301 and a .NET 10 solution.
- API projects divided into Api, Application, Contracts, Core.Domain, Infrastructure, Client, and Shared.Kernel.
- Blazor Admin and Portal hosts plus shared `Blazor.UI`, Aspire AppHost, and ServiceDefaults.
- Test assemblies for API, architecture, infrastructure, unit, Admin, Portal, and shared UI.
- API versioning, standardized API response/validation, OpenAPI/Scalar, JWT/Identity, permission authorization, tenant scope validation, global exception/request middleware, and CORS policy.
- EF Core conventions for lower-case names, restrictive deletes, audit fields, soft-delete filtering, and tenant/organization/branch/financial-year query filters.
- Platform services for queued messaging, recurring jobs, documents, authentication confirmation, and typed client foundations.
- Reference guidance explicitly chooses modular monolith, stable contracts, module-owned tables, outbox/idempotency for side effects, and architecture tests.
- `Mobile`, `MobileUI`, and `MobileTest` directories contain no implementation at review time.

### Legacy Xamarin apps

Two additional mobile solutions use the same BookDoc backend/database:

- `BookDocApp` is the clinic staff app: 241 C# files, 117 XAML files, 56 shared view models and 13 primary menu workflows. It includes provider schedules, appointment operations, patient search/profile/history/documents, clinical notes, treatment/progression/vitals, reminders and payment collection.
- `BookDocCustApp` is the patient app: 145 C# files, 62 XAML files, 33 shared view models and nine primary authenticated menu workflows. It includes account activation/recovery, guest/authenticated doctor or treatment search, slot selection, appointment request/edit/cancel/history, packages, payments, profile/contact changes, clinic contact and directions.
- Both use Xamarin.Forms 5 with Android/iOS/UWP heads, WCF-style hand-built HTTP calls, global in-memory app/session state and ViewModels as wire models. No automated test projects were found.
- Both embed a shared client secret and plain-HTTP/static endpoint strategy; device identity participates in automatic login. These patterns are security findings to eliminate, not migration candidates.
- Detailed evidence and the target mapping are in [Legacy Xamarin Mobile Discovery](09-xamarin-mobile-discovery.md).

## What should be reused from SDTS.ERP2026

Reuse by adapting and renaming, subject to license/ownership confirmation:

- Project/dependency structure and architecture tests.
- Base audit and scope concepts; request scope validation; permission catalog approach.
- API response, paging/filter allow-list, versioning, OpenAPI, exception handling, and typed-client conventions.
- Messaging queue, recurring jobs, documents abstraction, idempotency pattern, and Aspire development orchestration.
- Blazor design primitives, shell, authentication state, grid behavior, feedback, settings, and host composition.
- Test-project layout and CI quality gates.

Do not blindly reuse:

- ERP-specific names such as financial year where a clinic workflow does not require them.
- Generic `BaseService`/`BaseController` behavior for clinical commands; SDTS documentation itself identifies overreach risk.
- Client-supplied scope without durable grant verification.
- Dynamic query behavior without field allow-lists and complexity limits.
- Current mobile folders, because they are placeholders rather than architecture evidence.

## Important unknowns to resolve at G0

1. Actual production SQL Server version, database size, table/view/procedure/function/trigger inventory, row counts, invalid foreign keys, duplicates, and encoding.
2. Number of clinics/branches and whether SaaS multi-tenancy is required or only multi-branch operation.
3. Appointment duration, double-booking, shared-resource, walk-in, cancellation, queue, and timezone rules actually used by staff.
4. Clinical specialties in scope and whether templates/signing/amendments are required for each.
5. Tax, invoice numbering, refund, insurer, credit, package, and daily-close rules by jurisdiction.
6. Mobile persona deployment: the old apps prove patient and mixed clinic-staff experiences, but we still need to decide whether doctor, receptionist, therapist, nurse and cashier journeys share one policy-shaped app or separate distributions.
7. Required integrations: SMS, WhatsApp, email, payment gateway, laboratory, pharmacy, ABDM/health ID, insurance, devices, or accounting.
8. Compliance jurisdiction, consent model, retention, breach response, and hosting/data-residency requirements.
9. Reporting catalog: owner, filters, formula, output, frequency, and whether each report is still used.
10. Cutover tolerance, maintenance window, acceptable downtime, rollback window, and legacy read-only retention.
11. Employee/payroll source database, authoritative ownership, identifiers, attendance/leave/pay rules and integration direction when the user supplies it.
12. Resource categories/capacity rules for beds, imaging modalities, rooms and equipment; whether each is scheduled, queued, allocated or all three.
13. Existing email/WhatsApp templates, provider accounts, opt-in evidence, approved template IDs, webhook behavior and localization needs.
14. Windows-service message types, repeat jobs, schedules, retry states, owners and whether each is still active.
15. Legacy report users, filters, formulas, outputs, frequency, sensitivity and classification as rebuild/merge/replace/retire.

## Discovery deliverables

- Schema/profile report with no patient-identifying values.
- Screen/workflow inventory demonstrated by real users.
- Report and integration catalog.
- Windows-service job/message disposition and active-report catalog with named owners.
- Data classification and retention matrix.
- Approved terminology and module boundaries.
- Feature priority using Must/Should/Could/Won't for clinic MVP.
- Signed assumptions/decision log in document 08.

## Baseline conclusion

The legacy application is a valuable requirements oracle and migration source, but not a safe code-migration source. The strongest likely reuse is scheduling vocabulary, lookup/reference data, billing/report rules, and user-tested workflows. The target should be a clean implementation on SDTS architecture primitives with explicit clinical safety, scope isolation, concurrency, and automated tests.
