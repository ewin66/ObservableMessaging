using IBM.WMQ;
using System.Collections;

namespace ObservableMessaging.IbmMq.Helpers
{
    public static class MQConnectionPropertiesHelper
    {
        public static Hashtable CreateConnectionProperties(string channel, string host, int? port)
        {
            Hashtable connectionProperties = new Hashtable();
            if (!string.IsNullOrEmpty(host))
                connectionProperties.Add(MQC.HOST_NAME_PROPERTY, host);

            if (!string.IsNullOrEmpty(channel))
                connectionProperties.Add(MQC.CHANNEL_PROPERTY, channel);

            if (port.HasValue)
                connectionProperties.Add(MQC.PORT_PROPERTY, port.Value);

            connectionProperties.Add(MQC.TRANSPORT_PROPERTY, MQC.TRANSPORT_MQSERIES_MANAGED);
            return connectionProperties;
        }
    }
}
