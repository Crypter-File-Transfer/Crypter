using System;
using System.Threading;
using System.Threading.Tasks;
using Crypter.DataAccess;
using Crypter.DataAccess.Entities;
using EasyMonads;

namespace Crypter.Core.Features.UserConsent.Commands;

public record SaveAcknowledgementOfRecoveryKeyRisksCommand(Guid UserId) : MediatR.IRequest<Unit>;

// ReSharper disable once ClassNeverInstantiated.Global
internal sealed class SaveAcknowledgementOfRecoveryKeyRisksCommandHandler
    : MediatR.IRequestHandler<SaveAcknowledgementOfRecoveryKeyRisksCommand, Unit>
{
    private readonly DataContext _dataContext;

    public SaveAcknowledgementOfRecoveryKeyRisksCommandHandler(DataContext dataContext)
    {
        _dataContext = dataContext;
    }
    
    public async Task<Unit> Handle(SaveAcknowledgementOfRecoveryKeyRisksCommand request, CancellationToken cancellationToken)
    {
        UserConsentEntity newConsent =
            new UserConsentEntity(request.UserId, ConsentType.RecoveryKeyRisks, true, DateTime.UtcNow);
        _dataContext.UserConsents.Add(newConsent);
        await _dataContext.SaveChangesAsync(CancellationToken.None);
        return Unit.Default;
    }
}
