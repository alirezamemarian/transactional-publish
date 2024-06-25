using Newtonsoft.Json;

namespace TransactionalPublish.Services
{
    public class ActualMessagePublisher
    {
        public Task PublishAsync(object message, CancellationToken cancellationToken = default)
        {
            Console.WriteLine($"Publishing message: {JsonConvert.SerializeObject(message, Formatting.None)}");

            return Task.CompletedTask;
        }
    }
}
