using DotNetCore.CAP.Filter;
using DotNetCore.CAP.Messages;
using System;
using System.Linq;

namespace Ziggurat.CapAdapter;

public class BootstrapFilter : SubscribeFilter
{
    public override void OnSubscribeExecuting(ExecutingContext context)
    {
        var message = context.Arguments
            .FirstOrDefault(x => x is IMessage);
        if (message is null)
            throw new InvalidOperationException("Message must be of type IMessage");
        ((IMessage)message).MessageId = context.DeliverMessage.GetId();
        ((IMessage)message).MessageGroup = context.DeliverMessage.GetGroup();
    }
}