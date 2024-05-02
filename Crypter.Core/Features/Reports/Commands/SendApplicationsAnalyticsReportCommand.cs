/*
 * Copyright (C) 2024 Crypter File Transfer
 *
 * This file is part of the Crypter file transfer project.
 *
 * Crypter is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Affero General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * The Crypter source code is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU Affero General Public License for more details.
 *
 * You should have received a copy of the GNU Affero General Public License
 * along with this program.  If not, see <http://www.gnu.org/licenses/>.
 *
 * You can be released from the requirements of the aforementioned license
 * by purchasing a commercial license. Buying such a license is mandatory
 * as soon as you develop commercial activities involving the Crypter source
 * code without disclosing the source code of your own applications.
 *
 * Contact the current copyright holder to discuss commercial license options.
 */

using System.Threading;
using System.Threading.Tasks;
using Crypter.Common.Primitives;
using Crypter.Core.Features.Reports.Models;
using Crypter.Core.Services.Email;
using Crypter.Core.Settings;
using Crypter.DataAccess;
using MediatR;
using Microsoft.Extensions.Options;

namespace Crypter.Core.Features.Reports.Commands;

internal sealed record SendApplicationsAnalyticsReportCommand(ApplicationAnalyticsReport Report)
    : IRequest<bool>;

internal sealed class SendApplicationsAnalyticsReportCommandHandler
    : IRequestHandler<SendApplicationsAnalyticsReportCommand, bool>
{
    private readonly AnalyticsSettings _analyticsSettings;
    private readonly IEmailService _emailService;

    public SendApplicationsAnalyticsReportCommandHandler(
        IOptions<AnalyticsSettings> analyticsSettings,
        IEmailService emailService)
    {
        _analyticsSettings = analyticsSettings.Value;
        _emailService = emailService;
    }
    
    public async Task<bool> Handle(SendApplicationsAnalyticsReportCommand request, CancellationToken cancellationToken)
    {
        if (!_analyticsSettings.EnableEmailedReports || string.IsNullOrEmpty(_analyticsSettings.ReportRecipientEmailAddress))
        {
            return true;
        }
        
        if (!EmailAddress.TryFrom(_analyticsSettings.ReportRecipientEmailAddress, out EmailAddress validEmailAddress))
        {
            return false;
        }
        
        return await _emailService.SendApplicationAnalyticsReportEmailAsync(
            validEmailAddress,
            request.Report);
    }
}
