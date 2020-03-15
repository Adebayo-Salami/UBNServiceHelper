using System;
using PinIssuance.Net.Bridge.PostBridge.Utilities;

namespace PinIssuance.Net.Bridge.PostBridge.Client.Response
{
    public abstract class MessageResponse
    {
        private string _ResponseCode;
        private string _transactionID;
        private DateTime _transDateTime;
        private string _SystemsTraceAuditNumber;

        public MessageResponse(Trx.Messaging.Message responseMessage)
        {
            if (responseMessage.Fields.Contains(39))
            {
                this._ResponseCode = responseMessage.Fields[39].Value.ToString();
            }
            if (responseMessage.Fields.Contains(11))
            {
                this._SystemsTraceAuditNumber = responseMessage.Fields[11].Value.ToString();
            }
        }

        /// <summary>
        /// Gets or sets the response code.
        /// </summary>
        /// <value>The response code.</value>
        public string ResponseCode
        {
            get
            {
                return this._ResponseCode;
            }
            set
            {
                this._ResponseCode = value;
            }
        }

        /// <summary>
        /// Gets or sets the response description.
        /// </summary>
        /// <value>The response description.</value>
        public string ResponseDescription
        {
            get
            {
                return ResponseDescriptor.GetResponseDescription(this._ResponseCode);
            }
        }
        public string SystemsTraceAuditNumber
        {
            get
            {
                return this._SystemsTraceAuditNumber;
            }
            set
            {
                this._SystemsTraceAuditNumber = value;
            }
        }
        public string TransactionID
        {
            get
            {
                return this._transactionID;
            }
            set
            {
                this._transactionID = value;
            }
        }
        public DateTime TransDateTime
        {
            get
            {
                return this._transDateTime;
            }
            set
            {
                this._transDateTime = value;
            }
        }
    }
}
