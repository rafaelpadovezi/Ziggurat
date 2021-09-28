using System.Threading.Tasks;

namespace Newgrange
{
    public interface IConsumerService<TMessage> where TMessage : IMessage
    {
        Task ProcessMessageAsync(TMessage message);
    }
}