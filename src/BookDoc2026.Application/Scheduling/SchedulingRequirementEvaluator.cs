using BookDoc2026.Domain.Catalog;
using BookDoc2026.Domain.Common;
using BookDoc2026.Domain.Scheduling;

namespace BookDoc2026.Application.Scheduling;

public static class SchedulingRequirementEvaluator
{
    public static void EnsureComplete(
        IReadOnlyCollection<ResourceReservation> reservations,
        IReadOnlyDictionary<long, BookableResource> resources,
        IReadOnlyCollection<ServiceResourceRequirement> requirements)
    {
        var requirementsByCategory = requirements
            .GroupBy(requirement => requirement.CategoryId)
            .ToDictionary(group => group.Key, group => group.ToArray());

        foreach (var reservation in reservations)
        {
            if (!resources.TryGetValue(reservation.ResourceId, out var resource))
                throw new DomainRuleException("A held resource is no longer available for confirmation.");
            if (reservation.RequirementRoleCode is not null
                && (!requirementsByCategory.TryGetValue(resource.CategoryId, out var categoryRequirements)
                    || !categoryRequirements.Any(requirement =>
                        string.Equals(requirement.RoleCode, reservation.RequirementRoleCode, StringComparison.Ordinal))))
                throw new DomainRuleException("A held resource uses an unknown service requirement role.");
        }

        foreach (var requirement in requirements.Where(requirement => !requirement.IsOptional))
        {
            var categoryRequirementCount = requirementsByCategory[requirement.CategoryId].Length;
            var supplied = reservations
                .Where(reservation =>
                {
                    var resource = resources[reservation.ResourceId];
                    if (resource.CategoryId != requirement.CategoryId) return false;
                    return string.Equals(
                               reservation.RequirementRoleCode,
                               requirement.RoleCode,
                               StringComparison.Ordinal)
                           || (reservation.RequirementRoleCode is null && categoryRequirementCount == 1);
                })
                .Sum(reservation => reservation.Quantity);
            if (supplied < requirement.Quantity)
                throw new DomainRuleException(
                    $"Required resource role {requirement.RoleCode} is not fully satisfied.");
        }
    }
}
