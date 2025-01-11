using Crypter.Common.Client.Models;

namespace Crypter.Common.Client.Events;

public sealed class RecoveryKeyCreatedEventArgs
{
    public RecoveryKey RecoveryKey { get; }

    public RecoveryKeyCreatedEventArgs(RecoveryKey recoveryKey)
    {
        RecoveryKey = recoveryKey;
    }
}
