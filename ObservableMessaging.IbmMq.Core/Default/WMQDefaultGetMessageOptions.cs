using ObservableMessaging.IbmMq.Core.Interfaces;
using System;

namespace ObservableMessaging.IbmMq.Core.Default
{
    public class WMQDefaultGetMessageOptions : IWMQGetMessageOptions
    {
        public int Options { get; set; } = 0;
    }

    public class WMQDefaultMessage : IWMQMessage
    {
        private byte[] _messageId = Guid.NewGuid().ToByteArray();

        public int BackoutCount { get; set; } = 0;

        public byte[] CorrelationId { get; set; }

        public int MessageLength => _value == null ? 0 : _value.Length;

        public byte[] MessageId => _messageId;

        private string _value;

        public void WriteString(string value)
        {
            _value = value;
        }

        public string ReadString(int length)
        {
            return _value;
        }
    }
}
