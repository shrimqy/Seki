﻿using Microsoft.Extensions.Logging;
using Sefirah.App.RemoteStorage.Helpers;
using System.Collections.Concurrent;

namespace Sefirah.App.RemoteStorage.Worker;
public sealed class FileLocker(ILogger<FileLocker> logger) : IDisposable
{
    private readonly Dictionary<string, SemaphoreQueue> _lockers = [];

    public async Task<IDisposable> Lock(string relativePath)
    {
        await GetOrCreate(relativePath).WaitAsync();
        return new Disposable(() => Release(relativePath));
    }

    private SemaphoreQueue GetOrCreate(string relativePath)
    {
        lock (_lockers)
        {
            if (_lockers.TryGetValue(relativePath, out SemaphoreQueue? value))
            {
                return value;
            }
            var sempahore = new SemaphoreQueue();
            _lockers[relativePath] = sempahore;
            return sempahore;
        }
    }

    private void Release(string relativePath)
    {
        lock (_lockers)
        {
            if (!_lockers.TryGetValue(relativePath, out var semaphore))
            {
                logger.LogWarning("Could not find semaphore for {relativePath}", relativePath);
                return;
            }
            var freed = semaphore.Release();
            if (freed)
            {
                semaphore.Dispose();
                _lockers.Remove(relativePath);
            }
        }
    }

    public void Dispose()
    {
        foreach (var kvp in _lockers)
        {
            kvp.Value.Dispose();
        }
    }

    private class SemaphoreQueue : IDisposable
    {
        private ConcurrentQueue<TaskCompletionSource<bool>> _queue = new();
        private readonly SemaphoreSlim _semaphore = new(1);

        public Task WaitAsync()
        {
            var tcs = new TaskCompletionSource<bool>();
            _queue.Enqueue(tcs);
            _semaphore.WaitAsync()
                .ContinueWith(t =>
                {
                    // Since SemaphoreSlim.WaitAsync is not FIFO, this may not be the tcs that was enqueued
                    // Just confirming go-ahead on oldest tcs in the queue
                    if (_queue.TryDequeue(out var popped))
                    {
                        popped.SetResult(true);
                    }
                });
            return tcs.Task;
        }

        public bool Release()
        {
            var isEmpty = _queue.IsEmpty;
            _semaphore.Release();
            return isEmpty;
        }

        public void Dispose()
        {
            _semaphore.Dispose();
        }
    }
}
