using IBM.WMQ;
using log4net;
using ObservableMessaging.IbmMq.Subjects;
using System;
using System.Threading;

namespace ObservableMessaging.IbmMq.ConsoleApp
{
    class Program
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(Program));

        static void Main(string[] args)
        {
            IObserver<MQMessage> errorQueue = new OutboundMessageQueue("QM1", "DEV.DEAD.LETTER.QUEUE", host: "mqhost.local", port: 1414, channel: "DEV.APP.SVRCONN");

            IObservable<MQMessage> inbound = new InboundMessageQueue("QM1", "DEV.QUEUE.1", host: "mqhost.local", port: 1414, channel: "DEV.APP.SVRCONN", errorQueue: errorQueue);
            IObserver<MQMessage> outbound = new OutboundMessageQueue("QM1", "DEV.QUEUE.1", host: "mqhost.local", port: 1414, channel: "DEV.APP.SVRCONN");

            IObservable<string> stringObservable = new MQStringAdapterSubject(inbound);

            stringObservable.Subscribe(mqmessage => {
                log.Info($"Received message: {mqmessage}");
                // throw new Exception("Opsy");
            });

            while (true) {
                MQMessage message = new MQMessage();
                message.WriteString("hello");

                outbound.OnNext(message);
                Thread.Sleep(250);
            }

            Console.ReadLine();
        }
    }
}
