using EasySave.Core.Models;
using System.Collections.Concurrent;

namespace EasySave.Core.Services
{
    /// <summary>
    /// Thread-safe priority queue shared across all active backup jobs.
    /// Priority order: priority-light → non-priority-light → priority-heavy → non-priority-heavy.
    /// Heavy files are controlled by _heavyFileSemaphore in BackupService (one at a time).
    /// </summary>
    public class GlobalFileQueue
    {
        // Two separate queues: priority files first, then normal files.
        // Heavy files are mixed in but gated by the heavy semaphore in the consumer.
        private readonly ConcurrentQueue<FileJob> _priorityQueue = new();
        private readonly ConcurrentQueue<FileJob> _normalQueue = new();

        // Counts how many jobs have not yet finished feeding the queue
        private int _activeProducers;
        private readonly object _producerLock = new();

        /// <summary>Number of files currently waiting in the queue.</summary>
        public int PendingCount => _priorityQueue.Count + _normalQueue.Count;

        /// <summary>
        /// Registers a new job as an active producer (call before Enqueue).
        /// </summary>
        public void RegisterProducer()
        {
            lock (_producerLock) _activeProducers++;
        }

        /// <summary>
        /// Signals that a job has finished feeding all its files.
        /// </summary>
        public void ProducerDone()
        {
            lock (_producerLock) _activeProducers--;
        }

        /// <summary>
        /// Returns true when all producers are done AND both queues are empty.
        /// </summary>
        public bool IsCompleted =>
            _activeProducers == 0 && _priorityQueue.IsEmpty && _normalQueue.IsEmpty;

        /// <summary>
        /// Enqueues a file into the appropriate priority lane.
        /// </summary>
        public void Enqueue(FileJob file)
        {
            if (file.IsPriority)
                _priorityQueue.Enqueue(file);
            else
                _normalQueue.Enqueue(file);
        }

        /// <summary>
        /// Tries to dequeue; returns true if a file was retrieved regardless of lane.
        /// Priority files are always served before normal files.
        /// </summary>
        public bool TryDequeueAny(out FileJob file)
        {
            if (_priorityQueue.TryDequeue(out file)) return true;
            return _normalQueue.TryDequeue(out file);
        }
    }
}
