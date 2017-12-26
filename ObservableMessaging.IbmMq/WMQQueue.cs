using IBM.WMQ;
using ObservableMessaging.IbmMq.Core.Interfaces;

namespace ObservableMessaging.IbmMq
{
    public class WMQQueue : IWMQQueue
    {
        private readonly MQQueue _underlying;

        public WMQQueue(MQQueue underlying)
        {
            _underlying = underlying;
        }

        public bool IsOpen => _underlying.IsOpen;

        public void Get(MQMessage message, MQGetMessageOptions gmo, int maxMsgSize, int spigmo)
        {
            _underlying.Get(message, gmo, maxMsgSize, spigmo);
        }

        public void Get(MQMessage message)
        {
            _underlying.Get(message);
        }

        public void Get(MQMessage message, MQGetMessageOptions gmo)
        {
            _underlying.Get(message, gmo);
        }
    }

}
