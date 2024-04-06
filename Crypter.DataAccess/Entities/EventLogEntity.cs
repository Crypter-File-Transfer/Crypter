using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Crypter.DataAccess.Entities;

public class EventLogEntity : IDisposable
{
    public Guid Id { get; set; }
    public EventLogType EventLogType { get; set; }
    public JsonDocument AdditionalData { get; set; }
    public DateTimeOffset Timestamp { get; set; }

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
    TransferDownloadSuccess,
    TransferDownloadFailure
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
