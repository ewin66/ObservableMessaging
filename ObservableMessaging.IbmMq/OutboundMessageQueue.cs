using IBM.WMQ;
using log4net;
using ObservableMessaging.IbmMq.Helpers;
using System;
using System.Collections;

namespace ObservableMessaging.IbmMq
{
    public class OutboundMessageQueue : IObserver<MQMessage>
    {
        private static readonly ILog logger = LogManager.GetLogger(typeof(OutboundMessageQueue));

        private readonly string _qmgr;
        private readonly string _qname;
        private readonly string host;
        private readonly int? port;
        private readonly string channel;
        private readonly Func<MQQueueManager> _mqQueueManagerFactory;

        private readonly Hashtable _connectionProperties;

        private MQQueueManager queueManager = null;
        private MQQueue queue = null;

        public OutboundMessageQueue(string qmgr, string qname, string host, int? port, string channel, Func<MQQueueManager> mqQueueManagerFactory=null)
        {
            this._qmgr = qmgr;
            this._qname = qname;
            this.host = host;
            this.port = port;
            this.channel = channel;
            _connectionProperties = MQConnectionPropertiesHelper.CreateConnectionProperties(channel, host, port);

            _mqQueueManagerFactory = mqQueueManagerFactory??  ( () => new MQQueueManager(_qmgr, _connectionProperties) );
        }

        public void OnCompleted()
        {
        }

        public void OnError(Exception error)
        {
        }

        public void OnNext(MQMessage value)
        {
            if (queue == null || queueManager == null || !queueManager.IsConnected || !queue.IsOpen) {
                queueManager = _mqQueueManagerFactory();
                queue = queueManager.AccessQueue(_qname, MQC.MQOO_INPUT_AS_Q_DEF | MQC.MQOO_OUTPUT);
            }
            MQPutMessageOptions pmo = new MQPutMessageOptions();
            queue.Put(value, pmo);
        }
    }
}
