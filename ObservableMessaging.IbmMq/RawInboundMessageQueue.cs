﻿using System;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;
using IBM.WMQ;
using log4net;
using ObservableMessaging.Core.Interfaces;

namespace ObservableMessaging.IbmMq
{
    public class RawInboundMessageQueue : IObservable<MQMessage>, ICancellable, IDisposable
    {
        private static readonly ILog logger = LogManager.GetLogger(typeof(RawInboundMessageQueue));

        private readonly string _qmgr;
        private readonly string _qname;
        private readonly string _correlationId;
        private readonly int? _messageType;
        private readonly int _numConcurrentDequeueTasks;
        private readonly bool _useTransactions;
        private readonly int _numBackoutAttempts;

        private readonly IObserver<MQMessage> _errorQueue;
        private readonly IObserver<Exception> _exceptions;
        private readonly Func<MQQueueManager> _mqQueueManagerFactory;
        private readonly Func<MQMessage> _messageFactory;

        private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
        private readonly MQGetMessageOptions _mqgmo;
        private readonly object _connectionLock = new object();
        private readonly Subject<MQMessage> _subject = new Subject<MQMessage>();

        private Task[] _dequeueTasks;

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
            Func<MQGetMessageOptions> messageOptionsFactory = null,
            Func<MQMessage> messageFactory = null,
            Func<MQQueueManager> mqQueueManagerFactory = null) 
        {
            _qmgr = qmgr;
            _qname = qname;
            _numConcurrentDequeueTasks = concurrentWorkers;
            _useTransactions = useTransactions;
            _numBackoutAttempts = numBackoutAttempts;
            _errorQueue = errorQueue;
            _correlationId = correlationId;
            _messageType = messageType;
            _exceptions = exceptions;

            _messageFactory = messageFactory ?? (() => new MQMessage());
            _mqgmo = messageOptionsFactory()?? new MQGetMessageOptions();
            _mqQueueManagerFactory = mqQueueManagerFactory;

            if (autoStart) {
                Start();
            }
        }

        public void Start() {
            if (_dequeueTasks == null) {
                logger.Info($"Starting {_numConcurrentDequeueTasks} Dequeue tasks");
                _dequeueTasks = new Task[_numConcurrentDequeueTasks];
                for (int i = 0; i < _numConcurrentDequeueTasks; i++)
                    _dequeueTasks[i] = Task.Factory.StartNew(DequeueTask, i, _cancellationTokenSource.Token);
            } else
                logger.Warn($"Dequeue tasks have already started.");
        }

        private void DequeueTask(object i) {
            int threadNum = (int)i;
#if DEBUG
            logger.Info($" Dequeue thread {threadNum} running on thread ({Thread.CurrentThread.ManagedThreadId}: {Thread.CurrentThread.Name}) ");
#endif

            MQQueueManager queueManager = null;
            MQQueue queue = null;
            while (! _cancellationTokenSource.Token.IsCancellationRequested ) {

                if (queue == null || queueManager == null || !queueManager.IsConnected || !queue.IsOpen) {
                    logger.Debug($"DequeueTask {threadNum}: Initializing connection to queue {_qname}");
                    queueManager = _mqQueueManagerFactory();
                    queue = queueManager.AccessQueue(_qname, MQC.MQOO_INPUT_AS_Q_DEF); 
                }

                MQMessage message = _messageFactory();
                bool commitOrBackoutRequired = false;
                try {  
                    try {
                        queue.Get(message, _mqgmo);
                        if (_useTransactions)
                            commitOrBackoutRequired = true;

                        int messageIdLength = message.MessageId.Length;
                        string messageId = (messageIdLength > 8)? BitConverter.ToString(message.MessageId, messageIdLength - 8, 8) : BitConverter.ToString(message.MessageId, 0, messageIdLength);
                        logger.Debug($"DequeueTask {threadNum}: Received message {messageId}");

                        try {
                            _subject.OnNext(message);
                            logger.Debug($"DequeueTask {threadNum}: Message {messageId} emitted successfully");
                            if (commitOrBackoutRequired) {
                                logger.Debug($"DequeueTask {threadNum}: Committing message {messageId}");
                                queueManager.Commit();
                            }
                        }
                        catch (Exception subjectException)  {
                            logger.Error($"DequeueTask {threadNum}: Exception occurred within the subject, proceed to commit/rollback/errorqueue phase ", subjectException);
                            try {

                                if (commitOrBackoutRequired) {
                                    logger.Info($"DequeueTask {threadNum}: Exception occurred during transaction, message rollback count is {message.BackoutCount} ");
                                    if (message.BackoutCount < _numBackoutAttempts) {
                                        logger.Info($"DequeueTask {threadNum}: Proceeding to rollback message after exception: {message.BackoutCount} of {_numBackoutAttempts} attempts");
                                        queueManager.Backout();
                                        logger.Info($"DequeueTask {threadNum}: Message rollback is complete");
                                    } else {
                                        logger.Info($"DequeueTask {threadNum}: Message backout count {message.BackoutCount} equals or exceeds max threshold of {_numBackoutAttempts} attempt(s), committing message off queue...");
                                        queueManager.Commit();

                                        if (_errorQueue == null) {
                                            logger.Info($"DequeueTask {threadNum}: Error queue has not been enabled, message will be discarded.");
                                        } else  {
                                            logger.Info($"DequeueTask {threadNum}: Proceed to emit message to error queue  {message.BackoutCount} / {_numBackoutAttempts}");
                                            try {
                                                _errorQueue.OnNext(message);
                                            }
                                            catch (Exception exc2)  {
                                                logger.Info($"DequeueTask {threadNum}: Exception occurred while emiting message to error queue", exc2);
                                            }
                                        }
                                    }
                                }
                            }
                            catch (Exception exc2) {
                                logger.Info($"DequeueTask {threadNum}: Exception occurred during commit/rollback/errorqueue phase", exc2);
                            }
                            finally {

                                    try {
                                        _exceptions?.OnNext(subjectException);
                                    }
                                    catch (Exception exc2)  {
                                        logger.Info($"DequeueTask {threadNum}: Exception occurred while emiting exception event to subject", exc2);
                                    }
                            }
                        }

                    }
                    catch (MQException mqExc) {
                        if (mqExc.Reason == 2033) {
                            // message not available, ignore and micro-sleep 
                            Task.Delay(50);
                        } else throw;
                    }
                }
                catch (Exception exc) {
                    logger.Error($"DequeueTask {threadNum}: Exception occurred during dequeue task loop", exc);
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