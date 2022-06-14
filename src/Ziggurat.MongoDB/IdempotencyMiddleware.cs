// using Microsoft.Extensions.Logging;
// using MongoDB.Driver;
// using System.Threading.Tasks;
// using Ziggurat.Internal;
//
// namespace Ziggurat.MongoDB
// {
//     internal class IdempotencyMiddleware<TMessage> : IConsumerMiddleware<TMessage>
//         where TMessage : IMessage
//     {
//         private readonly ILogger<IdempotencyMiddleware<TMessage>> _logger;
//         private readonly IMongoClient _client;
//
//         public IdempotencyMiddleware(
//             IMongoClient client,
//             ILogger<IdempotencyMiddleware<TMessage>> logger)
//         {
//             _client = client;
//             _logger = logger;
//         }
//
//         public async Task OnExecutingAsync(TMessage message, ConsumerServiceDelegate<TMessage> next)
//         {
//             if (await HasProcessedAsync(message))
//             {
//                 _logger.LogMessageExists(message);
//                 return;
//             }
//
//             try
//             {
//                 await next(message);
//             }
//             catch (MongoWriteException ex) when(IsMessageExistsError(ex))
//             {
//                 // If is unique key error it means that the message
//                 // was already processed and should do nothing
//                 _logger.LogMessageExists(message);
//             }
//         }
//
//         private bool IsMessageExistsError(MongoWriteException ex)
//         {
//             if (ex.InnerException is not MongoBulkWriteException)
//                 return false;
//
//             return ex.WriteError.Category == ServerErrorCategory.DuplicateKey &&
//                    ex.Message.Contains(ZigguratMongoDbOptions.ProcessedCollection);
//         }
//
//         private async Task<bool> HasProcessedAsync(TMessage message)
//         {
//             var collection = _client
//                 .GetDatabase(ZigguratMongoDbOptions.MongoDatabaseName)
//                 .GetCollection<MessageTracking>(ZigguratMongoDbOptions.ProcessedCollection);
//             var builder = Builders<MessageTracking>.Filter;
//             var filter = builder.Eq(x => x.Id, MessageTracking.CreateId(message.MessageId, message.MessageGroup));
//                 
//             return await collection.CountDocumentsAsync(filter) > 0;
//         }
//     }
// }