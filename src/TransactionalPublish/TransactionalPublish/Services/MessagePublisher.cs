using System.Collections.Concurrent;
using System.Transactions;

namespace TransactionalPublish.Services
{
    public class MessagePublisher
    {
        private readonly ActualMessagePublisher _actualMessagePublisher;
        private readonly ConcurrentDictionary<string, List<object>> _buffers;

        public MessagePublisher(ActualMessagePublisher actualMessagePublisher)
        {
            _actualMessagePublisher = actualMessagePublisher;
            _buffers = new ConcurrentDictionary<string, List<object>>();
        }

        public async Task PublishAsync(object message, CancellationToken cancellationToken = default)
        {
            if (Transaction.Current is not null && Transaction.Current.TransactionInformation.Status == TransactionStatus.Active)
            {
                var buffer = GetCurrentTransactionBuffer(Transaction.Current!);
                buffer.Add(message);
            }
            else
            {
                await _actualMessagePublisher.PublishAsync(message, cancellationToken);
            }
        }

        private List<object> GetCurrentTransactionBuffer(Transaction transaction)
        {
            var transactionId = transaction.TransactionInformation.LocalIdentifier;

            return _buffers.GetOrAdd(transactionId, (key) =>
            {
                transaction.TransactionCompleted += TransactionCompleted;
                return new List<object>();
            });
        }

        private void TransactionCompleted(object? sender, TransactionEventArgs e)
        {
            var info = e.Transaction!.TransactionInformation;
            var transactionId = info.LocalIdentifier;

            if (_buffers.TryRemove(transactionId, out var buffer))
            {
                if (info.Status == TransactionStatus.Committed)
                {
                    foreach (var message in buffer)
                    {
                        _actualMessagePublisher.PublishAsync(message).Wait();
                    }
                }

                buffer.Clear();
            }
        }
    }
}
