using DotNetCore.CAP.Filter;
using DotNetCore.CAP.Messages;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Ziggurat.CapAdapter;

/// <summary>
/// CAP filter that sets the message with the message Id and message Group, which will be
/// used by Ziggurat pipeline.
/// </summary>
public class BootstrapFilter : SubscribeFilter
{
    public override Task OnSubscribeExecutingAsync(ExecutingContext context)
    {
        var message = context.Arguments
            .FirstOrDefault(x => x is IMessage);
        if (message is null)
            throw new InvalidOperationException("Message must be of type IMessage");
        ((IMessage)message).MessageId = context.DeliverMessage.GetId();
        ((IMessage)message).MessageGroup = context.DeliverMessage.GetGroup();
        return Task.CompletedTask;
    }
}