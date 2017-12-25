using IBM.WMQ;
using System;
using System.Reactive.Subjects;

namespace ObservableMessaging.IbmMq.Subjects
{
    public class MQStringAdapterSubject : ISubject<MQMessage, string>
    {
        private readonly IObservable<MQMessage> _incomming;

        private readonly Subject<string> _subject = new Subject<string>();

        public MQStringAdapterSubject(IObservable<MQMessage> incomming)
        {
            _incomming = incomming;
            _incomming.Subscribe(this);
        }

        public void OnCompleted()
        {
            _subject.OnCompleted();
        }

        public void OnError(Exception error)
        {
            _subject.OnError(error);
        }

        public void OnNext(MQMessage value)
        {
            string readString = value.ReadString(value.MessageLength);
            try {
                _subject.OnNext(readString);
            }
            catch (Exception exc) {
                _subject.OnError(exc);
            }
        }

        public IDisposable Subscribe(IObserver<string> observer)
        {
            return _subject.Subscribe(observer);
        }
    }
}
