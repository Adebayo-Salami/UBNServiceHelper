using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PinIssuance.Net.Bridge.PostBridge.Client.DTO
{
    public class IccResponse
    {
        public IccResponse() { }
        public IccResponse(string iccData)
        { 
        
        }
        public string ApplicationTransactionCounter { get; set; }
        public string CardAuthenticationResultsCode { get; set; }
        public string IssuerAuthenticationData { get; set; }
        public string IssuerScriptTemplate1 { get; set; }
        public string IssuerScriptTemplate2 { get; set; }
    }
}