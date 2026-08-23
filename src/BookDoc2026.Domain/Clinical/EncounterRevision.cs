using BookDoc2026.Domain.Common;

namespace BookDoc2026.Domain.Clinical;

public sealed class EncounterRevision : TenantScopedEntity
{
    private EncounterRevision() { }

    public long BranchId { get; private set; }
    public long EncounterId { get; private set; }
    public int RevisionNumber { get; private set; }
    public EncounterRevisionKind Kind { get; private set; }
    public long? ParentRevisionId { get; private set; }
    public long AuthorActorId { get; private set; }
    public string SpecialtyCode { get; private set; } = string.Empty;
    public string TemplateKey { get; private set; } = string.Empty;
    public string TemplateVersion { get; private set; } = string.Empty;
    public string ChiefComplaint { get; private set; } = string.Empty;
    public string? History { get; private set; }
    public string? Examination { get; private set; }
    public string? Assessment { get; private set; }
    public string? Plan { get; private set; }
    public string? Instructions { get; private set; }
    public string? BodySite { get; private set; }
    public string? LateralityCode { get; private set; }
    public string ContentHash { get; private set; } = string.Empty;
    public string? AmendmentReason { get; private set; }
    public DateTimeOffset? SignedUtc { get; private set; }

    public EncounterContent Content => EncounterContent.Create(SpecialtyCode, TemplateKey, TemplateVersion,
        ChiefComplaint, History, Examination, Assessment, Plan, Instructions, BodySite, LateralityCode);

    internal static EncounterRevision Create(ClinicalEncounter encounter, int revisionNumber,
        EncounterRevisionKind kind, long? parentRevisionId, EncounterContent content, long actorId,
        string? amendmentReason, DateTimeOffset now)
    {
        if (revisionNumber <= 0 || actorId <= 0)
            throw new DomainRuleException("A valid revision number and author are required.");
        var revision = new EncounterRevision
        {
            TenantId = encounter.TenantId,
            BranchId = encounter.BranchId,
            EncounterId = encounter.Id,
            RevisionNumber = revisionNumber,
            Kind = kind,
            ParentRevisionId = parentRevisionId,
            AuthorActorId = actorId,
            SpecialtyCode = content.SpecialtyCode,
            TemplateKey = content.TemplateKey,
            TemplateVersion = content.TemplateVersion,
            ChiefComplaint = content.ChiefComplaint,
            History = content.History,
            Examination = content.Examination,
            Assessment = content.Assessment,
            Plan = content.Plan,
            Instructions = content.Instructions,
            BodySite = content.BodySite,
            LateralityCode = content.LateralityCode,
            ContentHash = content.Hash(),
            AmendmentReason = amendmentReason,
            SignedUtc = kind is EncounterRevisionKind.Signed or EncounterRevisionKind.Amendment ? now : null
        };
        revision.StampCreated(now);
        return revision;
    }
}
