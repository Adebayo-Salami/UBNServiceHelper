using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Runtime.Serialization;

namespace PinIssuance.Core
{
    [DataContract]
    public class ConnectedTerminal
    {
        [DataMember]
        public string IpAddress { get; set; }
        [DataMember]
        public string Port { get; set; }
        [IgnoreDataMember]
        public TcpClient ClientSocket { get; set; }
        [DataMember]
        public string ClientID { get; set; }
    }
}
