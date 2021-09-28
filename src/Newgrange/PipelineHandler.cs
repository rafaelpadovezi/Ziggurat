using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Newgrange
{
    public class PipelineHandler<TMessage> : IConsumerService<TMessage>
        where TMessage : IMessage
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IConsumerService<TMessage> _service;

        public PipelineHandler(IServiceProvider serviceProvider, IConsumerService<TMessage> service)
        {
            _serviceProvider = serviceProvider;
            _service = service;
        }

        public async Task ProcessMessageAsync(TMessage message)
        {
            var middlewares = _serviceProvider.GetServices<IConsumerMiddleware<TMessage>>();

            var stack = new Stack<ConsumerServiceDelegate<TMessage>>();
            stack.Push(consumerMessage => _service.ProcessMessageAsync(consumerMessage));
            foreach (var middleware in middlewares.Reverse())
            {
                stack.Push(consumerMessage => middleware.OnExecutingAsync(consumerMessage, stack.Pop()));
            }

            await stack.Pop()(message);
        }
    }

    public delegate Task ConsumerServiceDelegate<TMessage>(TMessage message)
        where TMessage : IMessage;

    public interface IConsumerMiddleware<TMessage> where TMessage : IMessage
    {
        Task OnExecutingAsync(TMessage message, ConsumerServiceDelegate<TMessage> next);
    }
}