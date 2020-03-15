using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PinIssuance.Net.Bridge.PostBridge.Client.Messages;
using PinIssuance.Net.Bridge.PostBridge.Client.DTO;
using PinIssuance.Net.Bridge.PostBridge.Utilities;


namespace PinIssuance.Net.Bridge.PostBridge.Client.Messages
{
    public class BalanceEnquiryISO : Message
    {
        public BalanceEnquiryISO( Account acct, CardDetails theCard, string transactionID, string sequencenr)
            : base(600, transactionID)
        {
            this.Fields.Add(FieldNos.F2_PAN, theCard.PAN);
            this.Fields.Add(FieldNos.F3_ProcCode, string.Format("{0}{1}{2}", (int)TransactionType.BalanceEnquiry, AccountType.Default, AccountType.Default));
            this.Fields.Add(FieldNos.F14_CardExpiryDate, string.Format("{0:yyMM}", theCard.ExpiryDate));

            this.Fields.Add(FieldNos.F22_PosEntryMode, "011");
            this.Fields.Add(FieldNos.F25_PosConditionCode, "00");
            this.Fields.Add(FieldNos.F26_PinCaptureCode, "12");

            this.Fields.Add(FieldNos.F41_CardAcceptorTerminalCode, "terminal");
            this.Fields.Add(FieldNos.F42_CardAcceptorIDCode, "HSBC00000000000");
            this.Fields.Add(FieldNos.F43_CardAcceptorNameLocation, "test location");

            this.Fields.Add(FieldNos.F52_PinData, theCard.PIN);
           // this.Fields.Add(FieldNos.F53_SecurityInfo, theCard.NewPINBlock);

            this.Fields.Add(FieldNos.F102_Account1, "5010096187");
            //this.Fields.Add(123, "100040165110119");
            this.Fields.Add(123, "000000000000000");

            this.Fields.Add(FieldNos.F23_CardSequenceNo, sequencenr);
          //  this.Fields.Add(FieldNos.F56_MessageReasonCode, "0000");

        }
    }
}
