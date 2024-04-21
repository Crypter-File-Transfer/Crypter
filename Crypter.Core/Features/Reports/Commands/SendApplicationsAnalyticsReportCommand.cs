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

using System;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Crypter.Common.Primitives;
using Crypter.Core.Features.Reports.Models;
using Crypter.Core.Services.Email;
using Crypter.Core.Settings;
using Crypter.DataAccess;
using Crypter.DataAccess.Entities;
using Crypter.DataAccess.Entities.JsonTypes.EventLogAdditionalData;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Crypter.Core.Features.Reports.Commands;

public sealed record SendApplicationsAnalyticsReportCommand(int ReportPeriod)
    : IRequest<bool>;

internal sealed class SendApplicationsAnalyticsReportCommandHandler
    : IRequestHandler<SendApplicationsAnalyticsReportCommand, bool>
{
    private readonly AnalyticsSettings _analyticsSettings;
    private readonly DataContext _dataContext;
    private readonly IEmailService _emailService;

    public SendApplicationsAnalyticsReportCommandHandler(
        IOptions<AnalyticsSettings> analyticsSettings,
        DataContext dataContext,
        IEmailService emailService)
    {
        _analyticsSettings = analyticsSettings.Value;
        _dataContext = dataContext;
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
        
        DateTimeOffset now = DateTimeOffset.UtcNow;
        DateTimeOffset reportBegin = DateTimeOffset.Now.AddDays(-request.ReportPeriod);

        var rangeData = _dataContext.EventLogs
            .Where(eventLog => eventLog.Timestamp <= now && eventLog.Timestamp >= reportBegin);
        
        var reportData = await rangeData
            .GroupBy(eventLog => eventLog.EventLogType)
            .Select(groupedEvents => new
            {
                successfulUploads = groupedEvents
                    .Count(x => x.EventLogType == EventLogType.TransferUploadSuccess),
                successfulPreviews = groupedEvents
                    .Where(x => x.EventLogType == EventLogType.TransferPreviewSuccess)
                    .ToList(),
                successfulDownloads = groupedEvents
                    .Where(x => x.EventLogType == EventLogType.TransferDownloadSuccess)
                    .ToList(),
                successfulRegistrations = groupedEvents
                    .Count(x => x.EventLogType == EventLogType.UserRegistrationSuccess),
                successfulLogins = groupedEvents
                    .Where(x => x.EventLogType == EventLogType.UserLoginSuccess)
                    .ToList()
            })
            .FirstOrDefaultAsync(cancellationToken);

        TransferAnalytics transferAnalytics = new TransferAnalytics(
            Uploads: reportData?.successfulUploads ?? 0,
            UniquePreviews: reportData?.successfulPreviews
                .Select(x => x.AdditionalData.Deserialize<SuccessfulTransferPreviewAdditionalData>())
                .DistinctBy(x => x?.ItemId ?? Guid.Empty)
                .Count() ?? 0,
            UniqueDownloads: reportData?.successfulDownloads
                .Select(x => x.AdditionalData.Deserialize<SuccessfulTransferDownloadAdditionalData>())
                .DistinctBy(x => x?.ItemId ?? Guid.Empty)
                .Count() ?? 0
        );

        UserAnalytics userAnalytics = new UserAnalytics(
            UniqueLogins: reportData?.successfulLogins
                .Select(x => x.AdditionalData.Deserialize<SuccessfulUserLoginAdditionalData>())
                .DistinctBy(x => x?.UserId ?? Guid.Empty)
                .Count() ?? 0,
            Registrations: reportData?.successfulRegistrations ?? 0
        );

        ApplicationAnalyticsReport report = new ApplicationAnalyticsReport(
            Begin: reportBegin,
            End: now,
            transferAnalytics,
            userAnalytics);
        
        return await _emailService.SendApplicationAnalyticsReportEmailAsync(
            validEmailAddress,
            report);
    }
}
