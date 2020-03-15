using Newtonsoft.Json;
using PinIssuance.Net.Bridge.PostCard;
using PinIssuance.Net.Client.Pos;
using PinIssuance.Net.Client.Pos.Contract;
using PinIssuance.Net.Client.USSD.Request;
using PinSelection.KeyManagement;
using PrimeUtility.BussinessObjects;
using PrimeUtility.Core;
using PrimeUtility.Utility;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;

namespace PinIssuance.Net.Client.USSD
{
    public class PinController
    {
        public string Execute(string theMessage)
        {

            bool usePrimeHSM = Convert.ToBoolean(System.Configuration.ConfigurationManager.AppSettings["UsePrimeHSM"]);
            string response = string.Empty;
            string encryptionIndicator = string.Empty;
            try
            {
                new PANE.ERRORLOG.Error().LogInfo("PinController Web service entered...");
                if (theMessage == null)
                {
                    throw new ApplicationException("Invalid request data. The request data is null");
                }

                if (!(theMessage.ToUpper().StartsWith("C:") || theMessage.ToUpper().StartsWith("E:")))
                {
                    throw new ApplicationException("Invalid request data. The request data is expected to start with C: or E:");
                }

                encryptionIndicator = theMessage.Substring(0, 2);
                theMessage = theMessage.Substring(2);
                if (encryptionIndicator == "E:")
                {
                    theMessage = KeyManager.Rijndael(PrimeUtility.Configuration.ConfigurationManager.UssdConfig.UssdEncryptionKey, theMessage, CryptoMode.DECRYPTION);
                    new PANE.ERRORLOG.Error().LogInfo("Request Data: " + theMessage);
                    if (theMessage == null)
                    {
                        throw new ApplicationException("Invalid request data. Unable to decrypt the data, the expected encryption is Rijndael");
                    }
                }
                // formulate the actual request data
                USSDPinIssuanceRequest theRequest = PosMessageParser.ParseRequestMessage<USSDPinIssuanceRequest>(theMessage);
                IRequest pinRequest = theRequest as USSDPinIssuanceRequest;
                pinRequest.TerminalId = string.Format("USSD{0}", theRequest.CardPAN.Substring(theRequest.CardPAN.Length - 4));
                pinRequest.Function = "pinselection";

                // make the call
                Card theCard = CardUtilities.RetrieveCard(pinRequest.CardPAN, pinRequest.ExpiryDate);
                if (theCard == null) throw new ApplicationException("Invalid card data");
                if (theCard.expiry_date != pinRequest.ExpiryDate) throw new ApplicationException("Invalid expiry date");
                response = new PosMessageProcessor().DoPinSelection(
                    new PinIssuanceRequest()
                    {
                        CardPAN = pinRequest.CardPAN,
                        ExpiryDate = pinRequest.ExpiryDate,
                        Pin = pinRequest.Pin,
                        ConfirmPin = pinRequest.Pin,
                        TerminalId = pinRequest.TerminalId
                    },
                     theCard);
                // pinRequest as PinIssuanceRequest,
            }
            catch (Exception ex)
            {
                response = string.Format(PinConstants.DECLINED_RESPONSE_FORMAT, ex.Message);
                new PANE.ERRORLOG.Error().LogToFile(ex);
            }

            // encrypt the response before responding
            if (encryptionIndicator == "E:")
            {
                try
                {
                    response = KeyManager.Rijndael(PrimeUtility.Configuration.ConfigurationManager.UssdConfig.UssdEncryptionKey, response, CryptoMode.ENCRYPTION);
                }
                catch (Exception ex)
                {
                    new PANE.ERRORLOG.Error().LogToFile(ex);
                }
            }
            response = encryptionIndicator + response;

            return response;
        }

        public string IssuePin(string card, string phoneNumber)
        {

            bool usePrimeHSM = Convert.ToBoolean(System.Configuration.ConfigurationManager.AppSettings["UsePrimeHSM"]);
            string theMessage = "";
            string response = string.Empty;
            string encryptionIndicator = string.Empty;
            IRequest pinRequest = new USSDPinIssuanceRequest();
            try
            {

                new PANE.ERRORLOG.Error().LogInfo("PinController Web service entered...");
                new PANE.ERRORLOG.Error().LogInfo(string.Format("{0} - {1}", card, phoneNumber));
                if (string.IsNullOrWhiteSpace(card))
                {
                    return "1:Card value cannot be null";
                }
                if (string.IsNullOrWhiteSpace(phoneNumber))
                {
                    return "1:Phone cannot be null";
                }

                if (Convert.ToBoolean(System.Configuration.ConfigurationManager.AppSettings["UseDefaultUSSDCardData"]))
                {
                    // theMessage = "C:cardpan=5334771222311096,expiryDate=2204"; //- Sterling
                    // theMessage = "C:cardpan=5399232123065994,expiryDate=1802"; //-FBN
                    //theMessage = "C:cardpan=6280515555555555,expiryDate=2008"; //Keystone
                    theMessage = "C:cardpan=5334775926989586,expiryDate=2009";
                }
                else
                {
                    string expiry = "";
                    string pan = getCardDetails(card, phoneNumber, out expiry);

                    if (string.IsNullOrWhiteSpace(pan) || string.IsNullOrWhiteSpace(expiry))
                    {
                        return "1:An error occurred.";
                    }
                    theMessage = String.Format("C:cardpan={0},expiryDate={1},pin=1234,terminalId=12345678,PinOffset=1234|5399232123033091", pan, expiry);
                    new PANE.ERRORLOG.Error().LogInfo(theMessage);
                    //call prime method to get Pin Issuance Request
                }
                // formulate the actual request data
                USSDPinIssuanceRequest theRequest = PosMessageParser.ParseRequestMessage<USSDPinIssuanceRequest>(theMessage);
                pinRequest = theRequest as USSDPinIssuanceRequest;
                pinRequest.Pin = new Random().Next(1111, 9999).ToString();
                pinRequest.TerminalId = string.Format("USSD{0}", theRequest.CardPAN.Substring(theRequest.CardPAN.Length - 4));
                pinRequest.Function = "pinselection";

                Card theCard = null;

                if (usePrimeHSM)
                {
                    string staticKeyName = System.Configuration.ConfigurationManager.AppSettings["StaticKeyName"];
                    string panEncryptionKey = LiteDAO.GetLocalKey(staticKeyName);
                    string encryptedPan = ThalesSim.Core.Cryptography.TripleDES.TripleDESDecrypt(new ThalesSim.Core.Cryptography.HexKey(panEncryptionKey.Substring(0, 32)), pinRequest.CardPAN);

                    theCard = GetCardDetailsFromService(encryptedPan, pinRequest.ExpiryDate);
                }
                else
                {
                    theCard = CardUtilities.RetrieveCard(pinRequest.CardPAN, pinRequest.ExpiryDate, "pc_cards_1_A");
                }


                if (theCard == null) return "1:Invalid card data";
                if (theCard.expiry_date != pinRequest.ExpiryDate) return "1:Invalid expiry date";
                response = new PosMessageProcessor().DoPinOffsetUpdate(
                    new PinIssuanceRequest()
                    {
                        CardPAN = pinRequest.CardPAN,
                        ExpiryDate = pinRequest.ExpiryDate,
                        Pin = pinRequest.Pin,
                        ConfirmPin = pinRequest.Pin,
                        TerminalId = pinRequest.TerminalId
                    },
                     theCard);
                // pinRequest as PinIssuanceRequest,
            }
            catch (Exception ex)
            {
                response = string.Format("1:{0}", ex.Message);
                new PANE.ERRORLOG.Error().LogToFile(ex);
            }

            return response;
        }

        public static Card GetCardDetailsFromService(string cardPan, string expiryDate)
        {
            string CardDetailsUrl = System.Configuration.ConfigurationManager.AppSettings["CardDetailsURL"];

            System.Net.ServicePointManager.ServerCertificateValidationCallback += (se, cert, chain, sslerror) => { return true; };
            var httpWebRequest = (HttpWebRequest)WebRequest.Create(CardDetailsUrl);
            httpWebRequest.ContentType = "application/json";
            httpWebRequest.Method = "POST";

            using (var streamWriter = new StreamWriter(httpWebRequest.GetRequestStream()))
            {
                CardDetailsRequest cardRequest = new CardDetailsRequest();
                cardRequest.CardPAN = cardPan;
                cardRequest.ExpiryDate = expiryDate;
                cardRequest.Username = "PrimeTest";

                GetCardRequest request = new GetCardRequest();
                request.JsonCard = Newtonsoft.Json.JsonConvert.SerializeObject(cardRequest);
                string json = Newtonsoft.Json.JsonConvert.SerializeObject(request);
                streamWriter.Write(json);
                streamWriter.Flush();
                streamWriter.Close();
            }

            var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();
            using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
            {
                string result = streamReader.ReadToEnd();
                new PANE.ERRORLOG.Error().LogInfo(result);
                //  if(!string.IsNullOrEmpty(result) 
            }
            return null;
        }

        public string ActivateCard(string card, string phoneNumber)
        {

            bool usePrimeHSM = Convert.ToBoolean(System.Configuration.ConfigurationManager.AppSettings["UsePrimeHSM"]);
            string theMessage = "";
            string response = string.Empty;
            string encryptionIndicator = string.Empty;
            try
            {
                new PANE.ERRORLOG.Error().LogInfo("PinController Web service entered...");
                if (string.IsNullOrWhiteSpace(card))
                {
                    return "1:Card value cannot be null";
                }
                if (string.IsNullOrWhiteSpace(phoneNumber))
                {
                    return "1:Phone cannot be null";
                }

                if (Convert.ToBoolean("false"))
                {
                    theMessage = "C:cardpan=5334771222311096,expiryDate=2204,pin=1234,terminalId=12345678,PinOffset=1234|5399232123033091"; // -Sterling

                    //theMessage = "C:cardpan=5399233021500835,expiryDate=1907,pin=1234,terminalId=12345678,PinOffset=1234|5399232123033091"; // -FBN
                }
                else
                {

                    string expiry = "";
                    string pan = getCardDetails(card, phoneNumber, out expiry);

                    if (string.IsNullOrWhiteSpace(pan) || string.IsNullOrWhiteSpace(expiry))
                    {
                        return "1:No such card tied to customer";
                    }
                    theMessage = String.Format("C:cardpan={0},expiryDate={1},pin=1234,terminalId=12345678,PinOffset=1234|5399232123033091", pan, expiry);
                    new PANE.ERRORLOG.Error().LogInfo(theMessage);
                    //call prime method to get Pin Issuance Request
                }
                // formulate the actual request data


                USSDPinIssuanceRequest theRequest = PosMessageParser.ParseRequestMessage<USSDPinIssuanceRequest>(theMessage.Split('|')[0]);
                string oldPan = theMessage.Split('|')[1];
                IRequest pinRequest = theRequest as USSDPinIssuanceRequest;
                pinRequest.TerminalId = string.Format("USSD{0}", theRequest.CardPAN.Substring(theRequest.CardPAN.Length - 4));
                pinRequest.Function = "pinselection";

                Card theCard = null;
                string pinoffset = string.Empty;

                if (usePrimeHSM)
                {
                    theCard = CardUtilities.RetrieveCard(pinRequest.CardPAN, pinRequest.ExpiryDate, "pc_cards_1_A");
                    pinoffset = CardUtilities.GetCardPinOffset(oldPan, "pc_cards_1_A");
                }
                else
                {
                    theCard = CardUtilities.RetrieveCard(pinRequest.CardPAN, pinRequest.ExpiryDate, "pc_cards_1_A");
                    pinoffset = CardUtilities.GetCardPinOffset(oldPan, "pc_cards_1_A");
                }
                if (theCard == null) return ("1:Invalid card data");
                if (pinoffset == null) return ("1:Pinoffset of existing card is null");
                if (theCard.expiry_date != pinRequest.ExpiryDate) return ("1:Invalid expiry date");
                response = new PosMessageProcessor().DoCardActivation(
                    new PinIssuanceRequest()
                    {
                        CardPAN = pinRequest.CardPAN,
                        ExpiryDate = pinRequest.ExpiryDate,
                        Pin = pinRequest.Pin,
                        ConfirmPin = pinRequest.Pin,
                        TerminalId = pinRequest.TerminalId,
                        // Track2 = "5399233021500835D1907221011408619F",
                        // IccData = "<IccRequest><AmountAuthorized>000000000000</AmountAuthorized><AmountOther>000000000000</AmountOther><ApplicationInterchangeProfile>3800</ApplicationInterchangeProfile><ApplicationTransactionCounter>006C</ApplicationTransactionCounter><Cryptogram>E7ADCDEFBDE1A846</Cryptogram><CryptogramInformationData>80</CryptogramInformationData><CvmResults>420300</CvmResults><IssuerApplicationData>0110A0800322000062E300000000000000FF</IssuerApplicationData><TerminalCapabilities>E040E0</TerminalCapabilities><TerminalCountryCode>566</TerminalCountryCode><TerminalType>12</TerminalType><TerminalVerificationResult>0080048000</TerminalVerificationResult><TransactionCurrencyCode>566</TransactionCurrencyCode><TransactionDate>170707</TransactionDate><TransactionType>92</TransactionType><UnpredictableNumber>B4072947</UnpredictableNumber></IccRequest>"
                    },
                    theCard, pinoffset, oldPan);
                // pinRequest as PinIssuanceRequest,
            }
            catch (Exception ex)
            {
                response = string.Format("1:{0}", ex.Message);
                new PANE.ERRORLOG.Error().LogToFile(ex);
            }

            return response;
        }


        public string getCardDetails(string panComponent, string phone, out string expiry)
        {
            new PANE.ERRORLOG.Error().LogInfo("Inside get Card details");

            expiry = "";
            string clearPan = "";
            try
            {
                string staticKeyName = System.Configuration.ConfigurationManager.AppSettings["StaticKeyName"];
                string pageApiUrl = Convert.ToString(ConfigurationManager.AppSettings["PageApiUrl"]);
                string relativePath = "api/Flow?flowID=61f63e11-2885-409c-b0fc-6920f73b5774&awaitResponse=True&timeOut=30&isWeb=False&userName=3_31&ic=31&isClientFunc=False&hfService=4&FunctionID="; //Sterling

                //"api/Flow?flowID=61f63e11-2885-409c-b0fc-6920f73b5774&awaitResponse=True&timeOut=30&isWeb=False&userName=3_26&ic=26&isClientFunc=False&hfService=4&FunctionID=";
                //FBN
                //

                new PANE.ERRORLOG.Error().LogInfo(string.Format("{0}{1}", pageApiUrl, relativePath));

                Dictionary<string, object> result = null;

                using (HttpClient client = new HttpClient())
                {
                    client.BaseAddress = new Uri(pageApiUrl);
                    client.DefaultRequestHeaders.Accept.Clear();
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));


                    ServicePointManager.ServerCertificateValidationCallback += (sender, cert, chain, sslPolicyErrors) => true;

                    IDictionary<string, object> requestData = new Dictionary<string, object>();

                    requestData.Add("PhoneNumber", phone);

                    requestData.Add("PanComponent", panComponent);

                    var response = client.PostAsJsonAsync(relativePath, requestData).Result;
                    if (response != null)
                    {
                        new PANE.ERRORLOG.Error().LogInfo(response.ToString());
                    }
                    if (response.IsSuccessStatusCode)
                    {

                        new PANE.ERRORLOG.Error().LogInfo("Response is successfull");
                        object obj;
                        object encryptedPanObj;
                        object expiryDateObj;
                        result = response.Content.ReadAsAsync<Dictionary<string, object>>().Result;


                        result.TryGetValue("CommandFields", out obj);

                        var data = JsonConvert.DeserializeObject<Dictionary<string, object>>(Convert.ToString(obj));
                        var sb = new StringBuilder();
                        sb.Append(string.Format("{0}", Convert.ToString(obj)));
                        new PANE.ERRORLOG.Error().LogInfo(sb.ToString());
                        data.TryGetValue("EncryptedPan", out encryptedPanObj);
                        data.TryGetValue("ExpiryDate", out expiryDateObj);

                        if (encryptedPanObj != null && expiryDateObj != null)
                        {
                            new PANE.ERRORLOG.Error().LogInfo("EncryptedPan is not null");
                            var sb1 = new StringBuilder();
                            sb1.Append(string.Format("{0}", Convert.ToString(encryptedPanObj)));
                            var sb2 = new StringBuilder();
                            sb2.Append(string.Format("{0}", Convert.ToString(expiryDateObj)));
                            expiry = sb2.ToString();
                            string encryptedPan = sb1.ToString();
                            new PANE.ERRORLOG.Error().LogInfo("Encrypted Pan : " + encryptedPan + ", Expiry : " + expiry);
                            expiry = String.Format("{0}{1}", expiry.Split('/')[1], expiry.Split('/')[0]);
                            string panEncryptionKey = LiteDAO.GetLocalKey(staticKeyName);
                            clearPan = ThalesSim.Core.Cryptography.TripleDES.TripleDESDecrypt(new ThalesSim.Core.Cryptography.HexKey(panEncryptionKey.Substring(0, 32)), encryptedPan);
                        }

                    }
                }
            }
            catch (Exception ex)
            {
                new PANE.ERRORLOG.Error().LogToFile(ex);
            }

            return clearPan;
        }
    }
    class GetCardRequest
    {
        public string JsonCard { get; set; }
    }
}
