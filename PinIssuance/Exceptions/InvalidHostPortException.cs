using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PinIssuance.Services.Exceptions
{
    public class InvalidHostPortException : Exception
    {
        public InvalidHostPortException() : base("No or invalid Host Port specified") { }
    }
}
