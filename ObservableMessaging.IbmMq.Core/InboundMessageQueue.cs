using System;
using System.Collections;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;
using IBM.WMQ;
using log4net;
using ObservableMessaging.Core.Interfaces;
using ObservableMessaging.IbmMq.Core.Interfaces;

namespace ObservableMessaging.IbmMq
{
    public class RawInboundMessageQueue : IObservable<MQMessage>, ICancellable, IDisposable
    {
        private static readonly ILog logger = LogManager.GetLogger(typeof(RawInboundMessageQueue));

        private readonly string _qmgr;
        private readonly string _qname;
        private readonly string _correlationId;
        private readonly int? _messageType;
        private readonly int _concurrentWorkers;
        private readonly bool _useTransactions;
        private readonly int _numBackoutAttempts;
        private readonly IObserver<MQMessage> _errorQueue;
        private readonly IObserver<Exception> _exceptions;
        private readonly Func<IWMQQueueManager> _mqQueueManagerFactory;


        private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
        private readonly MQGetMessageOptions _mqgmo = new MQGetMessageOptions();
        private readonly object _connectionLock = new object();
        private readonly Subject<MQMessage> _subject = new Subject<MQMessage>();
        private static volatile int _messageCount = 0;

        private Task[] _dequeueTasks;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="qmgr"></param>
        /// <param name="qname"></param>
        /// <param name="channel"></param>
        /// <param name="host"></param>
        /// <param name="port"></param>
        /// <param name="concurrentWorkers"></param>
        /// <param name="useTransactions"></param>
        /// <param name="autoStart"></param>
        public RawInboundMessageQueue(
            string qmgr,
            string qname,
            string channel = null,
            string host = null,
            int? port = null,
            string correlationId = null,
            int? messageType = null,
            int concurrentWorkers = 1, 
            bool useTransactions = true,
            int numBackoutAttempts = 1,
            bool autoStart = true,
            IObserver<MQMessage> errorQueue = null,
            IObserver<Exception> exceptions = null,
            Func<IWMQQueueManager> mqQueueManagerFactory = null) 
        {
            _qmgr = qmgr;
            _qname = qname;
            _concurrentWorkers = concurrentWorkers;
            _useTransactions = useTransactions;
            _numBackoutAttempts = numBackoutAttempts;
            _errorQueue = errorQueue;
            _correlationId = correlationId;
            _messageType = messageType;
            _exceptions = exceptions;

            _mqgmo.Options = 0;
            if (_useTransactions) {
                _mqgmo.Options |= MQC.MQGMO_SYNCPOINT;
            }
            _mqgmo.Options |= MQC.MQGMO_WAIT;
            _mqgmo.Options |= MQC.MQGMO_FAIL_IF_QUIESCING;
            _mqQueueManagerFactory = mqQueueManagerFactory;

            if (autoStart) {
                Start();
            }
        }

        public void Start() {
            if (_dequeueTasks == null) {
                _dequeueTasks = new Task[_concurrentWorkers];
                for (int i = 0; i < _concurrentWorkers; i++)
                    _dequeueTasks[i] = Task.Factory.StartNew(DequeueTask, i, _cancellationTokenSource.Token);
            }
        }

        private void DequeueTask(object i) {
            int threadNum = (int)i;
#if DEBUG
            logger.Info($" Dequeue thread {threadNum} running on thread ({Thread.CurrentThread.ManagedThreadId}: {Thread.CurrentThread.Name}) ");
#endif

            IWMQQueueManager queueManager = null;
            IWMQQueue queue = null; 

            while( ! _cancellationTokenSource.Token.IsCancellationRequested ) {

                if (queue == null || queueManager == null || !queueManager.IsConnected || !queue.IsOpen) {
                    logger.Debug($"DequeueTask {threadNum}: Initializing connection to queue {_qname}");
                    queueManager = _mqQueueManagerFactory();
                    queue = queueManager.AccessQueue(_qname, MQC.MQOO_INPUT_AS_Q_DEF); 
                }

                MQMessage message = new MQMessage();

                if (!string.IsNullOrEmpty(_correlationId))
                    message.CorrelationId = System.Text.Encoding.UTF8.GetBytes(_correlationId);

                if (_messageType.HasValue)
                    message.MessageType = _messageType.Value;

                bool commitOrBackoutRequired = false;
                try {  
                    try {
                        queue.Get(message, _mqgmo);
                        if (_useTransactions)
                            commitOrBackoutRequired = true;

                        int messageId = Interlocked.Increment(ref _messageCount);
                        logger.Debug($"DequeueTask {threadNum}: Received message {messageId}");

                        _subject.OnNext(message);
                        logger.Debug($"DequeueTask {threadNum}: Message {messageId} emitted successfully");

                        if (commitOrBackoutRequired) {
                            logger.Debug($"DequeueTask {threadNum}: Committing message {messageId}");
                            queueManager.Commit();
                        }
                    }
                    catch (MQException mqExc) {
                        if (mqExc.Reason == 2033) {
                            // message not available, ignore and micro-sleep 
                            Task.Delay(10);
                        } else throw;
                    }
                }
                catch (Exception exc) {

                    logger.Info($"DequeueTask {threadNum}: Exception occurred during transaction", exc);
                    try {

                        if (commitOrBackoutRequired) {
                            logger.Info($"DequeueTask {threadNum}: Exception occurred during transaction, message rollback count is {message.BackoutCount} ", exc);
                            if (message.BackoutCount < _numBackoutAttempts) {
                                logger.Info($"DequeueTask {threadNum}: Proceeding to rollback message after exception: {message.BackoutCount} / {_numBackoutAttempts}");
                                queueManager.Backout();
                                logger.Info($"DequeueTask {threadNum}: Message Rollback is complete");
                            }
                            else {
                                logger.Info($"DequeueTask {threadNum}: backout count exceeds threshold, committing message off queue...");
                                queueManager.Commit();

                                if (_errorQueue == null) {
                                    logger.Info($"DequeueTask {threadNum}: Error queue has not been enabled.");
                                } else {
                                    logger.Info($"DequeueTask {threadNum}: Proceed to emit message to error queue  {message.BackoutCount} / {_numBackoutAttempts}", exc);
                                    try {
                                        _errorQueue.OnNext(message);
                                    }
                                    catch (Exception exc2) {
                                        logger.Info($"DequeueTask {threadNum}: Exception occurred while emiting message to error queue", exc2);
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception exc2) {
                        logger.Info($"DequeueTask {threadNum}: Exception occurred during rollback", exc2);
                    } 
                    finally {
                      
                        if (_exceptions != null)
                            try {
                                _exceptions.OnNext(exc);
                            }
                            catch (Exception exc2) {
                                logger.Info($"DequeueTask {threadNum}: Exception occurred while emiting exception event to subject", exc2);
                            }
                    }
                }
            }
        }

        public IDisposable Subscribe(IObserver<MQMessage> observer)
        {
            return _subject.Subscribe(observer);
        }

        public void Cancel()
        {
            _cancellationTokenSource.Cancel();
            Task.WaitAll(_dequeueTasks);
            _dequeueTasks = null;
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects).
                    Cancel();
                    _cancellationTokenSource.Dispose();
                    _subject.Dispose();
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.

                disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~InboundMessageQueue() {
        //   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        //   Dispose(false);
        // }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // TODO: uncomment the following line if the finalizer is overridden above.
            // GC.SuppressFinalize(this);
        }
        #endregion

    }
}