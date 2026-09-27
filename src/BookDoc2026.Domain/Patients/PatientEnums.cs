namespace BookDoc2026.Domain.Patients;

public enum PatientStatus
{
    Active = 1,
    Inactive = 2,
    Blocked = 3,
    Deceased = 4,
    Merged = 5
}

public enum PatientRelationshipType
{
    Spouse = 1,
    Father = 2,
    Mother = 3,
    Child = 4,
    Son = 5,
    Daughter = 6,
    Brother = 7,
    Sister = 8,
    Guardian = 9,
    Dependent = 10,
    Other = 99
}
