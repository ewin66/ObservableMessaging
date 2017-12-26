namespace ObservableMessaging.IbmMq.Core.Interfaces
{
    public interface IWMQQueue
    {
        bool IsOpen { get; }

        void Get(IWMQMessage message, IWMQGetMessageOptions gmo, int maxMsgSize, int spigmo);
        void Get(IWMQMessage message);
        void Get(IWMQMessage message, IWMQGetMessageOptions gmo);
    }

    public interface IWMQMessage
    {
        int BackoutCount { get; }
        byte[] CorrelationId { get; set; }
        int MessageLength { get; }

        void WriteString(string value);
        string ReadString(int messageLength);
    }

    public interface IWMQGetMessageOptions
    {
        int Options { get; set; }
    }
}
