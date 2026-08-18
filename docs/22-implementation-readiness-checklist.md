# 22 — Implementation Readiness Checklist

## Global readiness gate

Implementation should not start broadly until these are approved:

- product boundary and first release outcomes;
- Delhi pilot compliance applicability register and named legal/clinic reviewer;
- tenant/organization/branch and platform-support model;
- platform-controlled clinic application, verification, approval, provisioning, suspension and closure workflow;
- named roles, host access, permission/scope matrix and separation of duties;
- terminology and module data ownership;
- expected scale, availability, RPO/RTO, retention and accessibility targets;
- initial vendor/integration decisions or safe stubs;
- production database discovery and anonymized migration evidence;
- architecture ADRs, environment strategy and reference vertical slice backlog.

## Per-module ready checklist

| Area | Required before implementation |
|---|---|
| Product | user outcome, priority, release, owner and explicit exclusions |
| Workflow | normal path, exceptions, state machine and business examples |
| Security | permission, tenant/branch/record scope, masking, approval and audit |
| Data | aggregate owner, concepts, retention, migration source and indexes/concurrency plan |
| Contract | command/query shapes, errors, idempotency, versions and client consumers |
| UI | sitemap/flow, responsive/accessibility states and dangerous-action behavior |
| Side effects | events, Worker job, message, report, document or print behavior |
| Tests | acceptance examples, negative authorization, isolation, concurrency and reconciliation |
| Operations | telemetry, alert/runbook, feature flag, rollout and rollback/forward-fix |
| Platform hardening | correlation/redaction, audit-vs-telemetry ownership, deterministic time, configuration inheritance, compatibility, workload limits and degraded behavior |

## First reference-slice readiness

Recommended slice: provision a test tenant, create a branch and branch-scoped role, expose one catalog item through API/typed client/Admin, write audit evidence, execute one durable Worker job and prove another tenant cannot observe any data. It should include migration, authorization, integration and architecture tests before mass scaffolding.

## Stop conditions

Do not implement a module when its tenant boundary, record owner, safety-critical state transition, acceptance owner, legal dependency or migration source is unknown. Record the blocker in [Pre-development Decisions and Questionnaire](23-pre-development-decisions-and-questionnaire.md) and continue with an independent, ready preparation item.

Do not introduce a new shared project, distributed cache, search engine, feature-flag vendor or telemetry vendor only for architectural symmetry. Apply the evidence and ownership test in [Cross-Cutting Platform Hardening](34-cross-cutting-platform-hardening.md) first.
