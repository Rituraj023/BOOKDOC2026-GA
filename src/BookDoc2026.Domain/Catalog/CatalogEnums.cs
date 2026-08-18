namespace BookDoc2026.Domain.Catalog;

public enum CatalogItemStatus
{
    Active = 1,
    Inactive = 2
}

public enum ResourceKind
{
    Practitioner = 1,
    Space = 2,
    Bed = 3,
    ImagingModality = 4,
    Equipment = 5,
    Team = 6,
    ServicePoint = 7
}

public enum CapacityMode
{
    Exclusive = 1,
    Pooled = 2
}

public enum ResourceOperationalStatus
{
    Available = 1,
    Unavailable = 2,
    Maintenance = 3,
    Cleaning = 4,
    Blocked = 5
}
