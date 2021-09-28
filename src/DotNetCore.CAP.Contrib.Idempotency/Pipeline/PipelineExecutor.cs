using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace DotNetCore.CAP.Contrib.Idempotency.Pipeline
{
    public class PipelineExecutor<TMessage>
        where TMessage : IMessage
    {
        private readonly ILogger<PipelineExecutor<TMessage>> _logger;
        private readonly IEnumerable<IPipeline<TMessage>> _pipelines;

        public PipelineExecutor(
            IEnumerable<IPipeline<TMessage>> pipelines,
            ILogger<PipelineExecutor<TMessage>> logger)
        {
            _logger = logger;
            _pipelines = pipelines;
        }

        public async Task ExecuteAsync(TMessage message)
        {
            TMessage resultMessage = message;
            foreach (var pipeline in _pipelines)
            {
                var result = await pipeline.ExecuteAsync(resultMessage);
                if (result.IsFailure)
                {
                    _logger.LogError(result.Error);
                    return;
                }
                resultMessage = result.Value;
            }
            _logger.LogInformation("Success");

        }
    }
}