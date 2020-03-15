using System;
using System.Collections.Generic;
using System.Text;
using System.Collections.Specialized;
using System.IO;

namespace PrimeUtility.Configuration
{
    public class ConfigurationManager
    {
        public static FepConfiguration FepConfig
        {
            get
            {
                return new FepConfiguration();
            }
        }

        public static HsmConfiguration HsmConfig
        {
            get
            {
                return new HsmConfiguration();
            }
        }

        public static PosConfiguration PosConfig
        {
            get
            {
                return new PosConfiguration();
            }
        }



        public class FepConfiguration
        {
            NameValueCollection FepConfig = null;
            public FepConfiguration()
            {
                FepConfig = System.Configuration.ConfigurationManager.GetSection("PinIssuance.FEP.Bridge") as NameValueCollection;
            }

            public string FepIp
            {
                get
                {
                    return FepConfig["FepIp"];
                }
            }

            public string BridgeHostIp
            {
                get
                {
                    return FepConfig["BridgeHostIp"];
                }
            }

            public int ExternalServerPort
            {
                get
                {
                    return Convert.ToInt32(FepConfig["ExternalServerPort"]);
                }
            }

            public int InternalServerPort
            {
                get
                {
                    return Convert.ToInt32(FepConfig["InternalServerPort"]);
                }
            }

            public int RequestTimeout
            {
                get
                {
                    int timeout = 15000;
                    int.TryParse(FepConfig["RequestTimeout"], out timeout);
                    return timeout;
                }
            }

            public bool KeyExchangeOnConnect
            {
                get
                {
                    bool keyXhange = true;
                    bool.TryParse(FepConfig["KeyExchangeOnConnect"], out keyXhange);
                    return keyXhange;
                }
            }

            public bool IccDataIsInXmlFormat
            {
                get
                {
                    bool xmlFormat = false;
                    bool.TryParse(FepConfig["IccDataIsInXmlFormat"], out xmlFormat);
                    return xmlFormat;
                }
            }

            public string ZMK
            {
                get
                {
                    return FepConfig["ZMK"];
                }
            }
             
        }

        public class PosConfiguration
        {
            NameValueCollection PosConfig = null;
            public PosConfiguration()
            {
                PosConfig = System.Configuration.ConfigurationManager.GetSection("PinIssuance.POS.Bridge") as NameValueCollection;
            }


            public string PosHostAddress
            {
                get
                {
                    return PosConfig["PosHostAddress"];
                }
            }

            public int PosHostPort
            {
                get
                {
                    return Convert.ToInt32(PosConfig["PosHostPort"]);
                }
            }

            public int PosConnectionTimeout
            {
                get
                {
                    return Convert.ToInt32(PosConfig["PosConnectionTimeout"]);
                }
            }

            public string Tpk1
            {
                get
                {
                    return string.IsNullOrEmpty(PosConfig["Tpk1"]) ? string.Empty : PosConfig["Tpk1"].Replace(" ","");
                }
            }

            public string Tpk2
            {
                get
                {
                    return string.IsNullOrEmpty(PosConfig["Tpk2"]) ? string.Empty : PosConfig["Tpk2"].Replace(" ", "");
                }
            }

            public string BankName
            {
                get
                {
                    return PosConfig["BankName"];
                }
            }                         
        }

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

            public char[] Track2DataDelimeters
            {
                get
                {
                    if (string.IsNullOrEmpty(HsmConfig["Track2DataDelimeters"])) throw new ApplicationException("Track2DataDelimeters config item is empty. Populate Track2DataDelimeters item in PinIssuance.HSM.Bridge config section");
                    return HsmConfig["Track2DataDelimeters"].ToCharArray();
                }
            }

        }
    }
}
