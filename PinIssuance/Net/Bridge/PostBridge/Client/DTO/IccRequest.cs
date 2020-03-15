using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PinIssuance.Net.Bridge.PostBridge.Utilities;

namespace PinIssuance.Net.Bridge.PostBridge.Client.DTO
{
    public class IccRequest
    {
        public IccRequest() { }

        public IccRequest(string iccData)
        {
            /*
             * Cryptogram=0|
             * CryptogramInformationData=1|
             * IssuerApplicationData=2|
             * UnpredictableNumber=3|
             * ApplicationTransactionCounter=4|
             * TerminalVerificationResult=5|
             * TransactionDate=6|
             * TransactionType=7|
             * AmountAuthorized=8|
             * TransactionCurrencyCode=9|
             * TAG_5F34_APPL_PAN_SEQNUM=10|
             * ApplicationInterchangeProfile=11|
             * TerminalCountryCode=12|
             * AmountOther=13|
             * TerminalCapabilities=14|
             * ApplicationIdentifier=15|
             * TerminalApplicationVersionNumber=16|
             * CvmResults=17|
             * TerminalType=18|
             * InterfaceDeviceSerialNumber=19|
             * TransactionCurrencyCode=20|
             * ApplicationIdentifier21|
             * TerminalApplicationVersionNumber=22|
             * TransactionSequenceCounter=23|
             * CvmResults=24
             * 
             */

            string[] iccDataArray = iccData.Split('|');
            if (iccDataArray == null || iccDataArray.Length < 25)
            {
                throw new InvalidOperationException("Invalid ICC Data");
            }
            //ApplicationIdentifier,InterfaceDeviceSerialNumber,TerminalApplicationVersionNumber,TransactionSequenceCounter

            Cryptogram = iccDataArray[0];
            CryptogramInformationData = iccDataArray[1];
            IssuerApplicationData = iccDataArray[2];
            UnpredictableNumber = iccDataArray[3];
            ApplicationTransactionCounter = iccDataArray[4];
            TerminalVerificationResult = iccDataArray[5];
            TransactionDate = iccDataArray[6];
            TransactionType = iccDataArray[7];
            AmountAuthorized = iccDataArray[8];
            TransactionCurrencyCode = string.IsNullOrEmpty(iccDataArray[9]) ? string.Empty : Convert.ToInt32(iccDataArray[9]).ToString().PadLeft(3, '0');
            ApplicationInterchangeProfile = iccDataArray[11];
            TerminalCountryCode = string.IsNullOrEmpty(iccDataArray[12]) ? string.Empty : Convert.ToInt32(iccDataArray[12]).ToString().PadLeft(3,'0');
            AmountOther = iccDataArray[13];
            TerminalCapabilities = iccDataArray[14];
            CvmResults = iccDataArray[17];
            TerminalType = iccDataArray[18];
           // ChipConditionCode = "0";
            //ApplicationIdentifier = iccDataArray[15];
            //TerminalApplicationVersionNumber = iccDataArray[16];
            //  TAG_5F34_APPL_PAN_SEQNUM =iccDataArray[10];
            //InterfaceDeviceSerialNumber = iccDataArray[19];
            //TransactionSequenceCounter = iccDataArray[23];
            //TransactionCurrencyCode = iccDataArray[20];
            //ApplicationIdentifier = iccDataArray[21];
            //TerminalApplicationVersionNumber = iccDataArray[22];
            //CvmResults = iccDataArray[24];

        }

        public string AmountAuthorized { get; set; }
        public string AmountOther { get; set; }
        public string ApplicationIdentifier { get; set; }
        public string ApplicationInterchangeProfile { get; set; }
        public string ApplicationTransactionCounter { get; set; }
        public string ApplicationUsageControl { get; set; }
        public string AuthorizationResponseCode { get; set; }
        public string CardAuthenticationReliabilityIndicator { get; set; }
        public string CardAuthenticationResultsCode { get; set; }
        public string ChipConditionCode { get; set; }
        public string Cryptogram { get; set; }
        public string CryptogramInformationData { get; set; }
        public string CvmList { get; set; }
        public string CvmResults { get; set; }
        public string InterfaceDeviceSerialNumber { get; set; }
        public string IssuerActionCode { get; set; }
        public string IssuerApplicationData { get; set; }
        public string IssuerScriptResults { get; set; }
        public string TerminalApplicationVersionNumber { get; set; }
        public string TerminalCapabilities { get; set; }
        public string TerminalCountryCode { get; set; }
        public string TerminalType { get; set; }
        public string TerminalVerificationResult { get; set; }
        public string TransactionCategoryCode { get; set; }
        public string TransactionCurrencyCode { get; set; }
        public string TransactionDate { get; set; }
        public string TransactionSequenceCounter { get; set; }
        public string TransactionType { get; set; }
        public string UnpredictableNumber { get; set; }

        public override string ToString()
        {
            IccData iccData = new IccData();
            iccData.IccRequest = this;
            string xmlRequest = XMLSerializer.Serialize<IccData>(iccData);
            xmlRequest = xmlRequest.Substring(xmlRequest.IndexOf("<IccData>"));
            return xmlRequest;
        }

        
    }
}
