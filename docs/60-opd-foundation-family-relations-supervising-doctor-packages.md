# 60 — OPD Foundation: Patient Family Relations, Supervising Doctor Association, and Package Session Linkage

- **Document Identifier**: DOC-060
- **Owner**: Clinical + Patients + Contracts + Workforce + Frontend Portal
- **Status**: Implemented, Verified & Synchronized with Remote Git
- **Date**: 2026-09-27
- **Target Solution**: `BookDoc2026-GA.slnx`

---

## 1. Executive Summary & Outcome

This delivery establishes complete production parity and modern clean-architecture support for the three core OPD clinical requirements requested by clinic operations, preceding the IPD/Hospital expansion:

1. **Patient Family Member Relations**: Enables linking family members (spouse, child, parent, sibling, guardian) to a primary contact account, allowing multi-member booking and reception check-in from a single phone number without duplicate identity issues.
2. **Senior Doctor / Junior Doctor Association**: Links an attending/resident practitioner with a supervising senior consultant doctor across appointments and clinical encounter lifecycles.
3. **OPD Treatment Package Session Linkage & Independent Sessions**: Distinguishes package-covered sessions (consuming pre-paid visit entitlements with zero cashier fee) from standalone pay-per-visit sessions, and equips clinic staff (at reception and cashier) with 1-click tools to link or unlink sessions to/from active packages.
4. **OPD Portal Workflows**: Delivered a complete OPD Reception Desk (`/reception`), package linkage inspector in Cashier (`/billing/cashier`), and a structured Prescription (Rx) writer with clinical summary printing in Doctor Clinical Encounters (`/clinical/encounters`).

---

## 2. Legacy Evidence Preserved & Architecture Realignment

| Feature Area | Legacy Implementation (`BookDocAppointment`) | Modern Architecture (`BOOKDOC2026-GA`) |
| :--- | :--- | :--- |
| **Family Relations** | `CustomerRelationInfo` (`CustomerId`, `RelativeId`, `RelationId`). Raw numeric primary keys in public interfaces. | `PatientRelation` domain entity. Encoded `PublicIdKind.PatientRelation`. Strongly typed enum `PatientRelationshipType`. Idempotent API with audit logging. |
| **Senior/Junior Doctor** | `BookingObjectAssociateInfo` (`ObjectId`, `Supervise (bool)`). Hard-coded boolean flag without relational integrity. | `ClinicalEncounter.SupervisingPractitionerId` foreign key. Indexed in `clinical.clinical_encounter`. Included in `StartEncounterRequest`, `ReviseEncounterDraftRequest`, and `EncounterResponse`. |
| **Package Linkage** | Procedural stored procedure `SP_INCLUDEBOOKINGINPACAGE` and MVC partial `_ChangePaymentContract.cshtml`. | `ContractService` methods `LinkBookingToPackageAsync` & `UnlinkBookingFromPackageAsync`. Deducts from `ContractEntitlement.ConsumedUnits`. Returns immutable snapshots. |
| **Prescription Writer** | Unstructured free-text box in legacy Windows forms and web forms. | Structured `PrescriptionItem` model (Drug Name, Dosage, Frequency `1-0-1`, Duration, Advice). Formatted in draft instructions and rendered on a clinic-branded printable prescription slip. |

---

## 3. Domain Model Implementations

### 3.1 Patient Family Relations (`BookDoc2026.Domain.Patients`)
- **Entity**: `PatientRelation` inheriting `TenantScopedEntity`.
  - `PrimaryPatientId` (foreign key to `patient.patient`)
  - `RelatedPatientId` (foreign key to `patient.patient`)
  - `RelationshipType` (`Spouse`, `Child`, `Parent`, `Sibling`, `Guardian`, `Other`)
  - `IsEmergencyContact` (bool)
  - `IsGuardian` (bool)
  - `Notes` (string, max 500 chars)
  - `Version` (optimistic concurrency counter)
- **Validation**:
  - Self-relationship forbidden (`PrimaryPatientId != RelatedPatientId`).
  - Tenant boundary isolation enforced.

### 3.2 Supervising Practitioner Association (`BookDoc2026.Domain.Clinical`)
- **Entity**: `ClinicalEncounter`
  - Added `long? SupervisingPractitionerId` (foreign key to `workforce.practitioner_profile`).
  - Added method `AssignSupervisingPractitioner(long? practitionerId, DateTimeOffset now)` enforcing encounter draft status.
- **Roles in Workforce**:
  - `PractitionerRoleCodes`: `Attending`, `Supervising`, `Assisting`.

### 3.3 Package Link / Unlink Ledger (`BookDoc2026.Domain.Contracts`)
- **Entities**: `ContractAgreement`, `ContractEntitlement`, `EntitlementReservation`.
- **Invariants**:
  - `AvailableUnits = TotalUnits - ConsumedUnits - ReservedUnits`.
  - Linking a booking consumes/reserves 1 visit from active package entitlement.
  - Unlinking releases the reservation and returns entitlement units to the patient's balance.

---

## 4. API Endpoints & Contracts

All endpoints enforce tenant isolation, branch authorization policies, and obfuscate database IDs using `IPublicIdCodec`.

### 4.1 Patients & Family Relations (`PatientsController.cs`)
- `GET /api/v1/branches/{branchId}/patients/{patientId}/relations`: List all family members linked to patient.
- `POST /api/v1/branches/{branchId}/patients/{patientId}/relations`: Add and link a family member.
- `DELETE /api/v1/branches/{branchId}/patients/{patientId}/relations/{relationId}`: Unlink a family member.
- `POST /api/v1/branches/{branchId}/patients`: Register new patient or family member profile.
- `GET /api/v1/branches/{branchId}/patients/{patientId}`: Fetch patient demographics.

### 4.2 Practitioners & Senior Doctors (`PractitionersController.cs`)
- `GET /api/v1/branches/{branchId}/practitioners`: List all active doctors/practitioners in branch (with names, codes, roles, assigned services).
- `GET /api/v1/branches/{branchId}/practitioners/{practitionerId}`: Fetch single practitioner profile with credentials and assignments.

### 4.3 Treatment Packages (`ContractController.cs`)
- `GET /api/v1/branches/{branchId}/contracts/patient-packages/{patientId}`: List active packages for a patient (shows remaining visit counts and expiry).
- `GET /api/v1/branches/{branchId}/contracts/booking-package-status/{bookingId}`: Check if a booking is linked to a package or independent.
- `POST /api/v1/branches/{branchId}/contracts/link-booking-package/{bookingId}`: Link a booking to an active package.
- `POST /api/v1/branches/{branchId}/contracts/unlink-booking-package/{bookingId}`: Unlink a booking from a package.

---

## 5. Database Schema & EF Core Migration

**Migration File**: `20260927030525_FamilyRelationAndSupervisingDoctor.cs`
- Created table `patient.patient_relation`:
  - `id` (bigint, primary key)
  - `tenant_id` (bigint, not null)
  - `primary_patient_id` (bigint, not null, FK to `patient.patient`)
  - `related_patient_id` (bigint, not null, FK to `patient.patient`)
  - `relationship_type` (varchar(50), not null)
  - `is_emergency_contact` (boolean, not null)
  - `is_guardian` (boolean, not null)
  - `notes` (varchar(500), nullable)
  - `version` (bigint, not null)
  - Unique index on `(tenant_id, primary_patient_id, related_patient_id)`.
- Modified table `clinical.clinical_encounter`:
  - Added `supervising_practitioner_id` (bigint, nullable, FK to `workforce.practitioner_profile`).
  - Added index on `(tenant_id, supervising_practitioner_id)`.

---

## 6. Front-End Portal Implementations

### 6.1 OPD Reception Desk (`src/BookDoc2026.Portal/Pages/Reception.razor`)
- **Route**: `/reception` (added to topbar navigation).
- **Patient & Family Member Triage**:
  - Live search by mobile, patient ID, name, or email.
  - Displays primary account holder card with linked family member selector chips.
  - 1-click choice: "Check in Primary Patient" vs "Check in [Family Member Name] ([Relationship])".
  - Modal form to register and link new family members on the fly.
  - Modal form for quick walk-in patient registration.
- **Doctor Selection**:
  - Attending doctor dropdown (filtered to active practitioners).
  - Optional Supervising Senior Doctor dropdown.
- **OPD Package vs Independent Session Toggle**:
  - Auto-checks patient active packages.
  - Radio toggle: "Use Active Package (Deduct 1 visit)" vs "Independent Session (Bill at Cashier)".
- **Token Slip & Printing**:
  - Generates token `#A-0xx`.
  - Printable receipt formatted for thermal or laser slip printers.

### 6.2 Cashier Desk Package Linkage (`src/BookDoc2026.Portal/Pages/Cashier.razor`)
- When a booking is selected/entered:
  - Real-time status badge indicates whether the visit is linked to a package or independent.
  - If covered by package: shows "Covered by Package [PKG-XXX] (X visits remain)" with an **"Unlink Booking from Package"** button.
  - If independent visit and patient has active packages: provides a dropdown and **"Link Booking to Package"** button for retroactive coverage.

### 6.3 Doctor Clinical Encounters & Prescription Writer (`ClinicalEncounters.razor` & `EncounterDraftForm.razor`)
- **Supervising Doctor Assignment**:
  - Allows assigning or changing the senior supervising doctor on encounter drafts.
  - Displays supervisor badge on clinical agenda and patient encounter header.
- **Structured Prescription Writer (Rx)**:
  - Table interface to prescribe multiple medications with Drug Name, Dosage, Frequency (`1-0-1`, `1-1-1`, `SOS`), Duration, and Timing Advice (`After meals`, `Before meals`).
  - Automatically formats the structured prescription into clinical instructions.
- **Print Prescription Slip**:
  - Generates a clinical prescription slip with clinic header, patient details, attending doctor, supervising doctor, diagnosis, Rx table, and doctor signature block.

---

## 7. Verification & Automated Test Status

The full automated test suite was executed across all projects:
- **Unit Tests**: 100 passed, 0 failed.
- **Architecture Tests**: 9 passed, 0 failed (NetArchTest enforcing zero boundary leaks).
- **Integration Tests**: 35 passed, 0 failed (End-to-end API, DbContext, and Security).
- **Total**: **144 tests passed (100% green)**.
- **Git Remote Synchronization**: Committed and pushed to `origin/master` and `origin/main` on GitHub (`https://github.com/Rituraj023/BOOKDOC2026-GA.git`).
