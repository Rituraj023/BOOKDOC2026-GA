# 28 — Resource Catalog Reference Slice

Owner: Catalog/Resources + backend  
Status: Implemented reference slice; review required  
Last reviewed: 2026-08-09

## Outcome

The Resource Catalog reference slice is implemented on the shared `BookDoc2026_Dev` Microsoft SQL Server database. It provides the service and resource definitions required before generalized availability and atomic multi-resource scheduling can be built.

## Ownership boundary

This slice owns:

- tenant-wide clinical/operational service definitions;
- hierarchical tenant resource categories;
- branch-owned bookable resources;
- exclusive and pooled capacity declarations;
- resource-to-service capabilities;
- service-to-category resource requirements;
- immediate resource operational state and append-only status history.

It does not own practitioner licensing or employment, calendars, recurrence, slot holds, booking reservations, queue work, bed occupancy, radiology studies or billing prices. A practitioner resource may carry a future Workforce aggregate ID as `ExternalReferenceId`; it is not itself the practitioner credential record.

## Supported resource kinds

| Kind | Examples | Current catalog behavior |
|---|---|---|
| `Practitioner` | doctor, therapist, technician | exclusive or explicitly pooled; future Workforce link |
| `Space` | consultation, procedure or imaging room | branch resource and service capability |
| `Bed` | day-care/observation category or chair | reservation capability only; not inpatient occupancy |
| `ImagingModality` | X-ray, CT, MRI, ultrasound | exclusive machine and service-duration override |
| `Equipment` | ECG or physiotherapy equipment | individual or pooled capacity |
| `Team` | technician or service team pool | pooled capacity |
| `ServicePoint` | collection desk, pharmacy or cashier | future queue integration |

Categories can be hierarchical. A category code is unique within a tenant; a resource code is unique within a branch.

## API surface

| Method and route | Permission | Purpose |
|---|---|---|
| `POST /api/v1/branches/{branchId}/catalog/services` | `Catalog.Manage` | Create a tenant service |
| `GET /api/v1/branches/{branchId}/catalog/services` | `Catalog.View` | List active or all tenant services |
| `PUT /api/v1/branches/{branchId}/catalog/services/{serviceId}` | `Catalog.Manage` | Versioned service update/retirement |
| `POST /api/v1/branches/{branchId}/catalog/services/{serviceId}/resource-requirements` | `Catalog.Manage` | Declare a required/optional resource category role |
| `POST /api/v1/branches/{branchId}/catalog/resource-categories` | `Catalog.Manage` | Create a root or child category |
| `GET /api/v1/branches/{branchId}/catalog/resource-categories` | `Catalog.View` | List categories |
| `PUT /api/v1/branches/{branchId}/catalog/resource-categories/{categoryId}` | `Catalog.Manage` | Versioned category update/retirement |
| `POST /api/v1/branches/{branchId}/resources` | `Resources.Manage` | Create a branch resource |
| `GET /api/v1/branches/{branchId}/resources` | `Resources.View` | List/filter branch resources |
| `PUT /api/v1/branches/{branchId}/resources/{resourceId}` | `Resources.Manage` | Versioned resource/capacity update |
| `POST /api/v1/branches/{branchId}/resources/{resourceId}/capabilities` | `Resources.Manage` | Attach an active service capability |
| `POST /api/v1/branches/{branchId}/resources/{resourceId}/status` | `Resources.Status.Manage` | Record availability, maintenance, cleaning or block state |

All endpoints require current tenant, durable branch grant and permission. Tenant query filters and composite keys remain the secondary isolation boundary.

## Database implementation

The current `NumericKeyBaseline` creates the `catalog` and `resource` schemas with:

- `catalog.service`;
- `catalog.service_resource_requirement`;
- `resource.resource_category`;
- `resource.bookable_resource`;
- `resource.resource_capability`;
- `resource.resource_status_event`.

Status history uses one composite tenant + branch + resource foreign key. The clean baseline is applied to local `BookDoc2026_Dev`; all internal entity keys are non-identity `bigint`, while API IDs are protected strings under [DOC-031](31-numeric-and-protected-identifier-reference.md).

Database checks enforce enum ranges, service duration, exclusive capacity of one, pooled-capacity bounds, capability duration/capacity and required quantities. Unique indexes protect tenant/branch codes, capabilities and service requirements.

## Important invariants

- exclusive resources always have capacity one;
- pooled capacity is between 1 and 1,000;
- service duration and overrides are 5 to 1,440 minutes;
- only active services/resources/categories accept new relationships;
- a capability cannot require more than the resource capacity;
- resource capacity cannot be reduced below an active capability requirement;
- a category cannot retire while an active resource or service requirement uses it;
- inactive resources cannot become available;
- repeated or stale status transitions are rejected;
- status reason and actor are retained in append-only history;
- service requirements describe what Scheduling must reserve but do not reserve anything themselves.

## Verification evidence

The current solution baseline has 50 tests: 20 unit, 7 architecture and 23 integration tests. Catalog-specific coverage proves:

- domain capacity, duration and version rules;
- CT service, modality category, scanner resource, requirement and capability flow;
- filtered resource discovery;
- audited maintenance/cleaning transitions;
- duplicate code, missing permission and stale version rejection;
- unsafe category retirement and capacity reduction rejection;
- forged cross-tenant branch denial;
- full HTTP authorization flow;
- real SQL Server migration, translated queries, relationships and status persistence inside a rolled-back transaction.

## Deliberately deferred

- Workforce practitioner and branch-assignment aggregates;
- specialty catalogs and practitioner-service eligibility;
- resource-to-resource dependencies and compatible-resource rules;
- effective-dated capabilities and temporary downtime intervals;
- availability recurrence and exceptions;
- prices, taxes and payer-specific catalog entries;
- investigation and medication catalogs;
- bulk import, templates and Admin UI;
- performance/load testing at final tenant volumes.

The next recommended module is Scheduling Foundation: availability rules and exceptions, advisory availability search, expiring holds, and SQL Server-protected atomic reservation across every required resource.
