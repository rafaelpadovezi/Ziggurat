using System;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;

namespace DotNetCore.CAP.Contrib.Idempotency.Pipeline
{
    public class ConsumerServicePipeline<TMessage> : IPipeline<TMessage>
     where TMessage : IMessage
    {
        private readonly IConsumerService<TMessage> _service;

        public ConsumerServicePipeline(IConsumerService<TMessage> service) => _service = service;
        public async Task<Result<TMessage>> ExecuteAsync(TMessage message)
        {
            try
            {
                await _service.ProcessMessageAsync(message);
                return Result.Success(message);
            }
            catch (Exception ex)
            {
                return Result.Failure<TMessage>(ex.Message);
            }
        }

    }
}