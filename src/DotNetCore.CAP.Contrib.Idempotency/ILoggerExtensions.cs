using Microsoft.Extensions.Logging;

namespace DotNetCore.CAP.Contrib.Idempotency
{
    internal static class LoggerExtensions
    {
        public static void LogMessageExists<T, TMessage>(this ILogger<T> logger, TMessage message)
            where TMessage : IMessage
            => logger.LogInformation(
                "Message was processed already. Ignoring {MessageId}:{Type}.", message.MessageId, message.MessageGroup);
    }
}