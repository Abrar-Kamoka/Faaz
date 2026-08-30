using Faaz.Services.Administration.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Faaz.Services.Administration.Infrastructure.DatabaseContext;

// Standardized consultancy-service taxonomy — replaces the two hardcoded enums this used to be
// split across (ConsultantEnums.ServiceType / StudentEnums.HelpType) with one admin-editable list,
// modeled on what UK university-admissions consultancies actually offer end to end.
public static class ServiceSeeder
{
    private static readonly (string Name, string Category, int SortOrder)[] Defaults =
    [
        ("University & Course Shortlisting",         "Application Support", 10),
        ("Personal Statement Writing/Review",        "Application Support", 20),
        ("Statement of Purpose (SOP) Writing/Review", "Application Support", 30),
        ("Letter of Recommendation Guidance",        "Application Support", 40),
        ("UCAS Application Support",                 "Application Support", 50),
        ("Interview Preparation",                    "Application Support", 60),
        ("CV/Resume Review",                         "Application Support", 70),
        ("Portfolio Review",                         "Application Support", 80),
        ("Research Proposal Writing",                "Application Support", 90),
        ("Scholarships & Funding Applications",       "Funding",             100),
        ("Student Visa & Immigration Guidance",       "Post-Offer",          110),
        ("Financial Planning & Student Finance",      "Funding",             120),
        ("Accommodation & Pre-Departure Support",     "Post-Offer",          130),
        ("Post-Offer & Enrolment Support",            "Post-Offer",          140),
        ("Career Guidance",                          "Guidance",            150),
        ("General Guidance",                         "Guidance",            160),
    ];

    public static async Task SeedAsync(IServiceProvider sp)
    {
        using var scope = sp.CreateScope();
        var db     = scope.ServiceProvider.GetRequiredService<AdminDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<AdminDbContext>>();

        foreach (var (name, category, sortOrder) in Defaults)
        {
            var exists = await db.Services.IgnoreQueryFilters()
                                 .AnyAsync(x => x.Name == name);
            if (!exists)
            {
                var max = await db.Services.IgnoreQueryFilters()
                                  .MaxAsync(x => (int?)x.SrNo);
                await db.Services.AddAsync(new Service
                {
                    SrNo      = (max ?? 0) + 1,
                    Name      = name,
                    Category  = category,
                    SortOrder = sortOrder,
                    IsActive  = true
                });
                logger.LogInformation("Service seed: {Name}", name);
            }
        }

        await db.SaveChangesAsync();
    }
}
