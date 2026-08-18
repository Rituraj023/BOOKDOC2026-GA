# 13 — Stakeholders, Roles and Permission Preparation

## Confirmed initial roles

| Role | Primary host | Dashboard intent | High-risk controls |
|---|---|---|---|
| Platform operator | Control plane only | tenant/service health | no standing clinical access; support elevation audited |
| Clinic owner | Admin and permitted Portal | business, branch, revenue and compliance | cross-branch and export permissions explicit |
| Clinic administrator | Admin and permitted Portal | configuration, users and operations | cannot silently grant own restricted permissions |
| Receptionist | Portal; limited Admin only if granted | arrivals, bookings, queues and tasks | masked clinical/financial fields |
| Doctor/practitioner | Portal and MAUI | agenda, queue, encounters and results | scoped care relationship; signing authority |
| Nurse | Portal and later MAUI | assigned queues, vitals, tasks and care work | no prescription/signing unless separately authorized |
| Cashier | Portal | invoices, collections and close | refund/reversal/discount separation of duties |
| Radiology technician | Portal | modality worklist and queue | result verification/release separately permitted |
| Lab technician | Portal | collection/worklist/results | result verification/release separately permitted |
| Patient | Patient Portal and MAUI | own bookings, queue state, bills and released records | self-only, verified representatives handled separately |

Additional roles such as auditor, finance manager, call-centre user, pharmacist, records officer and hospital roles are added only after workflow approval.

## Permission model

Effective access is the intersection of:

```text
authenticated user
  + allowed host
  + tenant membership
  + active role/permission grant
  + organization/branch/service scope
  + record relationship or sensitivity rule
  + current record state
```

- Permissions are capabilities such as `Patients.View`, `Appointments.Create`, `Encounters.Sign`, `Payments.Refund`, `Reports.Schedule` and `Printing.Reprint`; role names never substitute for server checks.
- Deny by default. Scope is durable server data, not a client-supplied tenant or branch header.
- A role may receive a dashboard in Admin and Portal only when host access and card/query permissions are granted.
- Admin access additionally requires the approved network/IP route. IP allow-listing is defense in depth and never replaces identity, permission or tenant/branch scope.
- Portal and future mobile apps are internet-facing authenticated clients; mobile experiences reuse approved Portal use cases rather than Admin management functions.
- Sensitive clinical access, cross-branch access, bulk export, patient merge, report scheduling, discounts, refunds and support elevation require distinct permissions.
- Delegation, temporary coverage and emergency access require expiry, reason and audit evidence.

## Separation-of-duty candidates

- user creation versus privileged-role approval;
- invoice creation versus high-value discount approval;
- collection versus refund/reversal approval;
- clinical drafting versus signing/verification where applicable;
- report creation versus cross-branch scheduled distribution;
- print submission versus reprint of receipts or labels;
- platform support request versus tenant-data elevation approval.

## Permission-workshop output

Before each module begins, create a matrix with persona, host, command/query, tenant and branch scope, field masking, state conditions, approval/reason, audit event, notification and tests. Named clinic representatives must approve the matrix; UI hiding alone is never acceptance evidence.
