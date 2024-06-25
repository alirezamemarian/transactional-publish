using Microsoft.Extensions.DependencyInjection;
using System.Transactions;
using TransactionalPublish.Services;

namespace TransactionalPublish
{
    internal static class Program
    {
        static void Main(string[] args)
        {
            var services = new ServiceCollection()
                .AddScoped<MessagePublisher>()
                .AddScoped<ActualMessagePublisher>()
                .BuildServiceProvider();

            using var scope = services.CreateScope();
            var publisher = scope.ServiceProvider.GetRequiredService<MessagePublisher>();

            using(var transaction = new TransactionScope())
            {
                publisher.PublishAsync(new { Content = "Hello, World!" }).Wait();

                using (var innerTransaction = new TransactionScope(TransactionScopeOption.Required, TransactionScopeAsyncFlowOption.Enabled))
                {
                    publisher.PublishAsync(new { Content = "Hello, Inner World!" }).Wait();

                    innerTransaction.Complete();
                }

                transaction.Complete();
            }

            Console.ReadLine();
        }
    }
}
