using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PinIssuance.Net.Client.Pos.Contract;

namespace PinIssuance.Net.Client.Pos.Request
{
    public class PinIssuanceResponse : IResponse
    {
        public string Status { get; set; }
        public string IAD { get; set; }
        public string IssuerScript { get; set; }
    }
}