using System.Threading.Tasks;

namespace Ziggurat
{
    public interface IConsumerService<TMessage> where TMessage : IMessage
    {
        Task ProcessMessageAsync(TMessage message);
    }
}