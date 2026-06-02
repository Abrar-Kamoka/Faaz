namespace Faaz.Services.Identity.WebHost.Features.AdminApplications.DTOs;

public sealed record PagedResult<T>(
    IReadOnlyList<T> Items,
    int TotalCount,
    int Page,
    int PageSize,
    int TotalPages);
