namespace Faaz.Services.Identity.Domain;

// Fixed, well-known IDs for the 3 built-in system roles. Deliberately not regenerated on reseed —
// SuperAdmin's is looked up directly by RbacSeeder to attach permission claims, and pinning all
// three keeps every environment's "the Student role" etc. referring to the same row.
public static class SystemRoleIds
{
    public static readonly Guid SuperAdmin = Guid.Parse("b0044d0a-1f88-4957-953c-8b188a72aa02");
    public static readonly Guid Student    = Guid.Parse("c1155e1b-2f99-4a68-a64d-9c299b83bb03");
    public static readonly Guid Consultant = Guid.Parse("d2266f2c-3a00-4b79-b75e-ad3a0c94cc04");
}
