# 20 — Integrations and Vendor Readiness

## Integration register

| Integration | Release intent | Decisions/evidence required |
|---|---|---|
| Email | Clinic MVP | provider, verified domains, templates, bounce/complaint webhook, India hosting/privacy review |
| WhatsApp Business | Clinic MVP | approved BSP, business/template approval, opt-in/evidence, webhook signatures, pricing/limits |
| SMS | fallback/approved cases | DLT/entity/header/template requirements, provider, consent and delivery receipts |
| Mobile push | MAUI M1 | FCM/APNs ownership, device-token lifecycle, privacy-safe payload and deep links |
| SignalR | Clinic MVP | authenticated groups, reconnect/API reconciliation, scale and backplane decision |
| Payment gateway | when online payment approved | provider, webhook signatures, idempotency, settlement/refund reconciliation |
| GST/e-invoice/accounting | tenant-dependent | tax adviser decision, registration/threshold/applicability, export format/API |
| ABDM/ABHA | separately approved increment | HFR/HPR readiness, sandbox, consent manager/HIU/HIP role and certification requirements |
| PACS/RIS/LIS/analyzers | future or selected pilot | standards/protocol, vendor sandbox, patient/order/result matching, downtime workflow |
| Object storage/malware scan | Clinic MVP | India region, encryption/key control, retention, signed access and scanning SLA |
| Local printing | after browser/PDF gate | printer/label models, media, driver, workstation policy and branch pairing |

## Adapter and ownership rules

- Domain/Application code depends on a platform interface, never a vendor SDK directly.
- Secrets remain in managed configuration; tenant-specific credentials are encrypted, scoped and rotated.
- Incoming callbacks verify signature, timestamp/replay window, tenant/provider mapping and idempotency before state change.
- Accepted callbacks first enter a durable inbox and are reconciled before ambiguous external results trigger another side effect; detailed delivery rules are in [Cross-Cutting Platform Hardening](34-cross-cutting-platform-hardening.md).
- Provider outage, rate limit, partial success, ambiguity, reconciliation and contract/version change have tested behavior.
- No provider receives more patient/clinical data than the approved purpose requires.
- Contracts name service owner, business owner, support path, SLA, cost/limit, data location, subprocessor terms, retention and exit plan.
- Email/WhatsApp/SMS configuration supports tenant defaults and verified branch-specific sender identities where the provider and applicable registration permit them; a branch cannot enter an unverified arbitrary sender.

## Vendor selection gate

Before procurement or implementation, record requirements, at least two feasible options where available, security/privacy/legal review, sandbox proof, total cost, lock-in/export plan and approval. The planning references in [Backend Workers, Reporting and Printing](11-backend-workers-reporting-printing.md) are proposed technologies, not package or vendor authorization.
