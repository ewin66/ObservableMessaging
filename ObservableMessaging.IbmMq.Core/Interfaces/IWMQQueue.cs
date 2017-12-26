using IBM.WMQ;

namespace ObservableMessaging.IbmMq.Core.Interfaces
{
    public interface IWMQQueue
    {
        bool IsOpen { get; }

        void Get(MQMessage message, MQGetMessageOptions gmo, int maxMsgSize, int spigmo);
        void Get(MQMessage message);
        void Get(MQMessage message, MQGetMessageOptions gmo);
    }
}
