using System.Threading.Tasks;
using CSharpFunctionalExtensions;

namespace DotNetCore.CAP.Contrib.Idempotency.Pipeline
{
    public interface IPipeline<TMessage> where TMessage : IMessage
    {
        Task<Result<TMessage>> ExecuteAsync(TMessage message);
    }
}