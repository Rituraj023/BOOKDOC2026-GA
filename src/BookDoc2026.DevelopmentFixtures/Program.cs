using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using BookDoc2026.Application.Abstractions;
using BookDoc2026.Contracts.Security;
using BookDoc2026.Domain.Foundation;
using BookDoc2026.Domain.Identity;
using BookDoc2026.Domain.Patients;
using BookDoc2026.Domain.Catalog;
using BookDoc2026.Domain.Queues;
using BookDoc2026.Domain.Scheduling;
using BookDoc2026.Domain.Stakeholders;
using BookDoc2026.Domain.Workforce;
using BookDoc2026.Infrastructure;
using BookDoc2026.Infrastructure.Context;
using BookDoc2026.Infrastructure.Data;
using BookDoc2026.Messaging;
using BookDoc2026.Shared.Kernel.Security;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

const string confirmationText = "MANAGE_BOOKDOC_DEVELOPMENT_BROWSER_FIXTURES";
const string tenantSlug = "bookdoc-browser-fixture";
const string receptionEmail = "reception.browser@bookdoc.invalid";
const string technicianEmail = "technician.browser@bookdoc.invalid";
const string reviewerEmail = "radiology.reviewer.browser@bookdoc.invalid";
const string clinicianEmail = "clinician.browser@bookdoc.invalid";
const string unassignedClinicianEmail = "unassigned.clinician.browser@bookdoc.invalid";
const string receptionRole = "DevelopmentBrowserReception";
const string technicianRole = "DevelopmentBrowserTechnician";
const string reviewerRole = "DevelopmentBrowserRadiologyReviewer";
const string clinicianRole = "DevelopmentBrowserClinician";

var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
{
    Args = args,
    ContentRootPath = AppContext.BaseDirectory
});
builder.Services.AddSingleton<SystemExecutionContext>();
builder.Services.AddSingleton<ICurrentActor>(services => services.GetRequiredService<SystemExecutionContext>());
builder.Services.AddSingleton<ICorrelationContext>(services => services.GetRequiredService<SystemExecutionContext>());
builder.Services.AddBookDocInfrastructure(builder.Configuration);
builder.Services.AddBookDocMessaging();

using var host = builder.Build();
using var scope = host.Services.CreateScope();
var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
var environment = scope.ServiceProvider.GetRequiredService<IHostEnvironment>();
var action = configuration["Fixture:Action"]?.Trim().ToLowerInvariant();
var confirmation = configuration["Fixture:Confirmation"];
var password = configuration["Fixture:Password"];

if (!environment.IsDevelopment()) return Fail("Development fixtures require DOTNET_ENVIRONMENT=Development.");
if (!string.Equals(confirmation, confirmationText, StringComparison.Ordinal))
    return Fail($"Set Fixture:Confirmation to {confirmationText}.");
if (action is not ("create" or "remove")) return Fail("Fixture:Action must be create or remove.");
if (action == "create" && string.IsNullOrWhiteSpace(password)) return Fail("Fixture:Password is required for create.");

var connectionString = configuration.GetConnectionString("BookDoc")
    ?? throw new InvalidOperationException("ConnectionStrings:BookDoc is required.");
var databaseName = new SqlConnectionStringBuilder(connectionString).InitialCatalog;
if (!databaseName.EndsWith("_Dev", StringComparison.OrdinalIgnoreCase))
    return Fail("Fixture commands are restricted to a database whose name ends with _Dev.");

var db = scope.ServiceProvider.GetRequiredService<BookDocDbContext>();
if (!await db.Database.CanConnectAsync()) return Fail("The development database is not reachable.");
if ((await db.Database.GetPendingMigrationsAsync()).Any()) return Fail("Apply reviewed migrations before managing fixtures.");

var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
var roles = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();

return action == "create"
    ? await CreateAsync(db, users, roles, password!)
    : await RemoveAsync(db);

static async Task<int> CreateAsync(
    BookDocDbContext db,
    UserManager<ApplicationUser> users,
    RoleManager<ApplicationRole> roles,
    string password)
{
    if (await db.Tenants.IgnoreQueryFilters().AnyAsync(tenant => tenant.Slug == tenantSlug)
        || await users.FindByEmailAsync(receptionEmail) is not null
        || await users.FindByEmailAsync(technicianEmail) is not null
        || await users.FindByEmailAsync(reviewerEmail) is not null
        || await users.FindByEmailAsync(clinicianEmail) is not null
        || await users.FindByEmailAsync(unassignedClinicianEmail) is not null)
        return Fail("Browser fixtures already exist. Run the remove action before recreating them.");

    try
    {
        var strategy = db.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await db.Database.BeginTransactionAsync();
            var now = DateTimeOffset.UtcNow;
            var tenant = Tenant.Activate("BOOKDOC Browser Fixture Clinic", tenantSlug, now);
            var organization = Organization.Create(tenant.Id, "Browser Fixture Organization", now);
            var branch = Branch.Create(tenant.Id, organization.Id, "BROWSER", "Browser Fixture Delhi", now);
            var branchConfiguration = BranchConfiguration.CreateDefault(tenant.Id, branch.Id, branch.Code, now);
            var stakeholder = Stakeholder.CreatePerson(tenant.Id, "Aarav Browser Patient", now);
            var person = StakeholderPerson.Create(tenant.Id, stakeholder.Id, null, "Aarav", null, "Browser Patient",
                new DateOnly(1990, 1, 15), false, AdministrativeSex.Male, now);
            var contact = StakeholderContactPoint.Create(tenant.Id, stakeholder.Id, ContactPointType.Mobile,
                "+91 9876543210", true, now);
            var payloadHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes("BOOKDOC-BROWSER-FIXTURE-PATIENT")));
            var patient = Patient.Register(tenant.Id, branch.Id, Guid.NewGuid(), payloadHash, stakeholder.Id, null, now);
            var servicePoint = ImagingServicePoint.Create(tenant.Id, branch.Id, "XR-BROWSER",
                "Browser Test X-ray", ImagingModality.XRay, null, now);

            db.AddRange(tenant, organization, branch, branchConfiguration, stakeholder, person, contact, patient, servicePoint);
            await db.SaveChangesAsync();

            await EnsureRoleAsync(roles, receptionRole,
                FoundationPermissions.PatientsSearch,
                FoundationPermissions.QueuesCheckIn);
            await EnsureRoleAsync(roles, technicianRole,
                FoundationPermissions.InvestigationWorklistView,
                FoundationPermissions.RadiologyStudiesView,
                FoundationPermissions.RadiologyStudiesStart,
                FoundationPermissions.RadiologyAcquisitionsRecord,
                FoundationPermissions.QueuesCall,
                FoundationPermissions.QueuesProgress,
                FoundationPermissions.QueuesCancel,
                FoundationPermissions.QueuesDisplayView);
            await EnsureRoleAsync(roles, reviewerRole,
                FoundationPermissions.InvestigationWorklistView,
                FoundationPermissions.RadiologyStudiesView,
                FoundationPermissions.RadiologyStudiesQualityReview);
            await EnsureRoleAsync(roles, clinicianRole,
                FoundationPermissions.EncountersView,
                FoundationPermissions.EncounterDraftsManage,
                FoundationPermissions.EncountersSign,
                FoundationPermissions.CatalogView,
                FoundationPermissions.InvestigationsView,
                FoundationPermissions.InvestigationOrdersCreate,
                FoundationPermissions.InvestigationQueueHandoff,
                FoundationPermissions.QueuesCheckIn,
                FoundationPermissions.PhysiotherapyCarePlansView,
                FoundationPermissions.PhysiotherapyCarePlansManage,
                FoundationPermissions.PhysiotherapySessionsRecord,
                FoundationPermissions.PhysiotherapyOutcomesRecord);

            await CreateUserAsync(db, users, receptionEmail, "Browser Reception", receptionRole, password,
                tenant.Id, organization.Id, branch.Id, now);
            var technician = await CreateUserAsync(db, users, technicianEmail, "Browser Radiology Operator",
                technicianRole, password,
                tenant.Id, organization.Id, branch.Id, now);
            var reviewer = await CreateUserAsync(db, users, reviewerEmail, "Browser Quality Reviewer",
                reviewerRole, password, tenant.Id, organization.Id, branch.Id, now);
            var clinician = await CreateUserAsync(db, users, clinicianEmail, "Browser Physiotherapist",
                clinicianRole, password, tenant.Id, organization.Id, branch.Id, now);
            await CreateUserAsync(db, users, unassignedClinicianEmail, "Browser Unassigned Clinician",
                clinicianRole, password, tenant.Id, organization.Id, branch.Id, now);

            var clinicianStakeholder = Stakeholder.CreatePerson(tenant.Id, "Browser Physiotherapist", now);
            var clinicianPerson = StakeholderPerson.Create(tenant.Id, clinicianStakeholder.Id, null, "Browser",
                null, "Physiotherapist", new DateOnly(1988, 6, 10), false, AdministrativeSex.Female, now);
            var operatorStakeholder = Stakeholder.CreatePerson(tenant.Id, "Browser Radiology Operator", now);
            var operatorPerson = StakeholderPerson.Create(tenant.Id, operatorStakeholder.Id, null, "Browser",
                null, "Radiology Operator", new DateOnly(1989, 4, 12), false, AdministrativeSex.Female, now);
            var reviewerStakeholder = Stakeholder.CreatePerson(tenant.Id, "Browser Quality Reviewer", now);
            var reviewerPerson = StakeholderPerson.Create(tenant.Id, reviewerStakeholder.Id, null, "Browser",
                null, "Quality Reviewer", new DateOnly(1984, 9, 8), false, AdministrativeSex.Male, now);
            var service = ClinicalService.Create(tenant.Id, "PHY-BROWSER", "Browser Physiotherapy Assessment",
                "Synthetic browser acceptance service", 45, now);
            var investigationService = ClinicalService.Create(tenant.Id, "XR-KNEE-BROWSER", "Browser Knee X-ray",
                "Synthetic X-ray order service", 15, now);
            var resourceCategory = ResourceCategory.Create(tenant.Id, null, "PHY-PRACTITIONER",
                "Physiotherapy practitioners", ResourceKind.Practitioner, now);
            var imagingCategory = ResourceCategory.Create(tenant.Id, null, "XRAY",
                "X-ray imaging modality", ResourceKind.ImagingModality, now);
            var ctImagingCategory = ResourceCategory.Create(tenant.Id, null, "CT",
                "CT imaging modality", ResourceKind.ImagingModality, now);
            db.AddRange(clinicianStakeholder, clinicianPerson, operatorStakeholder, operatorPerson,
                reviewerStakeholder, reviewerPerson, service, investigationService, resourceCategory,
                imagingCategory, ctImagingCategory);
            await db.SaveChangesAsync();

            var investigationRequirement = ServiceResourceRequirement.Create(tenant.Id,
                investigationService.Id, imagingCategory.Id, "PRIMARY-MODALITY", 1, false, now);
            db.Add(investigationRequirement);
            await db.SaveChangesAsync();

            var resource = BookableResource.Create(tenant.Id, branch.Id, resourceCategory.Id, "PHY-BROWSER-01",
                "Browser Physiotherapist Resource", CapacityMode.Exclusive, 1, "Asia/Kolkata", null, now);
            var xrayEquipment = BookableResource.Create(tenant.Id, branch.Id, imagingCategory.Id,
                "XR-BROWSER-01", "Browser X-ray Machine", CapacityMode.Exclusive, 1, "Asia/Kolkata", null, now);
            var ctEquipment = BookableResource.Create(tenant.Id, branch.Id, ctImagingCategory.Id,
                "CT-BROWSER-01", "Browser CT Machine", CapacityMode.Exclusive, 1, "Asia/Kolkata", null, now);
            var practitioner = PractitionerProfile.Create(tenant.Id, clinicianStakeholder.Id, clinician.Id,
                "PHY-BROWSER-01", "PHYSIOTHERAPIST", now);
            var operatorPractitioner = PractitionerProfile.Create(tenant.Id, operatorStakeholder.Id,
                technician.Id, "XR-OPERATOR-01", "RADIOLOGY_TECHNICIAN", now);
            var reviewerPractitioner = PractitionerProfile.Create(tenant.Id, reviewerStakeholder.Id,
                reviewer.Id, "XR-REVIEWER-01", "RADIOLOGY_REVIEWER", now);
            db.AddRange(resource, xrayEquipment, ctEquipment, practitioner, operatorPractitioner,
                reviewerPractitioner);
            await db.SaveChangesAsync();

            var credential = PractitionerCredential.Create(tenant.Id, practitioner.Id, "PHYSIOTHERAPY",
                "BROWSER-REGISTRATION-01", "Synthetic Browser Credential Authority",
                DateOnly.FromDateTime(now.UtcDateTime).AddYears(-1),
                DateOnly.FromDateTime(now.UtcDateTime).AddYears(1), now);
            credential.Verify(credential.Version, clinician.Id, now);
            practitioner.Activate(practitioner.Version, credential.IsCurrent(DateOnly.FromDateTime(now.UtcDateTime)), now);
            var assignment = PractitionerAssignment.Create(tenant.Id, practitioner.Id, branch.Id, service.Id,
                resource.Id, "PRIMARY", DateOnly.FromDateTime(now.UtcDateTime).AddYears(-1), null, now);
            var operatorCredential = PractitionerCredential.Create(tenant.Id, operatorPractitioner.Id,
                "XRAY_TECH", "BROWSER-XR-OPERATOR-01", "Synthetic Browser Credential Authority",
                DateOnly.FromDateTime(now.UtcDateTime).AddYears(-1),
                DateOnly.FromDateTime(now.UtcDateTime).AddYears(1), now);
            operatorCredential.Verify(operatorCredential.Version, clinician.Id, now);
            operatorPractitioner.Activate(operatorPractitioner.Version,
                operatorCredential.IsCurrent(DateOnly.FromDateTime(now.UtcDateTime)), now);
            var operatorAssignment = PractitionerAssignment.Create(tenant.Id, operatorPractitioner.Id,
                branch.Id, investigationService.Id, null, "PERFORMING",
                DateOnly.FromDateTime(now.UtcDateTime).AddYears(-1), null, now);
            var reviewerCredential = PractitionerCredential.Create(tenant.Id, reviewerPractitioner.Id,
                "XRAY_QA", "BROWSER-XR-REVIEWER-01", "Synthetic Browser Credential Authority",
                DateOnly.FromDateTime(now.UtcDateTime).AddYears(-1),
                DateOnly.FromDateTime(now.UtcDateTime).AddYears(1), now);
            reviewerCredential.Verify(reviewerCredential.Version, clinician.Id, now);
            reviewerPractitioner.Activate(reviewerPractitioner.Version,
                reviewerCredential.IsCurrent(DateOnly.FromDateTime(now.UtcDateTime)), now);
            var reviewerAssignment = PractitionerAssignment.Create(tenant.Id, reviewerPractitioner.Id,
                branch.Id, investigationService.Id, null, "QUALITY_REVIEW",
                DateOnly.FromDateTime(now.UtcDateTime).AddYears(-1), null, now);
            var xrayCapability = ResourceCapability.Create(tenant.Id, xrayEquipment.Id,
                investigationService.Id, null, 1, now);
            var ctCapability = ResourceCapability.Create(tenant.Id, ctEquipment.Id,
                investigationService.Id, null, 1, now);
            db.AddRange(credential, assignment, operatorCredential, operatorAssignment, reviewerCredential,
                reviewerAssignment, xrayCapability, ctCapability);
            await db.SaveChangesAsync();

            var india = TimeZoneInfo.FindSystemTimeZoneById("Asia/Kolkata");
            var localBookingDate = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(now, india).DateTime).AddDays(1);
            var localStart = localBookingDate.ToDateTime(new TimeOnly(10, 0), DateTimeKind.Unspecified);
            var startUtc = new DateTimeOffset(TimeZoneInfo.ConvertTimeToUtc(localStart, india), TimeSpan.Zero);
            var endUtc = startUtc.AddMinutes(service.DefaultDurationMinutes);
            var bookingHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(
                "BOOKDOC-BROWSER-FIXTURE-CLINICAL-BOOKING")));
            var hold = SchedulingHold.Create(tenant.Id, branch.Id, patient.Id, service.Id, Guid.NewGuid(),
                bookingHash, startUtc, endUtc, now.AddMinutes(20), now);
            db.SchedulingHolds.Add(hold);
            await db.SaveChangesAsync();
            var reservation = ResourceReservation.Create(tenant.Id, branch.Id, hold.Id, resource.Id,
                startUtc, endUtc, 1, now, "PRIMARY");
            db.ResourceReservations.Add(reservation);
            await db.SaveChangesAsync();
            var booking = Booking.Confirm(hold, now);
            hold.Confirm(hold.Version, now);
            db.Bookings.Add(booking);
            await db.SaveChangesAsync();
            db.BookingResourceAllocations.Add(BookingResourceAllocation.FromReservation(booking, reservation, now));
            await db.SaveChangesAsync();

            await transaction.CommitAsync();
            Console.WriteLine("Development browser fixtures created.");
            Console.WriteLine($"Reception user: {receptionEmail}");
            Console.WriteLine($"Technician user: {technicianEmail}");
            Console.WriteLine($"Independent quality-reviewer user: {reviewerEmail}");
            Console.WriteLine($"Assigned clinician user: {clinicianEmail}");
            Console.WriteLine($"Unassigned clinician user: {unassignedClinicianEmail}");
            Console.WriteLine($"Synthetic clinical agenda date: {localBookingDate:yyyy-MM-dd}");
            Console.WriteLine("Synthetic investigation destination: XR-BROWSER / Browser Test X-ray");
            Console.WriteLine("Synthetic patient search: Aarav Browser Patient or 9876");
            Console.WriteLine("Run the explicit remove action after browser validation.");
            return 0;
        });
    }
    catch (Exception exception)
    {
        return Fail($"Fixture creation failed: {exception.Message}");
    }
}

static async Task<int> RemoveAsync(BookDocDbContext db)
{
    try
    {
        var strategy = db.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await db.Database.BeginTransactionAsync();
            var tenant = await db.Tenants.IgnoreQueryFilters().SingleOrDefaultAsync(item => item.Slug == tenantSlug);
            if (tenant is null)
            {
                Console.WriteLine("Development browser fixture tenant is absent; no tenant data was removed.");
            }
            else
            {
                var tenantId = tenant.Id;
                await db.RadiologyQualityReviews.IgnoreQueryFilters().Where(item => item.TenantId == tenantId).ExecuteDeleteAsync();
                await db.RadiologyStudyEvents.IgnoreQueryFilters().Where(item => item.TenantId == tenantId).ExecuteDeleteAsync();
                await db.RadiologyAcquisitionAttempts.IgnoreQueryFilters().Where(item => item.TenantId == tenantId).ExecuteDeleteAsync();
                await db.RadiologyStudies.IgnoreQueryFilters().Where(item => item.TenantId == tenantId).ExecuteDeleteAsync();
                await db.InvestigationOrderEvents.IgnoreQueryFilters().Where(item => item.TenantId == tenantId).ExecuteDeleteAsync();
                await db.PhysiotherapyOutcomeObservations.IgnoreQueryFilters().Where(item => item.TenantId == tenantId).ExecuteDeleteAsync();
                await db.PhysiotherapyTreatmentSessions.IgnoreQueryFilters().Where(item => item.TenantId == tenantId).ExecuteDeleteAsync();
                await db.PhysiotherapyCarePlanRevisions.IgnoreQueryFilters().Where(item => item.TenantId == tenantId).ExecuteDeleteAsync();
                await db.PhysiotherapyCarePlans.IgnoreQueryFilters().Where(item => item.TenantId == tenantId).ExecuteDeleteAsync();
                await db.QueueTicketEvents.IgnoreQueryFilters().Where(item => item.TenantId == tenantId).ExecuteDeleteAsync();
                await db.QueueTickets.IgnoreQueryFilters().Where(item => item.TenantId == tenantId).ExecuteDeleteAsync();
                await db.InvestigationOrders.IgnoreQueryFilters().Where(item => item.TenantId == tenantId).ExecuteDeleteAsync();
                await db.EncounterRevisions.IgnoreQueryFilters().Where(item => item.TenantId == tenantId).ExecuteDeleteAsync();
                await db.ClinicalEncounters.IgnoreQueryFilters().Where(item => item.TenantId == tenantId).ExecuteDeleteAsync();
                await db.ImagingServicePoints.IgnoreQueryFilters().Where(item => item.TenantId == tenantId).ExecuteDeleteAsync();
                await db.AuditEvents.IgnoreQueryFilters().Where(item => item.TenantId == tenantId).ExecuteDeleteAsync();
                await db.BookingResourceAllocations.IgnoreQueryFilters().Where(item => item.TenantId == tenantId).ExecuteDeleteAsync();
                await db.Bookings.IgnoreQueryFilters().Where(item => item.TenantId == tenantId).ExecuteDeleteAsync();
                await db.ResourceReservations.IgnoreQueryFilters().Where(item => item.TenantId == tenantId).ExecuteDeleteAsync();
                await db.SchedulingHolds.IgnoreQueryFilters().Where(item => item.TenantId == tenantId).ExecuteDeleteAsync();
                await db.PractitionerAssignments.IgnoreQueryFilters().Where(item => item.TenantId == tenantId).ExecuteDeleteAsync();
                await db.PractitionerCredentials.IgnoreQueryFilters().Where(item => item.TenantId == tenantId).ExecuteDeleteAsync();
                await db.PractitionerProfiles.IgnoreQueryFilters().Where(item => item.TenantId == tenantId).ExecuteDeleteAsync();
                await db.ResourceStatusEvents.IgnoreQueryFilters().Where(item => item.TenantId == tenantId).ExecuteDeleteAsync();
                await db.ResourceCapabilities.IgnoreQueryFilters().Where(item => item.TenantId == tenantId).ExecuteDeleteAsync();
                await db.ServiceResourceRequirements.IgnoreQueryFilters().Where(item => item.TenantId == tenantId).ExecuteDeleteAsync();
                await db.BookableResources.IgnoreQueryFilters().Where(item => item.TenantId == tenantId).ExecuteDeleteAsync();
                await db.ResourceCategories.IgnoreQueryFilters().Where(item => item.TenantId == tenantId).ExecuteDeleteAsync();
                await db.ClinicalServices.IgnoreQueryFilters().Where(item => item.TenantId == tenantId).ExecuteDeleteAsync();
                await db.Patients.IgnoreQueryFilters().Where(item => item.TenantId == tenantId).ExecuteDeleteAsync();
                await db.StakeholderContactPoints.IgnoreQueryFilters().Where(item => item.TenantId == tenantId).ExecuteDeleteAsync();
                await db.StakeholderPersons.IgnoreQueryFilters().Where(item => item.TenantId == tenantId).ExecuteDeleteAsync();
                await db.Stakeholders.IgnoreQueryFilters().Where(item => item.TenantId == tenantId).ExecuteDeleteAsync();
                await db.UserScopes.IgnoreQueryFilters().Where(item => item.TenantId == tenantId).ExecuteDeleteAsync();
                await db.BranchConfigurations.IgnoreQueryFilters().Where(item => item.TenantId == tenantId).ExecuteDeleteAsync();
                await db.Branches.IgnoreQueryFilters().Where(item => item.TenantId == tenantId).ExecuteDeleteAsync();
                await db.Organizations.IgnoreQueryFilters().Where(item => item.TenantId == tenantId).ExecuteDeleteAsync();
                await db.Tenants.IgnoreQueryFilters().Where(item => item.Id == tenantId).ExecuteDeleteAsync();
                Console.WriteLine("Development browser fixture tenant data removed.");
            }

            var normalizedEmails = new[] { receptionEmail.ToUpperInvariant(), technicianEmail.ToUpperInvariant(),
                reviewerEmail.ToUpperInvariant(),
                clinicianEmail.ToUpperInvariant(), unassignedClinicianEmail.ToUpperInvariant() };
            var userIds = await db.Users
                .Where(user => user.NormalizedEmail != null && normalizedEmails.Contains(user.NormalizedEmail))
                .Select(user => user.Id)
                .ToListAsync();
            await db.Set<ApplicationUserClaim>().Where(item => userIds.Contains(item.UserId)).ExecuteDeleteAsync();
            await db.Set<ApplicationUserLogin>().Where(item => userIds.Contains(item.UserId)).ExecuteDeleteAsync();
            await db.Set<ApplicationUserToken>().Where(item => userIds.Contains(item.UserId)).ExecuteDeleteAsync();
            await db.Set<ApplicationUserRole>().Where(item => userIds.Contains(item.UserId)).ExecuteDeleteAsync();
            await db.Users.Where(user => userIds.Contains(user.Id)).ExecuteDeleteAsync();

            var normalizedRoles = new[] { receptionRole.ToUpperInvariant(), technicianRole.ToUpperInvariant(),
                reviewerRole.ToUpperInvariant(),
                clinicianRole.ToUpperInvariant() };
            var roleIds = await db.Roles
                .Where(role => role.NormalizedName != null && normalizedRoles.Contains(role.NormalizedName))
                .Select(role => role.Id)
                .ToListAsync();
            await db.Set<ApplicationRoleClaim>().Where(item => roleIds.Contains(item.RoleId)).ExecuteDeleteAsync();
            await db.Roles.Where(role => roleIds.Contains(role.Id)).ExecuteDeleteAsync();
            await transaction.CommitAsync();
            Console.WriteLine("Development browser fixture users and roles removed.");
            return 0;
        });
    }
    catch (Exception exception)
    {
        return Fail($"Fixture removal failed: {exception.Message}");
    }
}

static async Task EnsureRoleAsync(RoleManager<ApplicationRole> roles, string name, params string[] permissions)
{
    var role = new ApplicationRole { Name = name, IsActive = true, IsPlatformRole = false };
    Ensure(await roles.CreateAsync(role));
    foreach (var permission in permissions)
        Ensure(await roles.AddClaimAsync(role, new Claim(BookDocClaimTypes.Permission, permission)));
}

static async Task<ApplicationUser> CreateUserAsync(
    BookDocDbContext db,
    UserManager<ApplicationUser> users,
    string email,
    string displayName,
    string role,
    string password,
    long tenantId,
    long organizationId,
    long branchId,
    DateTimeOffset now)
{
    var user = new ApplicationUser
    {
        UserName = email,
        Email = email,
        DisplayName = displayName,
        EmailConfirmed = true,
        IsActive = true,
        CreatedUtc = now,
        ModifiedUtc = now
    };
    Ensure(await users.CreateAsync(user, password));
    Ensure(await users.AddToRoleAsync(user, role));
    db.UserScopes.Add(ApplicationUserScope.Create(user.Id, tenantId, organizationId, branchId, true, now));
    await db.SaveChangesAsync();
    return user;
}

static void Ensure(IdentityResult result)
{
    if (!result.Succeeded) throw new InvalidOperationException(string.Join(" ", result.Errors.Select(error => error.Description)));
}

static int Fail(string message)
{
    Console.Error.WriteLine(message);
    return 1;
}
