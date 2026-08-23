# 18 — Terminology and Data Ownership Dictionary

## Core terminology

| Term | Planning definition | Owner |
|---|---|---|
| Tenant | independent clinic SaaS customer and primary isolation boundary | Platform |
| Organization | legal/business entity inside a tenant | Organization module |
| Branch | physical/operational clinic location with time zone, numbering and scope | Organization module |
| Patient | person receiving or requesting care within a tenant | Patient Registry |
| Patient account | authenticated self-service identity; not the clinical Patient aggregate | Identity/Patient Portal |
| Representative | authorized person acting for a patient under recorded relationship/authority | Patient Registry |
| Employee | workforce relationship with an organization; payroll details deferred | Workforce |
| Practitioner | credentialed care provider eligible for defined services/locations | Workforce/Clinical |
| Service | catalog offering with duration, eligibility and pricing references | Catalog |
| Resource | schedulable provider, room, modality, equipment, team, chair or bed category | Resource Scheduling |
| Appointment | accepted reservation for patient, service and required resources | Scheduling |
| Visit/check-in | patient's operational arrival episode | Queue/Encounter boundary |
| Queue ticket | operational waiting/service progression; not a clinical record | Queue |
| Encounter | clinical care episode and versioned documentation | Clinical |
| Investigation order | request for lab/imaging activity and its controlled lifecycle | Investigations |
| Physiotherapy care plan | versioned goals, measures, precautions, planned interventions and course status across therapy sessions | Clinical/Physiotherapy |
| Therapy session | one performed physiotherapy contact with interventions, response, measures and author | Clinical/Physiotherapy |
| Home exercise program | versioned patient instructions; not proof that an exercise was performed | Clinical/Physiotherapy |
| Orthopaedic assessment | encounter content describing musculoskeletal history/examination, body site, laterality and plan | Clinical/Orthopaedics |
| Procedure record | evidence that an authorized procedure was actually performed; distinct from order or schedule | Clinical |
| Admission | future inpatient episode; not equivalent to appointment or bed reservation | Future Inpatient |
| Invoice/receipt | issued financial obligation/evidence with immutable snapshots | Billing |

## Ownership rules

- A module writes its own tables and exposes contracts/events for other modules; direct cross-module table updates are prohibited.
- Identity owns authentication and grants, not practitioner credentials or patient demographics.
- Stakeholder owns shared party truth inside a tenant. A Stakeholder is exactly one Person or Corporate and owns reusable contacts, addresses, identifiers and document references.
- Patient Registry owns the patient role, patient number, clinical registration status and patient-specific attributes. A Patient must reference a Person stakeholder; it does not duplicate the person's name, birth date, sex, contacts, addresses, identifiers or documents.
- The implemented Practitioner role references a Person Stakeholder while Workforce owns credentials and assignments. Future employee, guardian, payer, supplier and corporate relationships should use the same rule: reference Stakeholder truth while each module owns only its role-specific state.
- Scheduling owns appointments/reservations; Queue owns service progression; Clinical owns encounter truth.
- Reporting owns definitions/executions/snapshots but reads module-owned projections, not unrestricted tables.
- Documents owns binary storage metadata; the originating module owns meaning, access and retention classification.
- Legacy IDs remain mapping evidence and never become authorization scope.

## Vocabulary decisions required

Approve Indian-market labels and translations for clinic/customer, doctor/practitioner, patient/customer, visit/encounter, test/investigation, receipt/payment, cancellation/refund and bed reservation/admission. UI aliases may vary by tenant, but contracts and database terms remain stable.
