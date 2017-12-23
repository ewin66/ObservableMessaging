using System;
using System.Collections;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;
using IBM.WMQ;
using ObservableMessaging.Core.Interfaces;

namespace ObservableMessaging.IbmMq
{
    public class InboundMessageQueue : IObservable<MQMessage>, ICancellable, IDisposable
    {
        private readonly string _qmgr;
        private readonly string _qname;
        private readonly int _concurrentWorkers;
        private readonly bool _useTransactions;

        private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
        private readonly MQGetMessageOptions _mqgmo = new MQGetMessageOptions();
        private readonly object _connectionLock = new object();
        private readonly Subject<MQMessage> _subject = new Subject<MQMessage>();
        private readonly Hashtable _connectionProperties = new Hashtable();

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
        public InboundMessageQueue(
            string qmgr, 
            string qname, 
            string channel=null, 
            string host=null, 
            int? port=null, 
            int concurrentWorkers = 1, 
            bool useTransactions = true,
            bool autoStart = true) 
        {
            _qmgr = qmgr;
            _qname = qname;
            _concurrentWorkers = concurrentWorkers;
            _useTransactions = useTransactions;

            if (!string.IsNullOrEmpty(host))
                _connectionProperties.Add(MQC.HOST_NAME_PROPERTY, host);
            
            if (!string.IsNullOrEmpty(channel))
                _connectionProperties.Add(MQC.CHANNEL_PROPERTY, channel); //"DEV.APP.SVRCONN"

            if (port.HasValue)
                _connectionProperties.Add(MQC.PORT_PROPERTY, port.Value); // port number
            
            _connectionProperties.Add(MQC.TRANSPORT_PROPERTY, MQC.TRANSPORT_MQSERIES_MANAGED);
            _mqgmo.Options = 0;
            if (_useTransactions) {
                _mqgmo.Options |= MQC.MQGMO_SYNCPOINT;
            }
            _mqgmo.Options |= MQC.MQGMO_WAIT;
            _mqgmo.Options |= MQC.MQGMO_FAIL_IF_QUIESCING;

            if (autoStart) {
                Start();
            }
        }

        public void Start() {
            if (_dequeueTasks == null) {
                _dequeueTasks = new Task[_concurrentWorkers];
                for (int i = 0; i < _concurrentWorkers; i++)
                    _dequeueTasks[i] = Task.Factory.StartNew(DequeueTask);
            }
        }

        private void DequeueTask() {      
            MQQueueManager queueManager = null;
            MQQueue queue = null; 

            while( ! _cancellationTokenSource.Token.IsCancellationRequested ) {

                if (queue == null || queueManager == null || !queueManager.IsConnected || !queueManager.IsOpen || !queue.IsOpen) {
                    queueManager = new MQQueueManager(_qmgr, _connectionProperties);
                    queue = queueManager.AccessQueue(_qname, MQC.MQOO_INPUT_AS_Q_DEF); 
                }

                MQMessage message = new MQMessage();
                try {
                    queue.Get(message, _mqgmo);
                    _subject.OnNext(message);
                    queueManager.Commit();
                }
                catch (MQException mqExc) {
                    if (mqExc.Reason == 1234) {
                        // message not available, ignore and micro-sleep 
                        Task.Delay(10);
                    }
                    else
                    {
                        queueManager.Backout();
                        _subject.OnError(mqExc);
                    }
                }
                catch (Exception exc) {
                    queueManager.Backout();
                    _subject.OnError(exc);
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
