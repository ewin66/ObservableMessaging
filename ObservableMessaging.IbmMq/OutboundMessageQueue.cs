using System;
using IBM.WMQ;

namespace ObservableMessaging.IbmMq
{
    public class OutboundMessageQueue : IObserver<MQMessage>
    {
        public void OnCompleted()
        {
            throw new NotImplementedException();
        }

        public void OnError(Exception error)
        {
            throw new NotImplementedException();
        }

        public void OnNext(MQMessage value)
        {
            throw new NotImplementedException();
        }
    }
}
