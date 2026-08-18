# 24 — Physiotherapy and Orthopaedics Preparation

Status: pre-development specialty baseline  
Pilot jurisdiction: Delhi  
Purpose: define specialty questions and artifacts before clinical implementation; not a clinical-practice protocol.

## Shared specialty foundation

Both specialties use Patient, Appointment, Resource, Queue, Encounter, Diagnosis, Procedure, Prescription, Investigation, Document and Billing foundations. Specialty content extends a common encounter through versioned templates and coded observations; it must not create a separate patient or appointment model.

Required shared capabilities:

- body region, side/laterality, onset/mechanism, pain and functional history;
- allergies, medications, comorbidities, red flags and safety alerts;
- referral/source, goals, diagnosis/problem list, plan and follow-up;
- imaging/investigation order, external result/document and reviewed status;
- clinical document signing/amendment, patient instructions and consent evidence;
- provider/room/equipment scheduling and reusable session/package billing;
- outcome trends that retain the original measurement, unit, tool/version and author.

## Physiotherapy workflow

1. Referral or self-presentation and safety/red-flag screening.
2. Initial assessment: complaint/history, pain characteristics, observation, range of motion, strength, functional limitations and selected validated outcome measures.
3. Goals and versioned care plan: frequency/duration, precautions, planned interventions/modalities and review point.
4. Schedule course of therapy using practitioner, room/equipment and optional package entitlement.
5. Session documentation: attendance, subjective response, measures, interventions, dosage/time where needed, tolerance/adverse event, exercise progression and next plan.
6. Reassessment: compare like-for-like outcome measures, revise goals/care plan and justify continuation/change.
7. Discharge, discontinuation or referral with summary and home exercise program.

Preparation decisions:

- assessment templates by body region and whether clinics can configure them;
- approved pain, range-of-motion, strength and functional outcome scales;
- exercise library ownership, media, language, versioning and patient delivery;
- treatment modalities/equipment, contraindication/precaution prompts and maintenance status;
- package versus pay-per-session rules, missed sessions, expiry, transfer and refunds;
- who may author, supervise, countersign, correct and discharge;
- whether offline MAUI session drafts are ever permitted.

## Orthopaedic workflow

1. History with mechanism/onset, body site/laterality, previous treatment and red flags.
2. Examination with observation, tenderness, movement, strength, stability/special tests and neurovascular findings as applicable.
3. Diagnosis/differential and investigation/imaging order or review.
4. Plan: medication, advice, immobilization/device, clinic procedure, physiotherapy referral, external surgical/hospital referral and follow-up.
5. If a clinic procedure occurs, separately record indication, site/side verification, consent, participants, materials/medication, result, complications and aftercare.
6. Track investigation/result and referral status without treating an external procedure as completed until evidence is received.

Preparation decisions:

- Orthopaedic services/procedures offered in the clinic release;
- body-region, laterality and injury classification dictionaries;
- prescription, implant/device, injection/procedure and consent templates;
- imaging provider/worklist/PACS boundary for the pilot;
- urgent red-flag escalation and external hospital/referral workflow;
- clinical photography/document rules and sensitive access;
- orthopaedic-to-physiotherapy referral, shared goals and feedback loop.

## Resource and queue preparation

- Physiotherapy resource categories may include therapist, cubicle/room, treatment table and modality/equipment.
- Orthopaedic resource categories may include practitioner, examination/procedure room and equipment.
- One appointment may require multiple resources atomically; reusable equipment may require cleaning/turnaround blocks.
- Queue/worklist stages and public displays must not reveal diagnosis or body site.
- Bed/day-care use, if later required for observation or procedure recovery, remains distinct from inpatient admission.

## Specialty reports and documents

Candidate outputs include initial assessment, care plan, session note, outcome-progress chart, exercise program, discharge summary, orthopaedic consultation, imaging/investigation order, procedure note, prescription and referral letter. Each needs owner, permissions, signed/snapshot classification, patient-release rule and golden example before implementation.

## Specialty readiness gate

Implementation begins only after Delhi clinical representatives approve the workflow, fields, outcome tools, professional permissions, red-flag/escalation behavior, signing/amendment, consent, report/document set, billing/package rules and ten anonymized acceptance cases per specialty. Professional-registration and establishment requirements follow [Nonfunctional, India Compliance and Security Preparation](17-nonfunctional-india-compliance-and-security.md).
