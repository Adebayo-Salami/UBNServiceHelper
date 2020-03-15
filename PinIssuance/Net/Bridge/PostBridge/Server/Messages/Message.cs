using System;
using PinIssuance.Net.Bridge.PostBridge.Utilities;

namespace PinIssuance.Net.Bridge.PostBridge.Server.Messages
{
    // this used to be internal, i change it to public to simulate a POS transaction
    // must be changed back to internal after test ... Ahmed

    public abstract class Message
    {
        private Trx.Messaging.Iso8583.Iso8583Message _isoMsg;
        private long _SystemTraceAuditNumber;
        private DateTime _TransDateTime;
        private DateTime _LocalDateTime;

        public ISO8583DataExtractor TheMessage { get; set; }

        public long SystemTraceAuditNumber
        {
            get { return _SystemTraceAuditNumber; }
            set { _SystemTraceAuditNumber = value; }
        }

        public DateTime TransDateTime
        {
            get { return _TransDateTime; }
            set { _TransDateTime = value; }
        }

        public DateTime LocalDateTime
        {
            get { return _LocalDateTime; }
            set { _LocalDateTime = value; }
        }

        public Trx.Messaging.Iso8583.Iso8583Message IsoMessage
        {
            get;
            set;
        }

        public long? TheTransactionID { get; set;}

        public Message(ISO8583DataExtractor message)
        {
            this.IsoMessage = message.IsoMessage;
            this._SystemTraceAuditNumber = message.SystemTraceAuditNumber;
            this.TheMessage = message;
        }

        public virtual bool Validate()
        {
            bool result = true;

            return result;
        }

        public abstract Trx.Messaging.Iso8583.Iso8583Message Execute();

        public override string ToString()
        {
            return this._isoMsg.ToString();
        }


    }
}
