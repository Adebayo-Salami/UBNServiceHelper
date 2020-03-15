using PinIssuance.Net.Bridge.PostBridge.Server.Messages;
using PinIssuance.Net.Bridge.PostBridge.Utilities;

namespace PinIssuance.Net.Bridge
{
    public class MessageParser
    {
        internal static Message Read(Trx.Messaging.Iso8583.Iso8583Message isoMessage, out bool isSignOn)
        {
            Message result = null;
            isSignOn = false;
            ISO8583DataExtractor message = new ISO8583DataExtractor(isoMessage);
            if (isoMessage.IsNetworkManagement())
            {
                if (isoMessage.Fields.Contains(FieldNos.F70_NetworkMgtInfoCode))
                {
                    switch (isoMessage.Fields[FieldNos.F70_NetworkMgtInfoCode].Value.ToString())
                    {
                        //Sign On
                        case "001":
                            result = new SignOn(message);
                            isSignOn = true;
                            break;
                        //Sign Off
                        case "002":
                            result = new SignOff(message);
                            break;
                        //Echo
                        case "301":
                            result = new Echo(message);
                            isSignOn = true;
                            break;
                        default:
                            result = new Echo(message);
                            break;
                    }
                }
            }
            else
            {
                result = new Echo(message);
            }
            return result;
        }

    }
}
