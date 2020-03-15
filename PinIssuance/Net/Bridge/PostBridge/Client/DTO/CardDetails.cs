using System;
using System.Collections.Generic;
using System.Text;

namespace PinIssuance.Net.Bridge.PostBridge.Client.DTO
{
    public class CardDetails
    {
        private string _PAN;

        public string PAN
        {
            get { return _PAN; }
            set { _PAN = value; }
        }
        private DateTime _expiryDate;

        public DateTime ExpiryDate
        {
            get { return _expiryDate; }
            set { _expiryDate = value; }
        }
        private byte[] _PIN = new byte[] { 0x04, 0x04, 0x04, 0x04, 0x04, 0x04, 0x04, 0x04 };

        public byte[] PIN
        {
            get { return _PIN; }
            set { _PIN = value; }
        }

        private byte[] _newPINBlock = new byte[] { 0x04, 0x04, 0x04, 0x04, 0x04, 0x04, 0x04, 0x04 };
        public byte[] NewPINBlock {
            get { return _newPINBlock; }
            set { _newPINBlock = value; }
        }

        public string IccData { get; set; }

        public string Track2 { get; set; }
    }
}
