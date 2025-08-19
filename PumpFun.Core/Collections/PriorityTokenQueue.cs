using System;
using System.Collections.Generic;
using System.Threading;

namespace PumpFun.Core.Collections
{
    public class PriorityTokenQueue
    {
        private readonly SortedDictionary<decimal, HashSet<string>> _queue = new();
        private readonly Dictionary<string, decimal> _tokenMarketCaps = new();
        private readonly ReaderWriterLockSlim _lock = new();

        public void Enqueue(string tokenAddress, decimal marketCap)
        {
            _lock.EnterWriteLock();
            try
            {
                // Remove old entry if exists
                if (_tokenMarketCaps.TryGetValue(tokenAddress, out var existingMarketCap))
                {
                    // Remove token from existing market cap set
                    if (_queue.TryGetValue(existingMarketCap, out var tokens))
                    {
                        tokens.Remove(tokenAddress);
                        if (tokens.Count == 0)
                        {
                            _queue.Remove(existingMarketCap);
                        }
                    }
                }

                // Add token to new market cap set
                if (!_queue.TryGetValue(marketCap, out var marketCapTokens))
                {
                    marketCapTokens = new HashSet<string>();
                    _queue.Add(marketCap, marketCapTokens);
                }
                marketCapTokens.Add(tokenAddress);

                // Update token market cap mapping
                _tokenMarketCaps[tokenAddress] = marketCap;
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        public bool TryDequeue(out string tokenAddress)
        {
            _lock.EnterWriteLock();
            try
            {
                if (_queue.Count == 0)
                {
                    tokenAddress = default!;
                    return false;
                }

                // Get the highest market cap
                var highestMarketCap = GetHighestMarketCap();
                var tokens = _queue[highestMarketCap];

                // Get a token from the set
                var enumerator = tokens.GetEnumerator();
                if (enumerator.MoveNext())
                {
                    tokenAddress = enumerator.Current;
                    tokens.Remove(tokenAddress);

                    if (tokens.Count == 0)
                    {
                        _queue.Remove(highestMarketCap);
                    }

                    // Remove from token market cap mapping
                    _tokenMarketCaps.Remove(tokenAddress);

                    return true;
                }
                else
                {
                    // Should not reach here
                    tokenAddress = default!;
                    return false;
                }
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        private decimal GetHighestMarketCap()
        {
            // SortedDictionary keys are sorted in ascending order
            // So we need to get the last key for the highest market cap
            var enumerator = _queue.Keys.GetEnumerator();
            decimal highestMarketCap = 0;
            while (enumerator.MoveNext())
            {
                highestMarketCap = enumerator.Current;
            }
            return highestMarketCap;
        }

        public bool IsEmpty
        {
            get
            {
                _lock.EnterReadLock();
                try
                {
                    return _queue.Count == 0;
                }
                finally
                {
                    _lock.ExitReadLock();
                }
            }
        }

        public int Count
        {
            get
            {
                _lock.EnterReadLock();
                try
                {
                    return _queue.Count;
                }
                finally
                {
                    _lock.ExitReadLock();
                }
            }
        }
    }
}