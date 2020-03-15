using System;
using System.Collections.Generic;
using System.Text;
using PinIssuance.Net.Bridge.PostBridge.Client.DTO;
using PinIssuance.Net.Bridge.PostBridge.Utilities;

namespace PinIssuance.Net.Bridge.PostBridge.Client.Response
{
    public class ChangePINResponse : MessageResponse
    {
        private string _issuerScript;
        private string _issuerAuthenticationData;
        private string _iccData;
        public ChangePINResponse(Trx.Messaging.Message responseMessage)
            : base(responseMessage)
        {
            if (responseMessage.Fields.Contains(127))
            {
                Trx.Messaging.Message field127 = responseMessage.Fields[127].Value as Trx.Messaging.Message;
                if (field127 != null && field127.Fields.Contains(25))
                {
                    string field127_25 = field127.Fields[25].Value.ToString() ;
                    IccData iccData = XMLSerializer.DeserializeXML<IccData>(field127_25);
                    if (iccData != null && iccData.IccResponse != null)
                    {
                        _issuerScript = iccData.IccResponse.IssuerScriptTemplate2;
                        _issuerAuthenticationData = iccData.IccResponse.IssuerAuthenticationData;
                        _iccData = field127_25;
                    }

                }

            }
        }

        public string IccData
        {
            get { return _iccData; }
            set { _iccData = value; }
        }

        public string IssuerScript
        {
            get
            {
                return this._issuerScript;
            }
        }

        public string IssuerAuthenticationData
        {
            get
            {
                return this._issuerAuthenticationData;
            }
        }
    }
}
