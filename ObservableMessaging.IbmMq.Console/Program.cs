using IBM.WMQ;
using ObservableMessaging.IbmMq.Subjects;
using System;

namespace ObservableMessaging.IbmMq.ConsoleApp
{
    class Program
    {
        static void Main(string[] args)
        {
            IObservable<MQMessage> inbound = new InboundMessageQueue("QM1", "DEV.QUEUE.1", host: "johns-mac-pro.local", port: 1414, channel: "DEV.APP.SVRCONN");
            IObservable<string> stringObservable = new MQStringAdapterSubject(inbound);
            stringObservable.Subscribe(mqmessage => {
                Console.WriteLine($"Received message: {mqmessage}");
            });
            Console.ReadLine();
        }
    }
}
