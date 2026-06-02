using Faaz.Services.Consultant.Infrastructure.Interfaces;
using Faaz.Services.Consultant.WebHost.Features.ConsultantApplication.DTOs;
using Faaz.SharedKernel.Exceptions;
using MediatR;

namespace Faaz.Services.Consultant.WebHost.Features.ConsultantApplication.Queries;

public class GetApplicationStatusQuery : IRequest<ApplicationStatusDto>
{
    public Guid UserId { get; set; }
}

internal sealed class GetApplicationStatusQueryHandler : IRequestHandler<GetApplicationStatusQuery, ApplicationStatusDto>
{
    private readonly IConsultantApplicationServices _appServices;

    public GetApplicationStatusQueryHandler(IConsultantApplicationServices appServices)
    {
        _appServices = appServices;
    }

    public async Task<ApplicationStatusDto> Handle(GetApplicationStatusQuery query, CancellationToken ct)
    {
        var app = await _appServices.GetByUserIdAsync(query.UserId, ct)
            ?? throw new NotFoundException("ConsultantApplication for user", query.UserId);

        return new ApplicationStatusDto
        {
            ApplicationId = app.Id,
            Status = app.ApplicationStatus.ToString(),
            AdminNotes = app.AdminNotes,
            SubmittedAt = app.SubmittedAt,
            UpdatedAt = app.UpdatedAt
        };
    }
}
