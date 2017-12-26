namespace ObservableMessaging.IbmMq.Core.Interfaces
{
    public interface IWMQQueueManager
    {
        bool IsConnected { get; }

        IWMQQueue AccessQueue(string queueName, int openOptions);
        void Backout();
        void Commit();
    }
}
