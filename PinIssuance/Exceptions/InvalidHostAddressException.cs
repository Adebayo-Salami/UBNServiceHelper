using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PinIssuance.Services.Exceptions
{
    public class InvalidHostAddressException : Exception
    {
        public InvalidHostAddressException() : base("No or invalid Host IP specified") { }
    }
}
