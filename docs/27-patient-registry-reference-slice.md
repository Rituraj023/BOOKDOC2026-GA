# 27 — Patient Registry Reference Slice

Owner: Patient Registry + backend  
Status: Implemented; demographic storage superseded by DOC-029  
Last reviewed: 2026-08-09

## Outcome

The Patient Registry increment remains implemented. Patient is a role referencing a Person stakeholder; shared party data belongs to the Stakeholder module. Older direct-patient demographic design is superseded by [DOC-029](29-stakeholder-party-reference-slice.md).

## Database decision

- Database engine remains Microsoft SQL Server through EF Core 10.
- API owns the database configuration and hosts Worker-library services in the same process and dependency container.
- Local development uses `Server=(localdb)\mssqllocaldb` and database `BookDoc2026_Dev`.
- Production must supply its connection string through approved secret/configuration management; no password is stored in the repository.
- One database is divided by owned schemas. This increment adds the `patient` schema.
- The empty local `BookDoc2026_Dev` database was rebuilt from the current `NumericKeyBaseline` migration on 2026-08-09.

This does not authorize connecting to or altering the legacy BookDoc databases. Legacy data will enter only through the reviewed migration and reconciliation process.

## Implemented behavior

- idempotent patient registration using a caller-generated request ID and SHA-256 payload fingerprint;
- tenant-wide Patient role with a branch recorded as the registration source and a required Person Stakeholder link;
- human-readable `PAT-` patient numbers backed by tenant-scoped uniqueness;
- stakeholder-owned honorific/name, date of birth with estimated flag and administrative sex; Patient owns blood group;
- normalized mobile/email contact points and primary-contact enforcement;
- normalized typed identifiers with issuer-aware uniqueness;
- initial Indian address with a six-digit postal-code database constraint;
- exact duplicate detection using name/date of birth, contact or typed identifier evidence;
- no automatic merge; possible duplicates require an explicit override reason and audit event;
- masked search results for mobile and email;
- full patient lookup only with the stronger view permission;
- versioned demographic corrections with conflict responses;
- append-only registration, duplicate-override and demographic-update audit events;
- EF query filters and composite tenant foreign keys for Stakeholder- and Patient-owned records.

## API surface

| Method and route | Permission | Result |
|---|---|---|
| `POST /api/v1/branches/{branchId}/patients` | `Patients.Register` | Idempotently registers a patient |
| `GET /api/v1/branches/{branchId}/patients?q={query}` | `Patients.Search` | Returns up to 25 masked tenant-scoped matches |
| `GET /api/v1/branches/{branchId}/patients/{patientId}` | `Patients.View` | Returns the authorized full registration record |
| `PUT /api/v1/branches/{branchId}/patients/{patientId}/demographics` | `Patients.Update` | Applies a version-checked demographic correction |

Every operation requires the permission, current tenant and branch grant. A branch ID claim forged for another tenant cannot bypass the EF tenant filter.

## SQL Server schema

The current `NumericKeyBaseline` creates:

| Table | Key controls |
|---|---|
| `patient.patient` | Stakeholder and registration-branch links; tenant patient-number and registration-request uniqueness; concurrency version |
| `stakeholder.contact_point` | normalized lookup and uniqueness; filtered primary contact per type |
| `stakeholder.identifier` | unique tenant + type + issuer + normalized value |
| `stakeholder.address` | multiple typed addresses; filtered primary address and India postal-code check |

Deletes remain restricted. The schema stores normalized lookup values as well as display values. Encryption/tokenization and retention decisions must be completed before real patient data is permitted.

## Duplicate and idempotency rules

Registration never merges records. Exact evidence creates a possible-duplicate stop. Authorized staff must review masked results and either select the existing patient or submit a reason for a separate record. Future merge/unmerge work requires its own state machine, permissions and audit design.

Repeating the same registration request ID with the same payload returns the original patient. Reusing it with a different branch or payload is rejected. Database uniqueness remains the final concurrency boundary.

## Verification evidence

The current solution baseline has 50 automated tests: 20 unit, 7 architecture and 23 integration tests. Patient-specific evidence covers:

- normalization, birth-date, blood-group, postal-code and concurrency invariants;
- idempotent registration;
- duplicate stop and audited override without auto-merge;
- masked mobile search;
- permission and stale-version HTTP behavior;
- forged cross-tenant denial;
- SQL Server connectivity, pending-migration check, registration and translated identifier search inside a rolled-back transaction.

The optional real-SQL test is enabled with:

```powershell
$env:BOOKDOC_SQLSERVER_TEST_CONNECTION='Server=(localdb)\mssqllocaldb;Database=BookDoc2026_Dev;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True'
dotnet test tests/BookDoc2026.IntegrationTests --filter FullyQualifiedName~SqlServerPatientSmokeTests
```

## Deliberately deferred Patient Registry work

- contact and address history/effective dates;
- contact verification and communication preference linkage;
- representatives, guardians and authority evidence;
- consent capture, withdrawal and evidence documents;
- alerts, referral sources and patient documents;
- duplicate work queue, manual merge and reversible unmerge;
- patient portal identity linking;
- configurable demographic fields or India-approved terminology;
- encryption/tokenization, retention and patient-request workflows;
- legacy patient mapping/import and reconciliation;
- performance testing at 100,000 and approximately 2,000,000 patients.

These are required before Patient Registry can be considered complete or pilot-ready. The next recommended new module is Resource Catalog, while the deferred Patient Registry capabilities remain explicit backlog gates.
