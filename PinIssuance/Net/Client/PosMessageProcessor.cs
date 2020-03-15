using PinIssuance.Net.Bridge.HSM;
using PinIssuance.Net.Bridge.PostBridge.Client;
using PinIssuance.Net.Bridge.PostBridge.Client.DTO;
using PinIssuance.Net.Bridge.PostBridge.Client.Response;
using PinIssuance.Net.Bridge.PostCard;
using PinIssuance.Net.Client.Pos.Contract;
using PrimeUtility.BussinessObjects;
using PrimeUtility.Configuration;
using PrimeUtility.Core;
using PrimeUtility.Utility;
using System;
using System.Data.SqlClient;
using System.Globalization;
using System.IO;

namespace PinIssuance.Net.Client.Pos
{
    public class PosMessageProcessor
    {
        public IRequest TheRequest { get; set; }
        public IRequest TheResponse { get; set; }
        // public string TheMessage { get; set; }

        /*public PosMessageProcessor(string message)
        {
            TheMessage = message;
        }
        */

        [Obsolete]
        public string Execute(string TheMessage)
        {
            string response = "";

            new PANE.ERRORLOG.Error().LogInfo("message from the pos");

            try
            {
                if (!string.IsNullOrEmpty(TheMessage))
                {
                    IRequest theRequest = null;

                    // parse the raw message and generate request from it
                    theRequest = PosMessageParser.ParseRequestMessage<PinIssuanceRequest>(TheMessage) as IRequest;
                    if (theRequest == null) throw new Exception("Invalid request data");
                    response = ProcessPinRequest(theRequest as PinIssuanceRequest);
                }
            }
            catch (Exception ex)
            {
                new PANE.ERRORLOG.Error().LogToFile(ex);
                //response = string.Format("C:2|DECLINED|{0}", ex.Message);
                response = string.Format("false|{0}", ex.Message);
            }

            return response;
        }

        public string Execute(PinIssuanceRequest theRequest)
        {
            string response = "";

            new PANE.ERRORLOG.Error().LogInfo("message from the pos");

            try
            {
                if (theRequest == null)
                    throw new Exception("Invalid request data");

                response = ProcessPinRequest(theRequest);

            }
            catch (Exception ex)
            {
                new PANE.ERRORLOG.Error().LogToFile(ex);
                //response = string.Format("C:2|DECLINED|{0}", ex.Message);
                response = string.Format("false|{0}", ex.Message);
            }

            return response;
        }

        protected string ProcessPinRequest(PinIssuanceRequest request)
        {
            string response = string.Empty;
            new PANE.ERRORLOG.Error().LogInfo("Start ProcessPinRequest");

            new PANE.ERRORLOG.Error().LogInfo("Before Retrieve from PostCard");
            if (string.IsNullOrEmpty(request.Track2)) throw new Exception("Track 2 data is empty");

            Card theCard = CardUtilities.GetCardFromPostCard(request.CardPAN, "pc_cards_1_A");

            new PANE.ERRORLOG.Error().LogInfo("After Retrieve from PostCard");
            switch (request.Function.ToLower())
            {
                case "pinselection":
                    response = DoPinSelection(request, theCard);
                    break;
                case "pinchange":
                    response = DoPinChange(request, theCard);
                    break;
            }

            new PANE.ERRORLOG.Error().LogInfo("End ProcessPinRequest");
            return response;
        }

        public string DoPinSelection(PinIssuanceRequest request, Card theCard)
        {

            new PANE.ERRORLOG.Error().LogInfo("Started Do PinSelection");
            if (request.Pin != request.ConfirmPin)
            {
                throw new ApplicationException("New Pin and Confirm New Pin are not the same");
            }

            string response = string.Empty;
            ThalesHsm hsm = new ThalesHsm();
            IGeneratePinOffsetResponse pinOffsset = null;
            ChangePINResponse cpResponse = null;
            string clearRandomPin = null;

            // obatin the account number
            string accountNo = "";
            try
            {
                accountNo = theCard.pan.Substring(theCard.pan.Length - 13, 12);
            }
            catch (Exception ex)
            {
                new PANE.ERRORLOG.Error().LogToFile(ex);
                throw new ApplicationException("Unable to derive account number from card PAN. Ensure that card PAN is minimum of 16 digits");
            }

            // Step 1: Generate a new encrypted Random Pin
            string _encryptedPIN;
            try
            {
                clearRandomPin = new Random().Next(1111, 9999).ToString();
                _encryptedPIN = hsm.PinGenerator().EncryptClearPin(clearRandomPin, accountNo).EncryptedPin;
                new PANE.ERRORLOG.Error().LogInfo("Generated Default Pin");
            }
            catch (Exception ex)
            {
                new PANE.ERRORLOG.Error().LogToFile(ex);
                Exception ex2 = new ApplicationException("Unable to Generate a new default Pin");
                throw ex2;
            }

            // Step 2: Generate the Pin offset for the random pin  
            try
            {
                if (theCard.pan.StartsWith("4"))
                {
                    pinOffsset = hsm.PinGenerator().GenerateVISAPinOffset(_encryptedPIN, accountNo, theCard.pan);
                }
                else
                {
                    pinOffsset = hsm.PinGenerator().GeneratePinOffset(_encryptedPIN, accountNo, theCard.pan);

                    new PANE.ERRORLOG.Error().LogInfo("Generated Pin Offset");
                }

            }
            catch (Exception ex)
            {
                new PANE.ERRORLOG.Error().LogToFile(ex);
                Exception ex2 = new ApplicationException("Unable to Generate the Pin offset for the random pin");
                throw ex2;
            }

            // Step 3: Update PostCard with the generated pin offset
            try
            {
                CardUtilities.UpdateCardPinOffset(theCard, pinOffsset.Offset.Substring(0, 4));

                new PANE.ERRORLOG.Error().LogInfo("Updated Pin Offset");
            }
            catch (Exception ex)
            {
                new PANE.ERRORLOG.Error().LogToFile(ex);
                Exception ex2 = new ApplicationException("Unable to Update PostCard with the generated pin offset");
                throw ex;
            }

            // Step 4: Do PinChange with random pin as oldpin and translated pin as newpin
            try
            {

                new PANE.ERRORLOG.Error().LogInfo("Connecting To FEP");
                Engine theFepEngine = new PinIssuance.Net.Bridge.PostBridge.Client.Engine(
                    PinConfigurationManager.FepConfig.BridgeHostIp,
                    PinConfigurationManager.FepConfig.InternalServerPort,
                    new CardAcceptor(request.TerminalId, request.TerminalId) { },
                    "trx"
                    );
                bool isConnectedToFEP = theFepEngine.Connect();

                new PANE.ERRORLOG.Error().LogInfo(string.Format("Connected to FEP - {0}", isConnectedToFEP));
                cpResponse = theFepEngine.DoChangePIN
                    (
                        new CardDetails()
                        {
                            ExpiryDate = DateTime.ParseExact(theCard.expiry_date, "yyMM", DateTimeFormatInfo.InvariantInfo),
                            PAN = theCard.pan,
                            PIN = GetPinBlockToPopulateIn52ISO(accountNo, clearRandomPin), //pinbytearr,
                            NewPINBlock = GetPinBlockToPopulateIn53ISO(accountNo, request.ConfirmPin), //newEncryptedPinBlock
                            IccData = request.IccData,
                            Track2 = request.Track2
                        },
                        new Bridge.PostBridge.Client.DTO.Account(accountNo, ""), theCard.seq_nr
                    );
            }
            catch (Exception ex)
            {
                new PANE.ERRORLOG.Error().LogToFile(ex);
                Exception ex2 = new ApplicationException("Unable to Do PinChange with random pin as oldpin and translated pin as newpin");
                throw ex2;
            }

            // Step 5: Obtain the isser script from PinChange response and return to the Pos
            if (cpResponse == null || string.IsNullOrEmpty(cpResponse.IssuerScript)) throw new ApplicationException("Invalid pin change response");
            if (cpResponse.ResponseCode == "00")
            {
                //response = string.Format("C:1|APPROVED|{0}|{1}", cpResponse.IssuerAuthenticationData, cpResponse.IssuerScript);
                response = string.Format("true|{0}", cpResponse.IccData);
                new PANE.ERRORLOG.Error().LogInfo("Pin Change Response: " + response);
            }
            else
            {
                //  response = string.Format("C:2|DECLINED|{0}", cpResponse.ResponseCode);
                response = string.Format("false|{0}", cpResponse.ResponseDescription);
            }

            new PANE.ERRORLOG.Error().LogInfo("Ended PinSelection");
            return response;
        }

        protected string DoPinChange(PinIssuanceRequest request, Card theCard)
        {
            if (request.Pin != request.ConfirmPin)
            {
                throw new ApplicationException("New Pin and Confirm New Pin are not the same");
            }

            string response = string.Empty;
            ThalesHsm hsm = new ThalesHsm();
            ChangePINResponse cpResponse = null;

            // obatin the account number
            string accountNo = "";
            try
            {
                accountNo = theCard.pan.Substring(theCard.pan.Length - 13, 12);
            }
            catch (Exception ex)
            {
                new PANE.ERRORLOG.Error().LogToFile(ex);
                throw new ApplicationException("Unable to derive account number from card PAN. Ensure that card PAN is minimum of 16 digits");
            }

            // Step 4: Do PinChange with random pin as oldpin and translated pin as newpin
            try
            {
                Engine theFepEngine = new PinIssuance.Net.Bridge.PostBridge.Client.Engine(
                    PinConfigurationManager.FepConfig.BridgeHostIp,
                    PinConfigurationManager.FepConfig.InternalServerPort,
                    new CardAcceptor(request.TerminalId, request.TerminalId) { },
                    "trx"
                    );

                theFepEngine.Connect();

                cpResponse = theFepEngine.DoChangePIN
                    (
                        new CardDetails()
                        {
                            ExpiryDate = DateTime.ParseExact(theCard.expiry_date, "yyMM", DateTimeFormatInfo.InvariantInfo),
                            PAN = theCard.pan,
                            PIN = GetPinBlockToPopulateIn52IsoInPinChange(accountNo, request.OldPin), //pinbytearr,
                            NewPINBlock = GetPinBlockToPopulateIn53ISO(accountNo, request.ConfirmPin), //newEncryptedPinBlock
                            IccData = request.IccData,
                            Track2 = request.Track2
                        },
                        new Bridge.PostBridge.Client.DTO.Account(accountNo, ""), theCard.seq_nr
                    );
            }
            catch (Exception ex)
            {
                new PANE.ERRORLOG.Error().LogToFile(ex);
                Exception ex2 = new ApplicationException("Unable to Do PinChange with random pin as oldpin and translated pin as newpin");
                throw ex2;
            }

            // Step 8: Obtain the isser script from PinChange response and return to the Pos
            if (cpResponse == null || string.IsNullOrEmpty(cpResponse.IssuerScript)) throw new ApplicationException("Invalid pin change response");
            if (cpResponse.ResponseCode == "00")
            {
                // response = string.Format("C:1|APPROVED|{0}|{1}|", cpResponse.IssuerAuthenticationData, cpResponse.IssuerScript);
                response = string.Format("true|{0}", cpResponse.IccData);

            }
            else
            {
                //response = string.Format("C:2|DECLINED|{0}", cpResponse.ResponseCode);
                response = string.Format("false|{0}", cpResponse.ResponseDescription);

            }
            return response;
        }

        public string GetPinOffset(string pin, string pan)
        {
            string response = string.Empty;
            ThalesHsm hsm = new ThalesHsm();
            IGeneratePinOffsetResponse pinOffsset = null;
            // ChangePINResponse cpResponse = null;
            string clearPin = null;


            new PANE.ERRORLOG.Error().LogInfo("In GetPinOffset");
            // obatin the account number
            string accountNo = "";
            try
            {
                accountNo = pan.Substring(pan.Length - 13, 12);
            }
            catch (Exception)
            {
                response = "1:Invalid CardNumber, ensure the card number is minimum of 16 digits";
                return response;
            }

            new PANE.ERRORLOG.Error().LogInfo("In GetPinOffset Step 1");
            // Step 1: Generate a new encrypted Random Pin
            string _encryptedPIN;
            try
            {
                clearPin = pin;
                _encryptedPIN = hsm.PinGenerator().EncryptClearPin(clearPin, accountNo).EncryptedPin;
                // new PANE.ERRORLOG.Error().LogInfo("Clear Pin: " + clearPin);
            }
            catch (Exception ex)
            {
                new PANE.ERRORLOG.Error().LogToFile(new Exception("Unable to Encrypt clear Pin", ex));
                //Exception ex2 = new ApplicationException("System error");
                //throw ex2;
                response = "1:System PIN Error";
                return response;
            }

            new PANE.ERRORLOG.Error().LogInfo("In GetPinOffset Step 2");
            // Step 2: Generate the Pin offset for the random pin  
            try
            {
                if (pan.StartsWith("4"))
                {
                    pinOffsset = hsm.PinGenerator().GenerateVISAPinOffset(_encryptedPIN, accountNo, pan);
                }
                else
                {
                    pinOffsset = hsm.PinGenerator().GeneratePinOffset(_encryptedPIN, accountNo, pan);
                }
                // new PANE.ERRORLOG.Error().LogInfo("PinVerificationKey :" + ConfigurationManager.HsmConfig.PinVerificationKey);
                //  new PANE.ERRORLOG.Error().LogInfo("PinValidationData :" + ConfigurationManager.HsmConfig.PinValidationData);
                //  new PANE.ERRORLOG.Error().LogInfo("DecimalisationTable for pin :" + ConfigurationManager.HsmConfig.DecimalisationTable);
                //  new PANE.ERRORLOG.Error().LogInfo("PinOffsset :" + pinOffsset);
                //  new PANE.ERRORLOG.Error().LogInfo("EncryptedPIN :" + _encryptedPIN);
                //  new PANE.ERRORLOG.Error().LogInfo("AccountNo :" + accountNo);
                //  new PANE.ERRORLOG.Error().LogInfo("Card.Pan :" + pan);
            }
            catch (Exception ex)
            {
                new PANE.ERRORLOG.Error().LogToFile(new Exception("Unable to Generate the Pin offset for the random pin", ex));
                response = "1:System PIN OffSet Error";
                return response;
            }

            return string.Format("0:{0}", pinOffsset.Offset.Substring(0, 4)); ;
        }
        

        public string GetPinOffsetAccess(string pin, string pan)
        {
            string response = string.Empty;
            ThalesHsm hsm = new ThalesHsm();
            IGeneratePinOffsetResponse pinOffsset = null;
            // ChangePINResponse cpResponse = null;
            string clearPin = null;


            new PANE.ERRORLOG.Error().LogInfo("In GetPinOffset");
            // obatin the account number
            string accountNo = "";
            try
            {
                accountNo = pan.Substring(pan.Length - 13, 12);
            }
            catch (Exception)
            {
                response = "1:Invalid CardNumber, ensure the card number is minimum of 16 digits";
                return response;
            }

            new PANE.ERRORLOG.Error().LogInfo("In GetPinOffset Step 1");
            // Step 1: Generate a new encrypted Random Pin
            string _encryptedPIN;
            try
            {
                clearPin = pin;
                _encryptedPIN = hsm.PinGenerator().EncryptClearPin(clearPin, accountNo).EncryptedPin;
                // new PANE.ERRORLOG.Error().LogInfo("Clear Pin: " + clearPin);
            }
            catch (Exception ex)
            {
                new PANE.ERRORLOG.Error().LogToFile(new Exception("Unable to Encrypt clear Pin", ex));
                //Exception ex2 = new ApplicationException("System error");
                //throw ex2;
                response = "1:System PIN Error";
                return response;
            }

            new PANE.ERRORLOG.Error().LogInfo("In GetPinOffset Step 2");
            // Step 2: Generate the Pin offset for the random pin  
            try
            {

                pinOffsset = hsm.PinGenerator().GeneratePinOffset(_encryptedPIN, accountNo, pan);

                // new PANE.ERRORLOG.Error().LogInfo("PinVerificationKey :" + ConfigurationManager.HsmConfig.PinVerificationKey);
                //  new PANE.ERRORLOG.Error().LogInfo("PinValidationData :" + ConfigurationManager.HsmConfig.PinValidationData);
                //  new PANE.ERRORLOG.Error().LogInfo("DecimalisationTable for pin :" + ConfigurationManager.HsmConfig.DecimalisationTable);
                //  new PANE.ERRORLOG.Error().LogInfo("PinOffsset :" + pinOffsset);
                //  new PANE.ERRORLOG.Error().LogInfo("EncryptedPIN :" + _encryptedPIN);
                //  new PANE.ERRORLOG.Error().LogInfo("AccountNo :" + accountNo);
                //  new PANE.ERRORLOG.Error().LogInfo("Card.Pan :" + pan);
            }
            catch (Exception ex)
            {
                new PANE.ERRORLOG.Error().LogToFile(new Exception("Unable to Generate the Pin offset for the random pin", ex));
                response = "1:System PIN OffSet Error";
                return response;
            }

            return string.Format("0:{0}", pinOffsset.Offset.Substring(0, 4)); ;
        }
        public string DoPinOffsetUpdate(PinIssuanceRequest request, Card theCard)
        {
            string guid = Guid.NewGuid().ToString();
            int step = 0;
            new PANE.ERRORLOG.Error().LogInfo(string.Format("In PosMessageProcessor.DoPinSelection HIT! [{0}]; Step: {1}", guid, ++step));

            string response = string.Empty;
            ThalesHsm hsm = new ThalesHsm();
            IGeneratePinOffsetResponse pinOffsset = null;
            // ChangePINResponse cpResponse = null;
            string clearPin = null;

            if (request.Pin != request.ConfirmPin)
            {
                response = "1:Invalid request data. New Pin and Confirm New Pin are not the same";
                return response;
            }

            new PANE.ERRORLOG.Error().LogInfo(string.Format("In PosMessageProcessor.DoPinSelection [{0}]; Step: {1}", guid, ++step));
            // obatin the account number
            string accountNo = "";
            try
            {
                accountNo = theCard.pan.Substring(theCard.pan.Length - 13, 12);
            }
            catch (Exception)
            {
                response = "1:Invalid CardNumber, ensure the card number is minimum of 16 digits";
                return response;
            }

            new PANE.ERRORLOG.Error().LogInfo(string.Format("In PosMessageProcessor.DoPinSelection [{0}]; Step: {1}", guid, ++step));
            // Step 1: Generate a new encrypted Random Pin
            string _encryptedPIN;
            try
            {
                clearPin = request.Pin;
                _encryptedPIN = hsm.PinGenerator().EncryptClearPin(clearPin, accountNo).EncryptedPin;
                new PANE.ERRORLOG.Error().LogInfo("Clear Pin: " + clearPin);
            }
            catch (Exception ex)
            {
                new PANE.ERRORLOG.Error().LogToFile(new Exception("Unable to Encrypt clear Pin", ex));
                //Exception ex2 = new ApplicationException("System error");
                //throw ex2;
                response = "1:System PIN Error";
                return response;
            }

            new PANE.ERRORLOG.Error().LogInfo(string.Format("In PosMessageProcessor.DoPinSelection [{0}]; Step: {1}", guid, ++step));
            // Step 2: Generate the Pin offset for the random pin  
            try
            {
                if (theCard.pan.StartsWith("4"))
                {
                    pinOffsset = hsm.PinGenerator().GenerateVISAPinOffset(_encryptedPIN, accountNo, theCard.pan);
                }
                else
                {
                    pinOffsset = hsm.PinGenerator().GeneratePinOffset(_encryptedPIN, accountNo, theCard.pan);
                }
                new PANE.ERRORLOG.Error().LogInfo("PinVerificationKey :" + ConfigurationManager.HsmConfig.PinVerificationKey);
                new PANE.ERRORLOG.Error().LogInfo("PinValidationData :" + ConfigurationManager.HsmConfig.PinValidationData);
                new PANE.ERRORLOG.Error().LogInfo("DecimalisationTable for pin :" + ConfigurationManager.HsmConfig.DecimalisationTable);
                new PANE.ERRORLOG.Error().LogInfo("PinOffsset :" + pinOffsset);
                new PANE.ERRORLOG.Error().LogInfo("EncryptedPIN :" + _encryptedPIN);
                new PANE.ERRORLOG.Error().LogInfo("AccountNo :" + accountNo);
                new PANE.ERRORLOG.Error().LogInfo("Card.Pan :" + theCard.pan);
            }
            catch (Exception ex)
            {
                new PANE.ERRORLOG.Error().LogToFile(new Exception("Unable to Generate the Pin offset for the random pin", ex));
                response = "1:System PIN OffSet Error";
                return response;
            }

            new PANE.ERRORLOG.Error().LogInfo(string.Format("In PosMessageProcessor.DoPinSelection [{0}]; Step: {1}", guid, ++step));
            // Step 3: Update PostCard with the generated pin offset
            try
            {
                bool usePrimeHSM = Convert.ToBoolean(System.Configuration.ConfigurationManager.AppSettings["UsePrimeHSM"]);
                bool useActiveActive = Convert.ToBoolean(System.Configuration.ConfigurationManager.AppSettings["UseActiveActive"]);
                if (usePrimeHSM)
                {
                    string staticKeyName = System.Configuration.ConfigurationManager.AppSettings["StaticKeyName"];
                    string panEncryptionKey = LiteDAO.GetLocalKey(staticKeyName);
                    string encryptedPan = ThalesSim.Core.Cryptography.TripleDES.TripleDESDecrypt(new ThalesSim.Core.Cryptography.HexKey(panEncryptionKey.Substring(0, 32)), theCard.pan);
                    UpdatePinOffsetService(encryptedPan, theCard.expiry_date, pinOffsset.Offset.Substring(0, 4));
                }
                else if (useActiveActive)
                {
                    CardUtilities.UpdateCardPinOffset_ActiveActive(theCard, pinOffsset.Offset.Substring(0, 4));
                }
                else
                {
                    CardUtilities.UpdateCardPinOffset(theCard, pinOffsset.Offset.Substring(0, 4));
                }
            }
            catch (Exception ex)
            {
                new PANE.ERRORLOG.Error().LogToFile(new Exception("Unable to Update PostCard with the generated pin offset", ex));
                //Exception ex2 = new ApplicationException("System error");
                //throw ex2;
                response = "1:System PIN OffSet Update Error";
                return response;
            }

            new PANE.ERRORLOG.Error().LogInfo(string.Format("In PosMessageProcessor.DoPinSelection [{0}]; Step: {1}", guid, ++step));
            return string.Format("0:{0}", clearPin);
        }

        public string DoCardActivation(PinIssuanceRequest request, Card theCard, string pinOffset, string oldCardPan)
        {
            string newPin = PinReverse.GetPin(oldCardPan, pinOffset);
            string sessionKeyName = System.Configuration.ConfigurationManager.AppSettings["SessionKeyName"];
            string encryptionKey = LiteDAO.GetLocalKey(sessionKeyName);
            request.Pin = GetPinBlock(request.CardPAN, newPin, encryptionKey);
            request.ConfirmPin = request.Pin;

            string response = DoPinSelection(request, theCard);
            if (string.IsNullOrWhiteSpace(response))
                return "1:Could not activate card";
            if (response.StartsWith("true"))
                return "0:Succesful Card Activation";
            else
                return string.Format("1:{1}", response.Split('|')[1]);
        }

        private string GetPinBlock(string pan, string pin, string encryptionKey)
        {
            if (string.IsNullOrEmpty(pan) || string.IsNullOrEmpty(pin)) return string.Empty;

            string accountNo = pan.Substring(pan.Length - 13, 12);
            string clearPinValue = string.Format("04{0}", pin).PadRight(16, 'F');
            string clearAcctValue = "0000" + accountNo;
            string pinBlock = ThalesSim.Core.Utility.XORHexStringsFull(clearAcctValue, clearPinValue);
            string encryptedpinBlock = ThalesSim.Core.Cryptography.TripleDES.TripleDESEncrypt(new ThalesSim.Core.Cryptography.HexKey(encryptionKey), pinBlock);
            return encryptedpinBlock;
        }
        public string GetClearPosPinBlock(string encryptedPinBlock)
        {
            string clearPosPinBlock = string.Empty;
            try
            {

                string sessionKeyName = System.Configuration.ConfigurationManager.AppSettings["SessionKeyName"];

                //the decryption key here is the zpk as it is 
                string decryptionKey = LiteDAO.GetLocalKey(sessionKeyName);// PinConfigurationManager.HsmConfig.ZPK; //ThalesSim.Core.Utility.XORHexStringsFull(PinConfigurationManager.PosConfig.Tpk1, PinConfigurationManager.PosConfig.Tpk2);
                clearPosPinBlock = ThalesSim.Core.Cryptography.TripleDES.TripleDESDecrypt(new ThalesSim.Core.Cryptography.HexKey(decryptionKey.Substring(0, 32)), encryptedPinBlock);

                new PANE.ERRORLOG.Error().LogInfo("clear pinblock " + clearPosPinBlock);
                new PANE.ERRORLOG.Error().LogInfo("Configuration.PinConfigurationManager.PosConfig.Tpk1: " + PinConfigurationManager.PosConfig.Tpk1);
            }
            catch (Exception ex)
            {
                new PANE.ERRORLOG.Error().LogToFile(ex);
                Exception ex2 = new ApplicationException("Unable to Get Clear Pos PinBlock");
                throw ex2;
            }
            return clearPosPinBlock;
        }

        public string EncryptCearPinBlock(string clearPinBlock)
        {
            string encryptionKey = GetEncryptionKey();
            string encryptrdPinBlock = ThalesSim.Core.Cryptography.TripleDES.TripleDESEncrypt(new ThalesSim.Core.Cryptography.HexKey(encryptionKey), clearPinBlock);
            return encryptrdPinBlock;
        }

        public string GetEncryptionKey()
        {
            //  string sessionKey = ConfigurationManager.AppSettings["SessionKeyName"];
            string clearZmkKeyComp1 = "";
            string clearZmkKeyComp2 = "";
            string zpk = "";
            if (Convert.ToBoolean(System.Configuration.ConfigurationManager.AppSettings["UseAppzoneSwitchZpk"]))
            {
                zpk = getZpkFromAppzoneSwitch().Substring(1);
                clearZmkKeyComp1 = System.Configuration.ConfigurationManager.AppSettings["keyComp1"]; // obtain ClearZmkComp1
                clearZmkKeyComp2 = System.Configuration.ConfigurationManager.AppSettings["keyComp2"]; // obtain clearZmkComp2
                new PANE.ERRORLOG.Error().LogInfo("123X23X" + zpk + "XX0");
            }
            else
            {
                zpk = PinConfigurationManager.HsmConfig.ZPK; //LiteDAO.GetLocalKey(sessionKey);
                clearZmkKeyComp1 = Utility.GetComponent("FepKeyComponent1");//ConfigurationManager.AppSettings["keyComp1"]; // obtain ClearZmkComp1
                clearZmkKeyComp2 = Utility.GetComponent("FepKeyComponent2"); //ConfigurationManager.AppSettings["keyComp2"]; // obtain clearZmkComp2
            }
            string clearZmkKey = ThalesSim.Core.Utility.XORHexStringsFull(clearZmkKeyComp1, clearZmkKeyComp2);
            string encryptionKey = ThalesSim.Core.Cryptography.TripleDES.TripleDESDecrypt(new ThalesSim.Core.Cryptography.HexKey(clearZmkKey), zpk);
            return encryptionKey;
        }

        private String getZpkFromAppzoneSwitch()
        {
            //query = string.Format("SELECT [ValueUnderParent] FROM [AppZoneSwitch].[dbo].[Keies] where id = 3");
            string query = ConfigurationManager.GetZpkQuery;
            new PANE.ERRORLOG.Error().LogInfo(string.Format("Inside getZpkFromAppzoneSwitch query - {0}", query));
            string Zpk = "";
            SqlConnection cn = new SqlConnection(ConfigurationManager.AppzoneSwitchConnection);
            SqlCommand cmd = new SqlCommand(query, cn);

            cn.Open();

            using (SqlDataReader reader = cmd.ExecuteReader())
            {
                if (reader.Read())
                {
                    Zpk = reader[0].ToString();
                }
            }
            return Zpk;
        }


        public string GenerateEncryptedPinBlock(string accountNo, string pin)
        {
            string clearPinValue = string.Format("04{0}", pin).PadRight(16, 'F');// "0" + pinLength + pin + "FFFFFFFFFF";
            string clarAcctValue = "0000" + accountNo;
            string pinBlock = ThalesSim.Core.Utility.XORHexStringsFull(clearPinValue, clarAcctValue);

            string encryptionKey = GetEncryptionKey();

            string toReturn = ThalesSim.Core.Cryptography.TripleDES.TripleDESEncrypt(new ThalesSim.Core.Cryptography.HexKey(encryptionKey.Substring(0, 32)), pinBlock);
            return toReturn;
        }

        public byte[] GetPinBlockToPopulateIn52ISO(string accountNo, string pin)
        {
            string encryptedPinBlock = GenerateEncryptedPinBlock(accountNo, pin);

            byte[] encryptedPinBytes = new byte[8];
            if (!string.IsNullOrEmpty(pin))
            {
                ThalesSim.Core.Utility.HexStringToByteArray(encryptedPinBlock, ref encryptedPinBytes);
            }
            return encryptedPinBytes;
        }

        public byte[] GetPinBlockForVISAToPopulateIn52ISO(string pin)
        {
            new PANE.ERRORLOG.Error().LogInfo("GetPinBlockForVISAToPopulateIn52ISO");
            string UDK_A = "00000000" + System.Configuration.ConfigurationManager.AppSettings["UDK_A"];
            new PANE.ERRORLOG.Error().LogInfo(" UDK_A=" + UDK_A);
            string paddedpin = "04" + pin + "FFFFFFFFFF";

            string clearPIN = ThalesSim.Core.Utility.XORHexStringsFull(UDK_A, paddedpin);
            new PANE.ERRORLOG.Error().LogInfo(" clearPIN=" + clearPIN);

            string encryptionKey = GetEncryptionKey();

            string encryptedPinBlock = ThalesSim.Core.Cryptography.TripleDES.TripleDESEncrypt(new ThalesSim.Core.Cryptography.HexKey(encryptionKey.Substring(0, 32)), clearPIN);
            new PANE.ERRORLOG.Error().LogInfo(" encryptedPinBlock=" + encryptedPinBlock);

            byte[] encryptedPinBytes = new byte[8];
            if (!string.IsNullOrEmpty(pin))
            {
                ThalesSim.Core.Utility.HexStringToByteArray(encryptedPinBlock, ref encryptedPinBytes);
            }
            return encryptedPinBytes;
        }

        public byte[] GetPinBlockToPopulateIn52IsoInPinChange(string accountNo, string encryptedPosPinBlock)
        {
            // step a: Decrypt the PIN block from the POS
            string clearPosPinBlock = GetClearPosPinBlock(encryptedPosPinBlock);

            // step b: re-encrypt the clear POS pin block with tpk_zpk
            string encryptedPosPinBlock_tpk = EncryptCearPinBlock(clearPosPinBlock);

            new PANE.ERRORLOG.Error().LogInfo("encrypted pin block " + encryptedPosPinBlock_tpk);

            byte[] encryptedPinBytes = new byte[8];
            ThalesSim.Core.Utility.HexStringToByteArray(encryptedPosPinBlock_tpk, ref encryptedPinBytes);
            return encryptedPinBytes;
        }

        public byte[] GetPinBlockToPopulateIn53ISO(string accountNo, string encryptedPosPinBlock)
        {
            // step a: Decrypt the PIN block from the POS
            string clearPosPinBlock = GetClearPosPinBlock(encryptedPosPinBlock);

            // step b: re-encrypt the clear POS pin block with tpk_zpk
            string encryptedPosPinBlock_tpk = EncryptCearPinBlock(clearPosPinBlock);

            byte[] holder = new byte[8];
            ThalesSim.Core.Utility.HexStringToByteArray(encryptedPosPinBlock_tpk, ref holder);
            byte[] EncryptedPin = new byte[48];
            EncryptedPin[0] = 1;
            holder.CopyTo(EncryptedPin, 1);

            return EncryptedPin;
        }

        public byte[] GetPinBlockForVISAToPopulateIn53ISO(string accountNo, string encryptedPosPinBlock)
        {
            new PANE.ERRORLOG.Error().LogInfo("in GetPinBlockForVISAToPopulateIn53ISO ");
            // step a: Decrypt the PIN block from the POS
            string clearPosPinBlock = GetClearPosPinBlock(encryptedPosPinBlock);
            new PANE.ERRORLOG.Error().LogInfo("clearPosPinBlock =" + clearPosPinBlock);

            //step b: get the pin padded with ffff
            string paddedPin = ThalesSim.Core.Utility.XORHexStringsFull(accountNo.PadLeft(16, '0'), clearPosPinBlock);
            new PANE.ERRORLOG.Error().LogInfo("paddedPin =" + paddedPin);

            string UDK_A = "00000000" + System.Configuration.ConfigurationManager.AppSettings["UDK_A"];

            //step c: get the visa pin block
            string clearPINBLOCK = ThalesSim.Core.Utility.XORHexStringsFull(UDK_A, paddedPin);
            new PANE.ERRORLOG.Error().LogInfo("clearPINBLOCK =" + clearPINBLOCK);

            // step d: re-encrypt the clear POS pin block with tpk_zpk
            string encryptedPosPinBlock_tpk = EncryptCearPinBlock(clearPINBLOCK);

            byte[] holder = new byte[8];
            ThalesSim.Core.Utility.HexStringToByteArray(encryptedPosPinBlock_tpk, ref holder);
            byte[] EncryptedPin = new byte[48];
            EncryptedPin[0] = 1;
            holder.CopyTo(EncryptedPin, 1);

            return EncryptedPin;
        }

        public void Enquiry()
        {

            Engine theFepEngine = new PinIssuance.Net.Bridge.PostBridge.Client.Engine(
                   PinConfigurationManager.FepConfig.BridgeHostIp,
                   PinConfigurationManager.FepConfig.InternalServerPort,
                   new CardAcceptor("1202222", "2222222") { },
                   "trx"
                   );

            theFepEngine.Connect();

            theFepEngine.DoBalanceEnquiry(
                new CardDetails()
                {
                    ExpiryDate = DateTime.ParseExact("4912", "yyMM", DateTimeFormatInfo.InvariantInfo),
                    PAN = "5145851169478915",
                    PIN = GetPinBlockToPopulateIn52ISO("585116947891", "3963"), //pinbytearr,
                                                                                // NewPINBlock = GetPinBlockToPopulateIn53ISO(accountNo) //newEncryptedPinBlock
                },
                        new Bridge.PostBridge.Client.DTO.Account("585116947891", ""), "002"
                        );

        }


        public static string UpdatePinOffsetService(string cardPan, string expiryDate, string pinOffset)
        {
            string result = "false";
            string CardDetailsUrl = System.Configuration.ConfigurationManager.AppSettings["UpdatePinOffsetURL"];

            System.Net.ServicePointManager.ServerCertificateValidationCallback += (se, cert, chain, sslerror) => { return true; };
            var httpWebRequest = (System.Net.HttpWebRequest)System.Net.WebRequest.Create(CardDetailsUrl);
            httpWebRequest.ContentType = "application/json";
            httpWebRequest.Method = "POST";

            using (var streamWriter = new StreamWriter(httpWebRequest.GetRequestStream()))
            {
                PinOffsetRequest cardRequest = new PinOffsetRequest();
                cardRequest.CardPAN = cardPan;
                cardRequest.ExpiryDate = expiryDate;
                cardRequest.PinOffSet = pinOffset;
                UpdatePinOffsetRequest request = new UpdatePinOffsetRequest();
                request.JsonCard = Newtonsoft.Json.JsonConvert.SerializeObject(cardRequest);
                string json = Newtonsoft.Json.JsonConvert.SerializeObject(request);
                streamWriter.Write(json);
                streamWriter.Flush();
                streamWriter.Close();
            }
            System.Net.ServicePointManager.ServerCertificateValidationCallback += (se, cert, chain, sslerror) => { return true; };
            var httpResponse = (System.Net.HttpWebResponse)httpWebRequest.GetResponse();
            using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
            {
                result = streamReader.ReadToEnd();
                new PANE.ERRORLOG.Error().LogInfo(result);
            }
            if (!string.IsNullOrWhiteSpace(result) && result.StartsWith("true"))
            {
                return result;
            }
            else
            {
                throw (new Exception(result));
            }
        }

        public string GenerateTrack2DataForMagstripe(string pan, string expiry, string serviceCode)
        {
            string response = string.Empty;
            ThalesHsm hsm = new ThalesHsm();
            IGeneratePinOffsetResponse pinOffsset = null;
            // ChangePINResponse cpResponse = null;
            
            new PANE.ERRORLOG.Error().LogInfo("In GenerateTrack2DataForMagstripe");
            // obatin the account number
            string accountNo = "";
            try
            {
                accountNo = pan.Substring(pan.Length - 13, 12);
            }
            catch (Exception)
            {
                response = "1:Invalid CardNumber, ensure the card number is minimum of 16 digits";
                return response;
            }

            #region Derive PIN Using the IBM Method

            new PANE.ERRORLOG.Error().LogInfo("In GenerateTrack2DataForMagstripe Step 1");
            // Step 1: Generate a new encrypted Random Pin
            string _encryptedPIN;
            try
            {
                //_encryptedPIN = hsm.PinGenerator().DeriveEncryptedPin(pan).EncryptedPin;
                _encryptedPIN = hsm.PinGenerator().EncryptClearPin("0000",accountNo).EncryptedPin;
                new PANE.ERRORLOG.Error().LogInfo("Derived: " + _encryptedPIN);
            }
            catch (Exception ex)
            {
                new PANE.ERRORLOG.Error().LogToFile(new Exception("Unable to Encrypt clear Pin", ex));
                //Exception ex2 = new ApplicationException("System error");
                //throw ex2;
                response = "1:System PIN Error";
                return response;
            }

            #endregion


            #region Generate PVV 
            new PANE.ERRORLOG.Error().LogInfo("In GenerateTrack2DataForMagstripe Step 2");
            // Step 2: Generate the Pin offset for the random pin  
            try
            {
                //pinOffsset = hsm.PinGenerator().GeneratePVV(_encryptedPIN, accountNo, pan);
                pinOffsset = hsm.PinGenerator().GeneratePinOffset(_encryptedPIN, accountNo, pan);
            }
            catch (Exception ex)
            {
                new PANE.ERRORLOG.Error().LogToFile(new Exception("Unable to Generate the PVV", ex));
                response = "1:System PVV Generation Error";
                return response;
            }

            #endregion
            //return string.Format("0:{0}", pinOffsset.Offset.Substring(0, 4)); ;


            #region Generate CVV 
            IGenerateCVVResponse cvv = hsm.CvvGenerator().GenerateCvv(pan, expiry, PinConfigurationManager.HsmConfig.CardVerificationKey, serviceCode);
            #endregion

            #region Format Track Data
            string discretionaryDataLessCvv = serviceCode + "01" + pinOffsset.Offset.Substring(0, 4);

            string cardHolderName = "CARDHOLDER";
            string track1 = string.Format("B{0}^{1}^{2}{3}{4}", pan, cardHolderName, expiry, discretionaryDataLessCvv, cvv.Cvv);
            string track2 = string.Format("{0}={1}{2}{3}", pan, expiry, discretionaryDataLessCvv, cvv.Cvv);
            #endregion

            new PANE.ERRORLOG.Error().LogInfo(track1);
            new PANE.ERRORLOG.Error().LogInfo(track2);


            return string.Format("0:{0}",track2);
        }
        class UpdatePinOffsetRequest
        {
            public string JsonCard { get; set; }
        }
    }
}
