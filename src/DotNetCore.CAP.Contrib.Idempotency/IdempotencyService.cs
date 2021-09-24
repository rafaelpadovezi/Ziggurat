using DotNetCore.CAP.Contrib.Idempotency.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Linq;
using System.Threading.Tasks;
using FluentValidation;
using System.Collections.Generic;

namespace DotNetCore.CAP.Contrib.Idempotency
{
    public class IdempotencyService<TMessage, TContext> : IConsumerService<TMessage>
        where TContext : DbContext
        where TMessage : IMessage
    {
        private readonly DbSet<MessageTracking> _messages;
        private readonly IConsumerService<TMessage> _service;
        private readonly IStorageHelper _storageHelper;
        private readonly ILogger<IdempotencyService<TMessage, TContext>> _logger;
        private readonly IValidator<TMessage> _validator;

        public IdempotencyService(
            TContext context,
            IConsumerService<TMessage> service,
            IStorageHelper storageHelper,
            ILogger<IdempotencyService<TMessage, TContext>> logger,
            IValidator<TMessage> validator)
        {
            _messages = context.Set<MessageTracking>();
            _service = service;
            _storageHelper = storageHelper;
            _logger = logger;
            _validator = validator;
        }

        public async Task ProcessMessageAsync(TMessage message)
        {
            var messageExists = await TrackMessageAsync(message);
            var messageIsValid = ValidateMessage(message);
            
            if (messageExists || messageIsValid is false)
                return;

            try
            {
                await _service.ProcessMessageAsync(message);
            }
            catch (DbUpdateException ex) when (_storageHelper.IsMessageExistsError(ex))
            {
                // If is unique constraint error it means that the message
                // was already processed and should do nothing
                LogMessageExists(message);
            }
        }

        private void LogMessageExists(TMessage message) =>
            _logger.LogInformation(
                "Message was processed already. Ignoring {MessageId}:{Type}.", message.MessageId, message.MessageGroup);

        private async Task<bool> TrackMessageAsync(TMessage message)
        {
            var messageExists = await _messages
                .Where(x => x.Id == message.MessageId)
                .Where(x => x.Type == message.MessageGroup)
                .AnyAsync();

            if (messageExists)
            {
                LogMessageExists(message);
                return true;
            }

            _messages.Add(new MessageTracking(message.MessageId, message.MessageGroup));
            return false;
        }

        private bool ValidateMessage(TMessage message)
        {
            var result = _validator.Validate(message);

            if (result.IsValid is false)
            {
                var errorsMessageFromResult = result.Errors.Select(error => error.ErrorMessage);
                var errors = string.Join("\n", errorsMessageFromResult);
                _logger.LogError(errors);
            }

            return result.IsValid;
        }
    }
}