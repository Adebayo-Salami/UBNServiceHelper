using PinIssuance.Net.Bridge.PostBridge.Client.DTO;
using PinIssuance.Net.Bridge.PostBridge.Utilities;
using PrimeUtility.Configuration;
using System;

namespace PinIssuance.Net.Bridge.PostBridge.Client.Messages
{
    internal class ChangePIN : Message
    {
        public ChangePIN(CardAcceptor cardAcceptor, Account acct, CardDetails theCard, string transactionID, string sequencenr)
            : base(600, transactionID)
        {
            if (!string.IsNullOrWhiteSpace(theCard.Track2))
            {
                this.Fields.Add(FieldNos.F35_Track2, theCard.Track2);
            }
            this.Fields.Add(FieldNos.F2_PAN, theCard.PAN);
            this.Fields.Add(FieldNos.F3_ProcCode, string.Format("{0}{1}{2}", (int)TransactionType.ChangePIN, AccountType.Default, AccountType.Default));
            this.Fields.Add(FieldNos.F14_CardExpiryDate, string.Format("{0:yyMM}", theCard.ExpiryDate));

            //this.Fields.Add(FieldNos.F22_PosEntryMode, "051");
            this.Fields.Add(FieldNos.F22_PosEntryMode, "011");
            this.Fields.Add(FieldNos.F25_PosConditionCode, "00");
            this.Fields.Add(FieldNos.F26_PinCaptureCode, "12");

            this.Fields.Add(FieldNos.F32_AcquiringInstitutionIDCode, "639138");

            this.Fields.Add(FieldNos.F41_CardAcceptorTerminalCode, "20700000"); //cardAcceptor.TerminalID);


            this.Fields.Add(FieldNos.F42_CardAcceptorIDCode, "20700000");//cardAcceptor.ID);


            this.Fields.Add(FieldNos.F43_CardAcceptorNameLocation, string.Format("{0}{1}{2}{3}", cardAcceptor.Location, cardAcceptor.City, cardAcceptor.State, cardAcceptor.Country));


            this.Fields.Add(FieldNos.F52_PinData, theCard.PIN);
            this.Fields.Add(FieldNos.F53_SecurityInfo, theCard.NewPINBlock);
            
            this.Fields.Add(FieldNos.F102_Account1, acct.Number);
            //new PANE.ERRORLOG.Error().LogInfo("acct.Number =" + acct.Number);
            // this.Fields.Add(123, "100040165110119");
           // this.Fields.Add(123, "511101512344201");
            if (string.IsNullOrWhiteSpace(theCard.IccData))
            {
                this.Fields.Add(123, "110001610114021");
            }
            else
            {
                string Field123 = Convert.ToString(System.Configuration.ConfigurationManager.AppSettings["Field123"]);
                if(string.IsNullOrEmpty(Field123))
                {
                    this.Fields.Add(123, "511101512344201");
                }
                else
                {
                    this.Fields.Add(123, Field123);
                }
            }
            this.Fields.Add(FieldNos.F23_CardSequenceNo, sequencenr);
            //new PANE.ERRORLOG.Error().LogInfo("sequencenr =" + sequencenr);

            this.Fields.Add(FieldNos.F56_MessageReasonCode, "0000");

            // populate the ICC Data
            Trx.Messaging.Message field127 = new Trx.Messaging.Message();


            if (PinConfigurationManager.FepConfig.IccDataIsInXmlFormat)
            {
                //IccRequest iccData = (IccRequest)XMLSerializer.Deserialize<IccRequest>(theCard.IccData);
                if (!string.IsNullOrWhiteSpace(theCard.IccData))
                {
                    string iccDataXml = string.Format("<IccData>{0}</IccData>", theCard.IccData);
                    field127.Fields.Add(25, iccDataXml);
                    this.Fields.Add(127, field127);
                }
            }
            else
            {
                IccRequest iccData = new IccRequest(theCard.IccData);
                Trx.Messaging.Message field127_25 = new Trx.Messaging.Message();
                field127_25.Fields.Add(2, iccData.AmountAuthorized);
                field127_25.Fields.Add(3, iccData.AmountOther);
                field127_25.Fields.Add(5, iccData.ApplicationInterchangeProfile);
                field127_25.Fields.Add(6, iccData.ApplicationTransactionCounter);
                field127_25.Fields.Add(11, "0"); // ChipConditionCode
                field127_25.Fields.Add(12, iccData.Cryptogram);
                field127_25.Fields.Add(13, iccData.CryptogramInformationData);
                field127_25.Fields.Add(15, iccData.CvmResults);
                field127_25.Fields.Add(18, iccData.IssuerApplicationData);
                field127_25.Fields.Add(21, iccData.TerminalCapabilities);
                field127_25.Fields.Add(22, iccData.TerminalCountryCode);
                field127_25.Fields.Add(23, iccData.TerminalType);
                field127_25.Fields.Add(24, iccData.TerminalVerificationResult);
                field127_25.Fields.Add(26, iccData.TransactionCurrencyCode);
                field127_25.Fields.Add(27, iccData.TransactionDate);
                field127_25.Fields.Add(29, iccData.TransactionType);
                field127_25.Fields.Add(30, iccData.UnpredictableNumber);
                field127.Fields.Add(25, field127_25);
                this.Fields.Add(127, field127);
            }







            //this.Fields.Add(FieldNos.F32_AcquiringInstitutionIDCode, "639138");
            //this.Fields.Add(FieldNos.F33_ForwardingInstitutionIDCode, "111111");
            //this.Fields.Add(FieldNos.F100_ReceivingInstitutionID, "628051043");
        }
    }
}
