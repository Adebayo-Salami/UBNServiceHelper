using PrimeUtility.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ThalesSim.Core;
using ThalesSim.Core.Cryptography;

namespace PinIssuance.Net.Bridge.HSM
{
    public class PinReverse
    {
        public static string GetPin(string cardPan, string pinOffset)
        {
            string naturalPin = GetNaturalPin(cardPan, pinOffset);
            string pin = Utility.AddNoCarry(pinOffset, naturalPin);

            return pin;
        }

        public static string GetPinOffset(string cardPan, string pin)
        {
            string naturalPin = GetNaturalPin(cardPan, pin);
            string offsetValue = Utility.SubtractNoBorrow(pin, naturalPin);

            return offsetValue;
        }

        private static string GetNaturalPin(string cardPan, string pin)
        {
            string acctNo = cardPan.Substring(cardPan.Length - 13, 12);
            string pinValData = cardPan.Substring(0, 10) + "N" + cardPan.Last();
            string expPinValData = pinValData.Substring(0, pinValData.IndexOf("N"));
            expPinValData = expPinValData + acctNo.Substring(acctNo.Length - 5, 5);
            expPinValData = expPinValData + pinValData.Substring(pinValData.IndexOf("N") + 1, (pinValData.Length - (pinValData.IndexOf("N") + 1)));
            // The PVK is a double-length key, so we need to do a 3DES-decrypt.
            string cryptAcctNum = TripleDES.TripleDESEncrypt(new HexKey(ConfigurationManager.HsmConfig.PinVerificationKey), expPinValData);
            string decimalisedAcctNum = Utility.Decimalise(cryptAcctNum, ConfigurationManager.HsmConfig.DecimalisationTable);
            string naturalPin = decimalisedAcctNum.Substring(0, pin.Length);

            return naturalPin;
        }
    }
}
