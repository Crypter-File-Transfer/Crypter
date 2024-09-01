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

using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Crypter.DataAccess.Entities;

public class EventLogEntity : IDisposable
{
    public Guid Id { get; init; }
    public EventLogType EventLogType { get; init; }
    public JsonDocument AdditionalData { get; init; }
    public DateTimeOffset Timestamp { get; init; }

    private EventLogEntity(EventLogType eventLogType, JsonDocument additionalData, DateTimeOffset timestamp)
    {
        EventLogType = eventLogType;
        AdditionalData = additionalData;
        Timestamp = timestamp;
    }
    
    public EventLogEntity(Guid id, EventLogType eventLogType, JsonDocument additionalData, DateTimeOffset timestamp)
    {
        Id = id;
        EventLogType = eventLogType;
        AdditionalData = additionalData;
        Timestamp = timestamp;
    }

    public static EventLogEntity Create<TAdditionalData>(EventLogType eventLogType, TAdditionalData additionalData, DateTimeOffset timestamp)
    {
        JsonDocument jsonData = JsonSerializer.SerializeToDocument(additionalData);
        return new EventLogEntity(eventLogType, jsonData, timestamp);
    }

    public void Dispose()
    {
        AdditionalData.Dispose();
        GC.SuppressFinalize(this);
    }
}

public enum EventLogType
{
    Unknown,
    UserRegistrationSuccess,
    UserRegistrationFailure,
    UserLoginSuccess,
    UserLoginFailure,
    TransferUploadSuccess,
    TransferUploadFailure,
    TransferPreviewSuccess,
    TransferPreviewFailure,
    TransferDownloadSuccess,
    TransferDownloadFailure,
    TransferMultipartInitializationSuccess,
    TransferMultipartInitializationFailure,
    TransferMultipartUploadSuccess,
    TransferMultipartUploadFailure,
    TransferMultipartUploadFinalizationSuccess,
    TransferMultipartUploadFinalizationFailure
}

public class EventLogEntityConfiguration : IEntityTypeConfiguration<EventLogEntity>
{
    public void Configure(EntityTypeBuilder<EventLogEntity> builder)
    {
        builder.ToTable("EventLog");

        builder.HasKey(x => x.Id);
        
        builder.HasIndex(x => x.EventLogType);
    }
}
