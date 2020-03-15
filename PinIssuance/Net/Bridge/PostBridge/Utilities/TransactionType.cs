using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PinIssuance.Net.Bridge.PostBridge.Utilities
{
    public enum TransactionType
    {
        Purchase = 0,
        CashWithdrawal = 1,
        BalanceEnquiry = 31,
        FullStatement = 36,
        MiniStatementInquiry = 38,
        LinkedAccountInquiry = 39,
        AccountsTransfer = 40,
        Payment = 50,
        HotListCard = 90,
        ChangePIN = 92

    }
}
