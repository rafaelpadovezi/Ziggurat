using System.Threading.Tasks;

namespace DotNetCore.CAP.Contrib.Idempotency
{
    public interface IConsumerService<TMessage> where TMessage : IMessage
    {
        Task ProcessMessageAsync(TMessage message);
    }
}