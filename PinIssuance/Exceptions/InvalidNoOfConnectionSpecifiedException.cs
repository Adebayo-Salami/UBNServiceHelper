using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PinIssuance.Services.Exceptions
{
    public class InvalidNumberOfConnectionSpecifiedException : Exception
    {
        public InvalidNumberOfConnectionSpecifiedException() : base("Invalid number of connection specified. Correct the number of cuncurrent connectetion allowed and try again") { }
    }
}
