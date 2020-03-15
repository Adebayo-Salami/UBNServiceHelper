using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PinIssuance.Net.Client.Pos.Contract;
using PinIssuance.Configuration;

namespace PinIssuance.Net.Client.Pos.Request
{
    public class PinIssuanceRequest : IRequest
    {
        public string Function { get; set; }
        public string TerminalId { get; set; }
        public string TerminalSerial { get; set; }
        public string OldPin { get; set; }
        public string OldPinSerial { get; set; }
        public string NewPin { get; set; }
        public string NewPinSerial { get; set; }
        public string ConfirmPin { get; set; }
        public string ConfirmPinSerial { get; set; }
        public string Track1 { get; set; }
        public string Track2 { get; set; }
        public string IccData { get; set; }
        public string CardPAN
        {
            get
            {
                return Track2.Substring(0, index);
            }
        }

        public string ExpiryDate
        {
            get
            {
                return Track2.Substring(index + 1, 4);
            }
        }

        private int index
        {
            get
            {
                return Track2.IndexOfAny(ConfigurationManager.HsmConfig.Track2DataDelimeters);
            }
        }
    }
}
