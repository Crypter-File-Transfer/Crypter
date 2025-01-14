using System;
using System.Threading;
using System.Threading.Tasks;
using Crypter.Common.Contracts.Features.UserConsents;
using Crypter.DataAccess;
using Crypter.DataAccess.Entities;
using MediatR;
using Unit = EasyMonads.Unit;

namespace Crypter.Core.Features.UserConsent.Commands;

public record SaveUserConsentCommand(Guid UserId, UserConsentType ConsentType)
    : IRequest<Unit>;

internal sealed class SaveUserConsentCommandHandler
    : IRequestHandler<SaveUserConsentCommand, Unit>
{
    private readonly DataContext _dataContext;

    public SaveUserConsentCommandHandler(DataContext dataContext)
    {
        _dataContext = dataContext;
    }
    
    public async Task<Unit> Handle(SaveUserConsentCommand request, CancellationToken _)
    {
        UserConsentEntity newConsent = new UserConsentEntity(request.UserId, request.ConsentType, true, DateTime.UtcNow);
        _dataContext.UserConsents.Add(newConsent);
        await _dataContext.SaveChangesAsync(CancellationToken.None);
        return Unit.Default;
    }
}
