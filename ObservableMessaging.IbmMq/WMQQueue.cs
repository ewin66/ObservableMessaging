using IBM.WMQ;
using ObservableMessaging.IbmMq.Core.Interfaces;
using System;

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

        public void Get(IWMQMessage message, IWMQGetMessageOptions gmo, int maxMsgSize, int spigmo)
        {
            MQMessage mqMessage = new MQMessage();
            MQGetMessageOptions getMessageOptions = new MQGetMessageOptions();
            getMessageOptions.Options = gmo.Options;
            _underlying.Get(mqMessage, getMessageOptions, maxMsgSize, spigmo);
            string value = mqMessage.ReadString(mqMessage.MessageLength);
            message.WriteString(value);
        }

        public void Get(IWMQMessage message)
        {
            MQMessage mqMessage = new MQMessage();
            _underlying.Get(mqMessage);
            string value = mqMessage.ReadString(mqMessage.MessageLength);
            message.WriteString(value);
        }

        public void Get(IWMQMessage message, IWMQGetMessageOptions gmo)
        {
            if (message is WMQMessage) {
                MQMessage mqMessage = ((WMQMessage)message).MQMessage;
                MQGetMessageOptions getMessageOptions = new MQGetMessageOptions();
                getMessageOptions.Options = gmo.Options;
                _underlying.Get(mqMessage, getMessageOptions);
            }
            else throw new ArgumentException($"Expected message of type {nameof(WMQMessage)}");
        }
    }

}
