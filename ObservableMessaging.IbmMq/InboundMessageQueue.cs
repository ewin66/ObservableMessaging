using System;
using System.Collections;
using System.Reactive.Linq;
using IBM.WMQ;
using ObservableMessaging.IbmMq.Helpers;

namespace ObservableMessaging.IbmMq
{
    public class InboundMessageQueue : IObservable<MQMessage>
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
            IObserver<MQMessage> errorQueue = null,
            IObserver<Exception> exceptions = null)
        {
            _qmgr = qmgr;
            _useTransactions = useTransactions;
            _connectionProperties = MQConnectionPropertiesHelper.CreateConnectionProperties(channel, host, port);

            _underlying = new RawInboundMessageQueue(qmgr, qname, channel, host, port, correlationId, messageType, concurrentWorkers, useTransactions, numBackoutAttempts, autoStart, errorQueue, exceptions, NewMessageOptions, NewMessage, NewQueueManager);
        }

        public IDisposable Subscribe(IObserver<MQMessage> observer)
        {
            return _underlying.Subscribe(observer);
        }

        private MQMessage NewMessage()
        {
            MQMessage mqMessage = new MQMessage();
            return mqMessage;
        }

        private MQGetMessageOptions NewMessageOptions()
        {
            MQGetMessageOptions mqgmo = new MQGetMessageOptions();
            mqgmo.Options = 0;
            if (_useTransactions)  {
                mqgmo.Options |= MQC.MQGMO_SYNCPOINT;
            }
            mqgmo.Options |= MQC.MQGMO_WAIT;
            mqgmo.Options |= MQC.MQGMO_FAIL_IF_QUIESCING;
            return mqgmo;
        }

        private MQQueueManager NewQueueManager()
        {
            return new MQQueueManager(_qmgr, _connectionProperties);
        }
    }
}