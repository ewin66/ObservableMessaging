using IBM.WMQ;
using ObservableMessaging.IbmMq.Core.Interfaces;

namespace ObservableMessaging.IbmMq
{
    public class WMQMessage : IWMQMessage
    {
        private readonly MQMessage _message;

        public MQMessage MQMessage { get =>  _message; }

        public WMQMessage(MQMessage message)
        {
            _message = message;
        }

        public int BackoutCount => _message.BackoutCount;

        public byte[] CorrelationId { get => _message.CorrelationId; set => _message.CorrelationId = value; }

        public int MessageLength => _message.MessageLength;

        public void WriteString(string value) => _message.WriteString(value);

        public string ReadString(int length) => _message.ReadString(length);
    }
}
