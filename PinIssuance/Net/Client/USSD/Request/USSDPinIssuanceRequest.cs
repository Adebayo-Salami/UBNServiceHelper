using PinIssuance.Net.Client.Pos.Contract;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PinIssuance.Net.Client.USSD.Request
{
    public class USSDPinIssuanceRequest : IRequest
    {
        public string CardPAN { get; set; }
        public string ExpiryDate { get; set; }
        public string Function { get; set; }
        public string IccData { get; set; }
        public string Pin { get; set; }
        public string TerminalId { get; set; }
        public string TerminalSerial { get; set; }

    }
}
