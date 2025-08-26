using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.RateLimiting;
using System.Threading.Tasks;

namespace KnxService
{

    public enum KnxOperationType
    {
        WriteGroupValue,
        ReadGroupValueAsync
    }
    public class KnxRateLimiterManager
    {
        private readonly ConcurrentDictionary<KnxOperationType, RateLimiter> _limiters;

        public KnxRateLimiterManager()
        {
            _limiters = new ConcurrentDictionary<KnxOperationType, RateLimiter>(new[]
            {
            new KeyValuePair<KnxOperationType, RateLimiter>(
                KnxOperationType.WriteGroupValue,
                new SlidingWindowRateLimiter(new SlidingWindowRateLimiterOptions
                {
                    PermitLimit = 2,
                    Window = TimeSpan.FromSeconds(2),
                    SegmentsPerWindow = 1,
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                    QueueLimit = 10
                })
            ),
            new KeyValuePair<KnxOperationType, RateLimiter>(
                KnxOperationType.ReadGroupValueAsync,
                new SlidingWindowRateLimiter(new SlidingWindowRateLimiterOptions
                {
                    PermitLimit = 10,
                    Window = TimeSpan.FromSeconds(2),
                    SegmentsPerWindow = 5,
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                    QueueLimit = 20
                })
            ),
        });
        }

        public async Task WaitAsync(KnxOperationType operationType, CancellationToken cancellationToken = default)
        {
            var limiter = _limiters[operationType];
            using var lease = await limiter.AcquireAsync(1, cancellationToken);

            if (!lease.IsAcquired)
            {
                throw new InvalidOperationException($"Rate limit exceeded for {operationType}.");
            }
        }

    }
}
