using System;
using System.Collections;
using System.Reactive.Linq;
using IBM.WMQ;
using ObservableMessaging.IbmMq.Core.Default;
using ObservableMessaging.IbmMq.Core.Interfaces;

namespace ObservableMessaging.IbmMq
{
    public class InboundMessageQueue : IObservable<IWMQMessage>
    {
        private readonly Hashtable _connectionProperties;
        private readonly string _qmgr;
        private readonly bool _useTransactions;

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
            IObserver<IWMQMessage> errorQueue = null,
            IObserver<Exception> exceptions = null)
        {
            _qmgr = qmgr;
            _useTransactions = useTransactions;
            Hashtable connectionProperties = CreateConnectionProperties(channel, host, port);
            _connectionProperties = connectionProperties;

            _underlying = new RawInboundMessageQueue(qmgr, qname, channel, host, port, correlationId, messageType, concurrentWorkers, useTransactions, numBackoutAttempts, autoStart, errorQueue, exceptions, NewMessageOptions, NewMessage, NewQueueManager);
        }

        private static Hashtable CreateConnectionProperties(string channel, string host, int? port)
        {
            Hashtable connectionProperties = new Hashtable();
            if (!string.IsNullOrEmpty(host))
                connectionProperties.Add(MQC.HOST_NAME_PROPERTY, host);

            if (!string.IsNullOrEmpty(channel))
                connectionProperties.Add(MQC.CHANNEL_PROPERTY, channel);

            if (port.HasValue)
                connectionProperties.Add(MQC.PORT_PROPERTY, port.Value);

            connectionProperties.Add(MQC.TRANSPORT_PROPERTY, MQC.TRANSPORT_MQSERIES_MANAGED);
            return connectionProperties;
        }

        public IDisposable Subscribe(IObserver<IWMQMessage> observer)
        {
            return _underlying.Subscribe(observer);
        }

        private IWMQMessage NewMessage()
        {
            MQMessage mqMessage = new MQMessage();
            return new WMQMessage(mqMessage);
        }

        private IWMQGetMessageOptions NewMessageOptions()
        {
            IWMQGetMessageOptions mqgmo = new WMQDefaultGetMessageOptions();
            mqgmo.Options = 0;
            if (_useTransactions)  {
                mqgmo.Options |= MQC.MQGMO_SYNCPOINT;
            }
            mqgmo.Options |= MQC.MQGMO_WAIT;
            mqgmo.Options |= MQC.MQGMO_FAIL_IF_QUIESCING;
            return mqgmo;
        }

        private IWMQQueueManager NewQueueManager()
        {
            return new WMQQueueManager(_qmgr, _connectionProperties);
        }
    }
}