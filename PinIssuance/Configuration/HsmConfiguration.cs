using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Text;

namespace PrimeUtility.Configuration
{
    public class HsmConfiguration
    {
        NameValueCollection HsmConfig = null;
        public HsmConfiguration()
        {
            HsmConfig = System.Configuration.ConfigurationManager.GetSection("PinIssuance.HSM.Bridge") as NameValueCollection;
        }

        public string HsmHost
        {
            get
            {
                return HsmConfig["HsmHost"];
            }
        }

        public int HsmPort
        {
            get
            {
                return Convert.ToInt32(HsmConfig["HsmPort"]);
            }
        }

        public int HsmHeaderLength
        {
            get
            {
                return Convert.ToInt32(HsmConfig["HsmHeaderLength"]);
            }
        }

        public string BDK
        {
            get
            {
                return HsmConfig["BDK"];
            }
        }

        public string ZPKLocation
        {
            get
            {
                return HsmConfig["ZPKLocation"];
            }
        }

        public string ZPK
        {
            get
            {
                if (!File.Exists(Path.Combine(ZPKLocation, "zpk.key")))
                {
                    throw new ApplicationException("Key exchange is yet to be done");
                }
                string sessionKey = File.ReadAllText(Path.Combine(ZPKLocation, "zpk.key"));

                return string.IsNullOrEmpty(sessionKey) && sessionKey.Contains("-") ? "" : sessionKey.Split('-')[0];
            }
        }

        public string ZPK_LMK
        {
            get
            {
                if (!File.Exists(Path.Combine(ZPKLocation, "storage.key")))
                {
                    throw new ApplicationException("Translated exchange key does not exist");
                }
                string translatedKey = File.ReadAllText(Path.Combine(ZPKLocation, "storage.key"));

                return string.IsNullOrEmpty(translatedKey) && translatedKey.Contains("-") ? "" : translatedKey.Split(new string[] { "-" }, StringSplitOptions.RemoveEmptyEntries)[0];
            }
        }


        public string PinVerificationKey
        {
            get
            {
                return HsmConfig["PinVerificationKey"];
            }
        }

        public string PinVerificationKeyVISA
        {
            get
            {
                return HsmConfig["PinVerificationKeyVISA"];
            }
        }

        public string DecimalisationTable
        {
            get
            {
                return HsmConfig["DecimalisationTable"];
            }
        }
        public string PinValidationData
        {
            get
            {
                return HsmConfig["PinValidationData"];
            }
        }
        public string CardVerificationKey
        {
            get
            {
                return HsmConfig["CardVerificationKey"];
            }
        }

        public bool UpdateExternalPinOffset
        {
            get
            {
                bool update = false;
                bool.TryParse(HsmConfig["UpdateExternalPinOffset"], out update);
                return update;
            }
        }
        public char[] Track2DataDelimeters
        {
            get
            {
                if (string.IsNullOrEmpty(HsmConfig["Track2DataDelimeters"])) throw new ApplicationException("Track2DataDelimeters config item is empty. Populate Track2DataDelimeters item in ViaCard.PinIssuance.HSM.Bridge config section");
                return HsmConfig["Track2DataDelimeters"].ToCharArray();
            }
        }

    }
}
