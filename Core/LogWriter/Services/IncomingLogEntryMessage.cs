namespace UBIS.Services.LogWriter.Services;

/// <summary>
/// Wire shape actually published by every service's RabbitMqLogWriterClient (AIM/MenuGenerator/
/// Email/Gateway each keep their own private copy of this same class - see e.g.
/// Core/AIM/Infrastructure/Logging/LogEntryMessage.cs). Deliberately separate from
/// Domain/LogEntry.cs (the EF entity, Id is a DB-assigned int IDENTITY): deserializing straight
/// into LogEntry used to fail on every real RabbitMQ message with a JsonException ("could not
/// convert String to Int32, Path: $.Id") because every publisher sends Id as a Guid, not an int -
/// found 2026-07-22 via regression testing (messages were endlessly requeuing, 87 stuck in the
/// queue, none ever reaching dbo.M_LogEntry). The incoming Id was never actually used anyway -
/// OnMessageReceivedAsync always resets Id to 0 before insert, since the DB assigns the real one -
/// so this type exists purely to deserialize successfully, not to preserve the publisher's Id.
/// </summary>
public class IncomingLogEntryMessage
{
    public Guid Id { get; set; }
    public string Level { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? Exception { get; set; }
    public string? Properties { get; set; }
    public DateTime CreatedAt { get; set; }
}
