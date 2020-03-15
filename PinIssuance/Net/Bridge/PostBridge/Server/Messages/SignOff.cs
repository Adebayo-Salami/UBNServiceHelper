using System;
using PinIssuance.Net.Bridge.PostBridge.Utilities;

namespace PinIssuance.Net.Bridge.PostBridge.Server.Messages
{
    internal class SignOff : Message
    {
        public SignOff(ISO8583DataExtractor message)
            : base(message)
        { 
        
        }

        public override Trx.Messaging.Iso8583.Iso8583Message Execute()
        {
            Trx.Messaging.Iso8583.Iso8583Message responseMessage = this.IsoMessage.Clone() as Trx.Messaging.Iso8583.Iso8583Message;
            responseMessage.Fields.Add(FieldNos.F39_ResponseCode, "00");

            responseMessage.Fields.Remove(new int[] { FieldNos.F7_TransDateTime, FieldNos.F12_TransLocalTime, FieldNos.F13_TransLocalDate });
            responseMessage.SetResponseMessageTypeIdentifier();

            DateTime transmissionDate = DateTime.Now;
            responseMessage.Fields.Add(FieldNos.F7_TransDateTime, string.Format("{0}{1}",
                string.Format("{0:00}{1:00}", transmissionDate.Month, transmissionDate.Day),
                string.Format("{0:00}{1:00}{2:00}", transmissionDate.Hour,
                transmissionDate.Minute, transmissionDate.Second)));

            responseMessage.Fields.Add(FieldNos.F12_TransLocalTime, string.Format("{0:00}{1:00}{2:00}", transmissionDate.Hour,
                transmissionDate.Minute, transmissionDate.Second));
            responseMessage.Fields.Add(FieldNos.F13_TransLocalDate, string.Format("{0:00}{1:00}", transmissionDate.Month, transmissionDate.Day));

            //responseMessage.Fields.Remove(127);
            return responseMessage;
        }

       
    }
}
