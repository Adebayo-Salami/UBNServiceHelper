using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;

namespace PrimeUtility.Configuration
{
    public class UssdConfiguration
    {
        NameValueCollection UssdConfig = null;
        public UssdConfiguration()
        {
            UssdConfig = System.Configuration.ConfigurationManager.GetSection("ViaCard.PinIssuance.USSD.Bridge") as NameValueCollection;
        }


        public string UssdClientHostAddress
        {
            get
            {
                return UssdConfig["UssdClientHostAddress"];
            }
        }

        public string UssdEncryptionKey
        {
            get
            {
                return UssdConfig["UssdEncryptionKey"];
            }
        }

        public int UssdClientHostPort
        {
            get
            {
                return Convert.ToInt32(UssdConfig["UssdClientHostPort"]);
            }
        }
    }
}
