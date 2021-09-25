using DotNetCore.CAP.Contrib.Idempotency.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Linq;
using System.Threading.Tasks;
using FluentValidation;
using System.Collections.Generic;
using CSharpFunctionalExtensions;

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
        private const string DuplicatedErrorMessage = "Message was processed already.";


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

        public async Task ProcessMessageAsync(TMessage message) =>
            await TrackMessageAsync(message)
                .Check(ValidateMessage)
                .Check(CallConsumerService)
                .OnFailure(error => _logger.LogInformation(error + "Ignoring {MessageId}:{Type}.", message.MessageId, message.MessageGroup));

        private async Task<Result<TMessage>> TrackMessageAsync(TMessage message)
        {
            var existsAnyMessage = await _messages
                .Where(x => x.Id == message.MessageId)
                .Where(x => x.Type == message.MessageGroup)
                .AnyAsync();

            if (existsAnyMessage)
                return Result.Failure<TMessage>(DuplicatedErrorMessage);

            _messages.Add(new MessageTracking(message.MessageId, message.MessageGroup));
            return Result.Success(message);
        }

        private Result<TMessage> ValidateMessage(TMessage message)
        {
            var result = _validator.Validate(message);

            if (result.IsValid is false)
            {
                var errorsMessageFromResult = result.Errors.Select(error => error.ErrorMessage);
                var errors = string.Join("\n", errorsMessageFromResult);
                return Result.Failure<TMessage>($"The message not is valid, {errors}");
            }

            return Result.Success(message);
        }

        private async Task<Result> CallConsumerService(TMessage message)
        {
            try
            {
                await _service.ProcessMessageAsync(message);
                return Result.Success();
            }
            catch (DbUpdateException ex) when (_storageHelper.IsMessageExistsError(ex))
            {
                // If is unique constraint error it means that the message
                // was already processed and should do nothing
                return Result.Failure<TMessage>(DuplicatedErrorMessage);
            }
        }
    }
}