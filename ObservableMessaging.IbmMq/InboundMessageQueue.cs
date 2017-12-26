using System;
using System.Collections;
using System.Reactive.Linq;
using IBM.WMQ;
using ObservableMessaging.IbmMq.Core.Interfaces;

namespace ObservableMessaging.IbmMq
{
    public class InboundMessageQueue : IObservable<MQMessage>
    {
        private readonly Hashtable _connectionProperties = new Hashtable();
        private readonly string _qmgr;
        private readonly string _qname;
        private readonly RawInboundMessageQueue _underlying;
        
        /// <param name="qmgr">Name of QueueManager</param>
        /// <param name="qname">Name of Queue</param>
        /// <param name="channel">Name of Channel</param>
        /// <param name="host">Hostname</param>
        /// <param name="port">Host port</param>
        /// <param name="concurrentWorkers"></param>
        /// <param name="useTransactions"></param>
        /// <param name="autoStart"></param>
        public InboundMessageQueue(
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
            IObserver<Exception> exceptions = null)
        {
            if (!string.IsNullOrEmpty(host))
                _connectionProperties.Add(MQC.HOST_NAME_PROPERTY, host);
            
            if (!string.IsNullOrEmpty(channel))
                _connectionProperties.Add(MQC.CHANNEL_PROPERTY, channel);

            if (port.HasValue)
                _connectionProperties.Add(MQC.PORT_PROPERTY, port.Value);
            
            _connectionProperties.Add(MQC.TRANSPORT_PROPERTY, MQC.TRANSPORT_MQSERIES_MANAGED);

            _underlying = new RawInboundMessageQueue(qmgr, qname, channel, host, port, correlationId, messageType, concurrentWorkers, useTransactions, numBackoutAttempts, autoStart, errorQueue, exceptions, NewQueueManager);
        }

        public IDisposable Subscribe(IObserver<MQMessage> observer)
        {
            return _underlying.Subscribe(observer);
        }

        private IWMQQueueManager NewQueueManager()
        {
            return new WMQQueueManager(_qmgr, _connectionProperties);
        }
    }
}