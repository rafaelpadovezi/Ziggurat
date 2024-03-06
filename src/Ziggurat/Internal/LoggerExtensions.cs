using Microsoft.Extensions.Logging;

namespace Ziggurat.Internal;

public static class LoggerExtensions
{
    internal static void LogMessageExists<T, TMessage>(this ILogger<T> logger, TMessage message)
        where TMessage : IMessage
    {
        logger.LogInformation(
            "Message was processed already. Ignoring {MessageId}:{Type}.", message.MessageId, message.MessageGroup);
    }
}