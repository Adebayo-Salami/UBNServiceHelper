using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PinIssuance.Net.Client.Pos.Request
{
    public class ParameterDownloadRequest : Contract.IRequest
    {
        public string Function { get; set; }
        public string TerminalId { get; set; }
        public string TerminalSerial { get; set; }
        public string Version { get; set; }


        public string CardPAN
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        public string Pin
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        public string ExpiryDate
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
        }
    }
}
