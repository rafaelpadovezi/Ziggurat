using System.Linq;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;

namespace DotNetCore.CAP.Contrib.Idempotency.Pipeline
{
    public class IdempotencyPipeline<TMessage> : IPipeline<TMessage>
     where TMessage : IMessage
    {
        private readonly DbSet<MessageTracking> _messages;
        public IdempotencyPipeline(DbSet<MessageTracking> messages) => _messages = messages;
        public async Task<Result<TMessage>> ExecuteAsync(TMessage message)
        {
            var messageExists = await _messages
               .Where(x => x.Id == message.MessageId)
               .Where(x => x.Type == message.MessageGroup)
               .AnyAsync();

            if (messageExists)
                return Result.Failure<TMessage>("Message was processed already. Ignoring {MessageId}:{Type}.");

            _messages.Add(new MessageTracking(message.MessageId, message.MessageGroup));

            return Result.Success(message);
        }
    }
}