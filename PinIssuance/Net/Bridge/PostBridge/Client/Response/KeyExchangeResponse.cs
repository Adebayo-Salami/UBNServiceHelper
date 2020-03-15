namespace PinIssuance.Net.Bridge.PostBridge.Client.Response
{
    public class KeyExchangeResponse : MessageResponse
    {
        public string CheckDigit { get; set; }

        public KeyExchangeResponse(Trx.Messaging.Message responseMessage)
            : base(responseMessage)
        {
            if (responseMessage.Fields.Contains(53))
            {
                string hexValue = string.Empty;
                ThalesSim.Core.Utility.ByteArrayToHexString(responseMessage.Fields[53].Value as byte[], ref hexValue);
                int keyLenght = 2;
                SessionKey = hexValue.Substring(0, keyLenght * 16);
                CheckDigit = hexValue.Substring(keyLenght * 16, 6);
            }
            if (responseMessage.Fields.Contains(125))
            {
                string hexValue =  responseMessage.Fields[125].Value.ToString();
                int keyLenght = 2;
                SessionKey = hexValue.Substring(0, keyLenght * 16);
                CheckDigit = hexValue.Substring(keyLenght * 16, 6);
            }
        }
        public string SessionKey { get; set; }



    }
}
