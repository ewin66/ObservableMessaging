using System;
using IBM.WMQ;
using ObservableMessaging.IbmMq.Core.Interfaces;

namespace ObservableMessaging.IbmMq
{
    public class OutboundMessageQueue : IObserver<IWMQMessage>
    {
        private string v1;
        private string v2;
        private string host;
        private int port;
        private string channel;

        public OutboundMessageQueue(string v1, string v2, string host, int port, string channel)
        {
            this.v1 = v1;
            this.v2 = v2;
            this.host = host;
            this.port = port;
            this.channel = channel;
        }

        public void OnCompleted()
        {
 //           throw new NotImplementedException();
        }

        public void OnError(Exception error)
        {
//            throw new NotImplementedException();
        }

        public void OnNext(IWMQMessage value)
        {
//            throw new NotImplementedException();
        }
    }
}
