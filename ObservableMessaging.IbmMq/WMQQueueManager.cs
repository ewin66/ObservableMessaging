using IBM.WMQ;
using ObservableMessaging.IbmMq.Core.Interfaces;
using System;
using System.Collections;

namespace ObservableMessaging.IbmMq
{
    public class WMQQueueManager : IWMQQueueManager, IDisposable
    {
        private readonly MQQueueManager _queueManager;

        public WMQQueueManager()
        {
            _queueManager = new MQQueueManager();
        }

        public WMQQueueManager(string queueManagerName) {
            _queueManager = new MQQueueManager(queueManagerName);
        }

        public WMQQueueManager(string queueManagerName, int Options) {
            _queueManager = new MQQueueManager(queueManagerName, Options);
        }

        public WMQQueueManager(string queueManagerName, Hashtable properties) {
            _queueManager = new MQQueueManager(queueManagerName, properties);
        }

        public WMQQueueManager(string queueManagerName, string Channel, string ConnName) {
            _queueManager = new MQQueueManager(queueManagerName, Channel, ConnName);
        }

        public WMQQueueManager(string queueManagerName, int Options, string Channel, string ConnName) {
            _queueManager = new MQQueueManager(queueManagerName, Options, Channel, ConnName);
        }

        public bool IsConnected => _queueManager.IsConnected;

        public IWMQQueue AccessQueue(string queueName, int openOptions)
        {
            MQQueue queue = _queueManager.AccessQueue(queueName, openOptions);
            return new WMQQueue(queue);
        }

        public void Backout()
        {
            _queueManager.Backout();
        }

        public void Commit()
        {
            _queueManager.Commit();
        }

        public void Dispose()
        {
            if (_queueManager is IDisposable)
                (_queueManager as IDisposable).Dispose();
        }
    }

}
