using IBM.WMQ;
using log4net;
using ObservableMessaging.IbmMq.Subjects;
using System;

namespace ObservableMessaging.IbmMq.ConsoleApp
{
    class Program
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(Program));
        static void Main(string[] args)
        {
            IObserver<MQMessage> errorQueue = new OutboundMessageQueue("QM1", "DEV.DEAD.LETTER.QUEUE", host: "hostname.local", port: 1414, channel: "DEV.APP.SVRCONN");
            IObservable<MQMessage> inbound = new InboundMessageQueue("QM1", "DEV.QUEUE.1", host: "hostname.local", port: 1414, channel: "DEV.APP.SVRCONN", errorQueue: errorQueue);

            IObservable<string> stringObservable = new MQStringAdapterSubject(inbound);

            stringObservable.Subscribe(mqmessage => {
                log.Info($"Received message: {mqmessage}");
                throw new Exception("Opsy");
            });
            Console.ReadLine();
        }
    }
}
