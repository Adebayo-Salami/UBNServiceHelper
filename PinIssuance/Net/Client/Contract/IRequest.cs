using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PinIssuance.Net.Client.Pos.Contract
{
    public interface IRequest
    {
        string Function { get; set; }
        string TerminalId { get; set; }
        string TerminalSerial { get; set; }
        string CardPAN { get; set; }
        string Pin { get; set; }
        string ExpiryDate { get; set; }
    }
}
