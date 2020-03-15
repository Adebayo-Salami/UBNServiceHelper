
using PrimeUtility.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ThalesSim.Core.Message.XML;

namespace PinIssuance.Net.Bridge.HSM
{
    public class ThalesHsm : IHsm
    {
        //private const string PIN_VALIDATION_DATA = "AB345CDF9E10";
        //private const string DECIMALIZED_TABLE = "2345678197894354";
        protected ThalesSim.Core.TCP.WorkerClient _thales;
        protected string _thalesData = string.Empty;
        private int _headerLength = 8;
        private int _encryptedPinLength = 5;
        private string _hostname;
        private int _port;
        private string _pinVerificationKey = string.Empty;
        private string _decimalisationTable = string.Empty;
        private string _pinValidationData = string.Empty;

        public ThalesHsm()
        {
            _pinVerificationKey = PinConfigurationManager.HsmConfig.PinVerificationKey;
            _decimalisationTable = PinConfigurationManager.HsmConfig.DecimalisationTable;
            _pinValidationData = PinConfigurationManager.HsmConfig.PinValidationData;
            if (!string.IsNullOrEmpty(PinConfigurationManager.HsmConfig.EncryptedPinLength))
            {
                _encryptedPinLength = Convert.ToInt32(PinConfigurationManager.HsmConfig.EncryptedPinLength);
            }
        }

        #region IHsm Members

        public void Setup(string hostname,
            int port,
            int? headerLength,
            int? encryptedPinLength)
        {
            if (headerLength.HasValue) _headerLength = headerLength.Value;
            if (encryptedPinLength.HasValue) _encryptedPinLength = encryptedPinLength.Value;
            _port = port;
            _hostname = hostname;
        }

        private string MessageHeader
        {
            get
            {
                return "".PadLeft(_headerLength, '0');
            }
        }

        private void thales_MessageArrived(ThalesSim.Core.TCP.WorkerClient sender, ref byte[] b, int len)
        {
            string s = string.Empty;

            for (int i = 0; i < len; i++)
            {
                s = s + Convert.ToChar(b[i]);
            }

            this._thalesData = s;
        }

        private string SendFunctionCommand(string s)
        {
            //new PANE.ERRORLOG.Error().LogInfo("message to hsm :" + s);
            _thalesData = "";
            this._thales.send(s);

            while (_thalesData == string.Empty && this._thales.IsConnected)
            {
                System.Threading.Thread.Sleep(1);
            }

            if (!this._thales.IsConnected)
            {
                return string.Empty;
            }
            else
            {
                return _thalesData;
            }
        }

        public string SendProxyCommand(string proxyCommand)
        {
            this.Connect();
            string reply = string.Empty;
            try
            {
                reply = SendFunctionCommand(proxyCommand);
            }
            catch
            {
                throw;
            }
            finally
            {
                this.Close();
            }
            return reply;
        }

        private ThalesSim.Core.Message.XML.MessageKeyValuePairs Send(string command, List<string> parameters, ThalesSim.Core.Message.XML.MessageFields responseFormat)
        {
            //Build Function Command
            StringBuilder commandBuilder = new StringBuilder(string.Format("{0}{1}", this.MessageHeader, command));
            foreach (string parameter in parameters)
            {
                commandBuilder.Append(parameter);
            }

            this.Connect();
            string reply = string.Empty;
            try
            {
                reply = SendFunctionCommand(commandBuilder.ToString());
            }
            catch
            {
                throw;
            }
            finally
            {
                this.Close();
            }

            if (string.IsNullOrEmpty(reply)) throw new ApplicationException("No reply from HSM");

            int respPosition = _headerLength + 2;
            string responseCode = reply.Substring(respPosition, 2);

            //string[] fbres = { "00", "01" };
            //if (reply.ToLower().Contains("fb") && !fbres.Contains(responseCode))
            //{
            //    throw new ApplicationException(responseCode);
            //}

            if (responseCode != "00")
            {
                if(responseCode == "02")
                {
                    //new PANE.ERRORLOG.Error().LogInfo("HSM response 02: Key inappropraite length for algorithm");
                }
                else if (reply.ToLower().Contains("fb") && reply.Length < respPosition + 2)  //if the reply is greater than the header+responsecode then it is fine
                {
                    throw new ApplicationException(responseCode);
                }
                else if (!reply.ToLower().Contains("fb"))
                {
                    throw new ApplicationException(responseCode);
                }
            }

            var msg = new ThalesSim.Core.Message.Message(reply);
            //Build Response Format
            responseFormat.Fields.InsertRange(0, new List<MessageField>()
            {
                new ThalesSim.Core.Message.XML.MessageField(){ Name="messageHeader", Length=_headerLength },
                new ThalesSim.Core.Message.XML.MessageField(){ Name="command", Length=2 },
                new ThalesSim.Core.Message.XML.MessageField(){ Name="response", Length=2 }
            });

            ThalesSim.Core.Message.XML.MessageKeyValuePairs results = new ThalesSim.Core.Message.XML.MessageKeyValuePairs();
            string result = null;
            ThalesSim.Core.Message.XML.MessageParser.Parse(msg, responseFormat, ref results, ref result);
            return results;
        }




        public IZonePinEncryptionKey ZonePinKeyManager()
        {
            return new ZonePinEncryptionKey(this);
        }

        public IPinTranslation PinTranslator()
        {
            return new PinTranslation(this);
        }

        public IPinVerification PinVerifier()
        {
            return new PinVerification(this);
        }

        public IPinGeneration PinGenerator()
        {
            return new PinGeneration(this);
        }

        public ICvvGeneration CvvGenerator()
        {
            return new CvvGeneration(this);
        }

        public void Connect()
        {
            this.Setup(PinConfigurationManager.HsmConfig.HsmHost, PinConfigurationManager.HsmConfig.HsmPort, null, null);
            this._thales = new ThalesSim.Core.TCP.WorkerClient(new System.Net.Sockets.TcpClient(this._hostname, this._port));
            this._thales.MessageArrived += new ThalesSim.Core.TCP.WorkerClient.MessageArrivedEventHandler(thales_MessageArrived);
            this._thales.InitOps();
        }

        public void Close()
        {
            _thales.TermClient();
        }

        #endregion

        #region Requests

        public class ZonePinEncryptionKey : IZonePinEncryptionKey
        {
            private ThalesHsm _theHsm;

            public ZonePinEncryptionKey(ThalesHsm theHsm)
            {
                _theHsm = theHsm;
            }

            #region IZonePinEncryptionKey Members

            public IGeneratePinEncryptionKeyResponse Generate(string exchangeKey, KeyScheme? exchangeKeyScheme, KeyScheme? storageKeyScheme)
            {
                int keyLength = exchangeKey.Length;

                //configure Parameters
                var parameters = new List<string>()
                 {
                     exchangeKey,

                 };

                if (exchangeKeyScheme.HasValue || storageKeyScheme.HasValue)
                {
                    parameters.Add(";");
                    parameters.Add(exchangeKeyScheme.HasValue ? ((char)exchangeKeyScheme.Value).ToString() : ((char)storageKeyScheme.Value).ToString());
                    parameters.Add(storageKeyScheme.HasValue ? ((char)storageKeyScheme.Value).ToString() : ((char)exchangeKeyScheme.Value).ToString());
                    parameters.Add("1");
                }

                //configure Response Format
                var responseFormat = new ThalesSim.Core.Message.XML.MessageFields();
                responseFormat.Fields.AddRange(new List<MessageField>()
                {
                    new MessageField(){ Name="zmk", Length=exchangeKey.Length },
                new MessageField(){ Name="lmk", Length=exchangeKey.Length },
                new MessageField(){ Name="checkValue", Length=6 }
            });
                var hsmResponse = _theHsm.Send("IA", parameters, responseFormat);

                IGeneratePinEncryptionKeyResponse response = new GeneratePinEncryptionKeyResponse();
                response.PinEncryptionKeyUnderExchangeKey = hsmResponse.Item("zmk");
                response.PinEncryptionKeyUnderStorageKey = hsmResponse.Item("lmk");
                response.CheckValue = hsmResponse.Item("checkValue");

                return response;
            }

            public ITranslatePinEncryptionKeyResponse TranslateFromExchangeKeyToStorageKey(string exchangeKey, string pinEncryptionKey, KeyScheme? exchangeKeyScheme, KeyScheme? storageKeyScheme)
            {
                //configure Parameters
                var parameters = new List<string>()
                 {
                    exchangeKey,
                     pinEncryptionKey
                 };

                if (exchangeKeyScheme.HasValue || storageKeyScheme.HasValue)
                {
                    parameters.Add(";");
                    parameters.Add(exchangeKeyScheme.HasValue ? ((char)exchangeKeyScheme.Value).ToString() : ((char)storageKeyScheme.Value).ToString());
                    parameters.Add(storageKeyScheme.HasValue ? ((char)storageKeyScheme.Value).ToString() : ((char)exchangeKeyScheme.Value).ToString());
                    parameters.Add("1");
                }

                //configure Response Format
                var responseFormat = new MessageFields();
                responseFormat.Fields.AddRange(new List<MessageField>()
                {
                    new MessageField(){Name="zpk_lmk", Length =pinEncryptionKey.Length},
                    new MessageField(){Name="checkValue", Length =6}
                });

                var hsmResponse = _theHsm.Send("FA", parameters, responseFormat);

                ITranslatePinEncryptionKeyResponse response = new TranslatePinEncryptionKeyResponse();
                response.TranslatedPinEncryptionKey = hsmResponse.Item("zpk_lmk");
                response.KeyCheckValue = hsmResponse.Item("checkValue");

                return response;
            }

            public ITranslatePinEncryptionKeyResponse TranslateFromStorageKeyToExchangeKey(string exchangeKey, string pinEncryptionKey, KeyScheme? exchangeKeyScheme, KeyScheme? storageKeyScheme)
            {
                throw new NotImplementedException();
            }

            #endregion
        }

        public class PinTranslation : IPinTranslation
        {
            private ThalesHsm _theHsm;

            public PinTranslation(ThalesHsm theHsm)
            {
                _theHsm = theHsm;
            }

            #region IPinTranslation Members

            public IPinTranslationResponse TranslateFromPinEncryptionKeyToAnother(bool isInterchange, string sourcePinEncryptionKey, string destinationPinEncryptionKey, int maxPinLength, string pinBlock, PinBlockFormats sourcePinBlockFormat, PinBlockFormats destinationPinBlockFormat, string accountNumber)
            {
                throw new NotImplementedException();
            }
            public IPinGenerationResponse TranslatePinFromBdkToZpkEncryption(string bdk, string zpk, string keySerialNumber, string pinBlock, string accountNumber)
            {
                if (string.IsNullOrEmpty(bdk)) throw new ArgumentNullException("bdk");
                if (string.IsNullOrEmpty(zpk)) throw new ArgumentNullException("zpk");
                if (string.IsNullOrEmpty(keySerialNumber)) throw new ArgumentNullException("keySerialNumber");
                if (string.IsNullOrEmpty(accountNumber)) throw new ArgumentNullException("accountNumber");
                if (accountNumber.Length > 18) throw new ArgumentException("accountNumber is longer than 12-digits");
                string keyDescriptor = "605"; //"002";

                //configure Parameters
                var parameters = new List<string>()
                 {
                     bdk,
                     zpk,
                     keyDescriptor,
                     keySerialNumber,
                     pinBlock,
                     ((int)PinBlockFormats.ANSI).ToString().PadLeft(2,'0'),
                     ((int)PinBlockFormats.ANSI).ToString().PadLeft(2,'0'),
                     accountNumber.PadLeft(12,'0')
                 };

                //configure Response Format
                var responseFormat = new MessageFields();
                responseFormat.Fields.Add(new MessageField() { Name = "encryptedPin", Length = 18 });// the first digit should not be used   // _theHsm._encryptedPinLength });

                var hsmResponse = _theHsm.Send("G0", parameters, responseFormat);
                IPinGenerationResponse response = new PinGeneratorResponse();
                response.EncryptedPin = hsmResponse.Item("encryptedPin").Substring(2);

                return response;
            }
            public IPinGenerationResponse TranslateFromPinEncryptionKeyToStorageKey(bool isInterchange, string pinEncryptionKey, string pinBlock, PinBlockFormats pinBlockFormat, string accountNumber)
            {
                if (string.IsNullOrEmpty(pinEncryptionKey)) throw new ArgumentNullException("pinEncryptionKey");
                if (string.IsNullOrEmpty(pinBlock)) throw new ArgumentNullException("pinBlock");
                if (string.IsNullOrEmpty(accountNumber)) throw new ArgumentNullException("accountNumber");
                if (accountNumber.Length > 12) throw new ArgumentException("accountNumber is longer than 12-digits");

                //configure Parameters
                var parameters = new List<string>()
                 {
                     pinEncryptionKey,
                     pinBlock,
                     ((int)pinBlockFormat).ToString().PadLeft(2,'0'),
                     accountNumber.PadLeft(12,'0')
                 };

                //configure Response Format
                var responseFormat = new MessageFields();
                responseFormat.Fields.Add(new MessageField() { Name = "encryptedPin", Length = _theHsm._encryptedPinLength });

                var hsmResponse = _theHsm.Send(isInterchange ? "JE" : "JC", parameters, responseFormat);
                IPinGenerationResponse response = new PinGeneratorResponse();
                response.EncryptedPin = hsmResponse.Item("encryptedPin");

                return response;
            }


            public IPinGenerationResponse TranslatePinFromLMKToZPKEncryption(string zpk, string EncryptionPin, PinBlockFormats pinBlockFormat, string accountNumber)
            {
                if (string.IsNullOrEmpty(EncryptionPin)) throw new ArgumentNullException("pinEncryptionKey");
                if (string.IsNullOrEmpty(accountNumber)) throw new ArgumentNullException("accountNumber");
                if (string.IsNullOrEmpty(zpk)) throw new ArgumentNullException("zpk is empty");

                if (accountNumber.Length > 12) throw new ArgumentException("accountNumber is longer than 12-digits");

                //configure Parameters
                var parameters = new List<string>()
                 {
                     zpk,
                     ((int)pinBlockFormat).ToString().PadLeft(2,'0'),
                     accountNumber.PadLeft(12,'0'),
                     EncryptionPin
                 };

                //configure Response Format
                var responseFormat = new MessageFields();
                responseFormat.Fields.Add(new MessageField() { Name = "encryptedPin", Length = 8 });// _theHsm._encryptedPinLength });

                var hsmResponse = _theHsm.Send("JG", parameters, responseFormat);
                IPinGenerationResponse response = new PinGeneratorResponse();
                response.EncryptedPin = hsmResponse.Item("encryptedPin");

                return response;
            }

            public IPinGenerationResponse TranslatePinFromEncryptionUnderInterchangeKeyToLMKEncryption(string zpk, string EncryptedPin, PinBlockFormats pinBlockFormat, string accountNumber)
            {
                if (string.IsNullOrEmpty(EncryptedPin)) throw new ArgumentNullException("pinEncryptionKey");
                if (string.IsNullOrEmpty(accountNumber)) throw new ArgumentNullException("accountNumber");
                if (string.IsNullOrEmpty(zpk)) throw new ArgumentNullException("zpk is empty");

                if (accountNumber.Length > 12) throw new ArgumentException("accountNumber is longer than 12-digits");

                //configure Parameters
                var parameters = new List<string>()
                 {
                     zpk,
                     EncryptedPin,
                     ((int)pinBlockFormat).ToString().PadLeft(2,'0'),
                     accountNumber.PadLeft(12,'0'),
                 };

                //configure Response Format
                var responseFormat = new MessageFields();
                responseFormat.Fields.Add(new MessageField() { Name = "encryptedPin", Length = _theHsm._encryptedPinLength });

                var hsmResponse = _theHsm.Send("JE", parameters, responseFormat);
                IPinGenerationResponse response = new PinGeneratorResponse();
                response.EncryptedPin = hsmResponse.Item("encryptedPin");

                return response;
            }


            public IPinGenerationResponse TranslatePinFromOneZPKEncryptionToAnother(string SourceZPK, string DestinationZPK, string SourcePinBlock, PinBlockFormats pinBlockFormat, string accountNumber)
            {
                if (string.IsNullOrEmpty(SourceZPK)) throw new ArgumentNullException("pinEncryptionKey");
                if (string.IsNullOrEmpty(accountNumber)) throw new ArgumentNullException("accountNumber");
                if (string.IsNullOrEmpty(DestinationZPK)) throw new ArgumentNullException("zpk is empty");
                if (string.IsNullOrEmpty(SourcePinBlock)) throw new ArgumentNullException("Source Pin Block is empty");

                if (accountNumber.Length > 12) throw new ArgumentException("accountNumber is longer than 12-digits");

                //configure Parameters
                var parameters = new List<string>()
                 {
                     SourceZPK,
                     DestinationZPK,
                     "12",
                     SourcePinBlock,
                     ((int)pinBlockFormat).ToString().PadLeft(2,'0'),
                     ((int)pinBlockFormat).ToString().PadLeft(2,'0'),
                     accountNumber.PadLeft(12,'0'),
                 };

                //configure Response Format
                var responseFormat = new MessageFields();
                responseFormat.Fields.Add(new MessageField() { Name = "encryptedPin", Length = 18 });

                var hsmResponse = _theHsm.Send("CC", parameters, responseFormat);
                IPinGenerationResponse response = new PinGeneratorResponse();
                response.EncryptedPin = hsmResponse.Item("encryptedPin").Substring(0, 16);

                return response;
            }


            public string DecryptAnEncrypted(string EncryptedPin, string accountNumber)
            {
                if (string.IsNullOrEmpty(EncryptedPin)) throw new ArgumentNullException("pinEncryptionKey");
                if (string.IsNullOrEmpty(accountNumber)) throw new ArgumentNullException("accountNumber");

                if (accountNumber.Length > 12) throw new ArgumentException("accountNumber is longer than 12-digits");

                //configure Parameters
                var parameters = new List<string>()
                 {
                     accountNumber.PadLeft(12,'0'),
                     EncryptedPin
                 };

                //configure Response Format
                var responseFormat = new MessageFields();
                responseFormat.Fields.Add(new MessageField() { Name = "Pin", Length = 4 });// _theHsm._encryptedPinLength });

                var hsmResponse = _theHsm.Send("NG", parameters, responseFormat);
                string response = hsmResponse.Item("Pin");

                return response;
            }

            public string TranslateDecimalizationTableFromOldToNewLMK()
            {

                var parameters = new List<string>()
                 {
                   PinConfigurationManager.HsmConfig.DecimalisationTable
                 };

                //configure Response Format
                var responseFormat = new MessageFields();
                responseFormat.Fields.Add(new MessageField() { Name = "decimalisationtable", Length = 16 });

                var hsmResponse = _theHsm.Send("LO", parameters, responseFormat);
                string response = hsmResponse.Item("decimalisationtable");

                return response;
            }
            #endregion



        }

        public class PinGeneration : IPinGeneration
        {
            private ThalesHsm _theHsm;

            public PinGeneration(ThalesHsm theHsm)
            {
                _theHsm = theHsm;
            }

            #region IPinGenerator Members

            public IPinGenerationResponse GenerateRandomPin(string accountNumber, int? pinLength)
            {
                if (string.IsNullOrEmpty(accountNumber)) throw new ArgumentNullException("accountNumber");
                if (accountNumber.Length > 12) throw new ArgumentException("accountNumber is longer than 12-digits");

                //configure Parameters
                var parameters = new List<string>()
                 {
                     accountNumber.PadLeft(12,'0')
                 };

                if (pinLength.HasValue)
                {
                    parameters.Add(pinLength.Value.ToString().PadLeft(2, '0'));
                }

                //configure Response Format
                var responseFormat = new MessageFields();
                responseFormat.Fields.Add(new MessageField() { Name = "encryptedPin", Length = _theHsm._encryptedPinLength });

                var hsmResponse = _theHsm.Send("JA", parameters, responseFormat);

                IPinGenerationResponse response = new PinGeneratorResponse();
                response.EncryptedPin = hsmResponse.Item("encryptedPin");

                return response;
            }

            public IPinGenerationResponse EncryptClearPin(string clearPin, string accountNumber)
            {
                if (string.IsNullOrEmpty(clearPin)) throw new ArgumentNullException("clearPin");
                if (string.IsNullOrEmpty(accountNumber)) throw new ArgumentNullException("accountNumber");
                if (accountNumber.Length > 12) throw new ArgumentException("accountNumber is longer than 12-digits");

                //configure Parameters
                var parameters = new List<string>()
                 {
                     clearPin.PadRight(_theHsm._encryptedPinLength, 'F'),
                     accountNumber.PadLeft(12,'0')
                 };

                //configure Response Format
                var responseFormat = new MessageFields();
                responseFormat.Fields.Add(new MessageField() { Name = "encryptedPin", Length = _theHsm._encryptedPinLength });

                var hsmResponse = _theHsm.Send("BA", parameters, responseFormat);

                IPinGenerationResponse response = new PinGeneratorResponse();
                response.EncryptedPin = hsmResponse.Item("encryptedPin");

                return response;
            }
            public IPinGenerationResponse DeriveEncryptedPin(string pan)
            {
                if (string.IsNullOrEmpty(pan)) throw new ArgumentNullException("accountNumber");
                if (pan.Length < 16) throw new ArgumentException("accountNumber is longer than 12-digits");
                string accountNumber = pan.Substring(pan.Length - 13, 12);

                //configure Parameters
                var parameters = new List<string>()
                 {
                     PinConfigurationManager.HsmConfig.PinVerificationKeyPVV,
                     "0000FFFFFFFF", //Default Pin offset 
                     "04", //minPinLength.ToString().PadLeft(2,'0'),
                     accountNumber.PadLeft(12,'0'),
                     PinConfigurationManager.HsmConfig.DecimalisationTable,
                     "P",//PinConfigurationManager.HsmConfig.PinValidationData
                     pan.Length == 19 ? pan.PadRight(20,'F') : pan
                 };

                //configure Response Format
                var responseFormat = new MessageFields();
                responseFormat.Fields.Add(new MessageField() { Name = "encryptedPin", Length = _theHsm._encryptedPinLength });

                var hsmResponse = _theHsm.Send("EE", parameters, responseFormat);

                IPinGenerationResponse response = new PinGeneratorResponse();
                response.EncryptedPin = hsmResponse.Item("encryptedPin");

                return response;
            }
            public IGeneratePinOffsetResponse GeneratePinOffset(string encryptedPin, string accountNumber)
            {
                if (string.IsNullOrEmpty(encryptedPin)) throw new ArgumentNullException("encryptedPin");
                if (string.IsNullOrEmpty(accountNumber)) throw new ArgumentNullException("accountNumber");
                if (accountNumber.Length > 12) throw new ArgumentException("accountNumber is longer than 12-digits");

                //configure Parameters
                var parameters = new List<string>()
                 {
                     PinConfigurationManager.HsmConfig.PinVerificationKey,
                     encryptedPin,
                     "04", //minPinLength.ToString().PadLeft(2,'0'),
                     accountNumber.PadLeft(12,'0'),
                     PinConfigurationManager.HsmConfig.DecimalisationTable,
                     "5145851169N5"//PinConfigurationManager.HsmConfig.PinValidationData
                 };

                //configure Response Format
                var responseFormat = new MessageFields();
                responseFormat.Fields.Add(new MessageField() { Name = "offset", Length = 12 });

                var hsmResponse = _theHsm.Send("DE", parameters, responseFormat);

                IGeneratePinOffsetResponse response = new GeneratePinOffsetResponse();
                response.Offset = hsmResponse.Item("offset");

                return response;
            }
            public IGeneratePinOffsetResponse GeneratePinOffset(string encryptedPin, string accountNumber, string cardPan)
            {
                if (string.IsNullOrEmpty(encryptedPin)) throw new ArgumentNullException("encryptedPin");
                if (string.IsNullOrEmpty(accountNumber)) throw new ArgumentNullException("accountNumber");
                if (accountNumber.Length > 12) throw new ArgumentException("accountNumber is longer than 12-digits");
                //string pinValidationData = cardPan.Substring(0, cardPan.Length - 6); //AHMED
                string pinValidationData = cardPan.Substring(0, 10);
                pinValidationData = string.Format("{0}N{1}", pinValidationData, cardPan.Last());
                //configure Parameters
                var parameters = new List<string>()
                 {
                     PinConfigurationManager.HsmConfig.PinVerificationKey,
                     encryptedPin,
                     "04", //minPinLength.ToString().PadLeft(2,'0'),
                     accountNumber.PadLeft(12,'0'),
                     PinConfigurationManager.HsmConfig.DecimalisationTable,
                     pinValidationData
                 };

                //configure Response Format
                var responseFormat = new MessageFields();
                responseFormat.Fields.Add(new MessageField() { Name = "offset", Length = 12 });

                var hsmResponse = _theHsm.Send("DE", parameters, responseFormat);

                IGeneratePinOffsetResponse response = new GeneratePinOffsetResponse();
                response.Offset = hsmResponse.Item("offset");

                return response;
            }

            public IGeneratePinOffsetResponse GenerateVISAPinOffset(string encryptedPin, string accountNumber, string cardPan)
            {
                if (string.IsNullOrEmpty(encryptedPin)) throw new ArgumentNullException("encryptedPin");
                if (string.IsNullOrEmpty(accountNumber)) throw new ArgumentNullException("accountNumber");
                if (accountNumber.Length > 12) throw new ArgumentException("accountNumber is longer than 12-digits");
                //string pinValidationData = cardPan.Substring(0, cardPan.Length - 6); //AHMED
                string pinValidationData = cardPan.Substring(0, 10);
                pinValidationData = string.Format("{0}N{1}", pinValidationData, cardPan.Last());
                //configure Parameters
                var parameters = new List<string>()
                 {
                     PinConfigurationManager.HsmConfig.PinVerificationKeyVISA,
                     encryptedPin,
                     accountNumber.PadLeft(12,'0'),
                     "1", //minPinLength.ToString().PadLeft(2,'0'),
                     
                    // PinConfigurationManager.HsmConfig.DecimalisationTable,
                    // pinValidationData
                 };

                //configure Response Format
                var responseFormat = new MessageFields();
                responseFormat.Fields.Add(new MessageField() { Name = "offset", Length = 4 });

                var hsmResponse = _theHsm.Send("DG", parameters, responseFormat);

                IGeneratePinOffsetResponse response = new GeneratePinOffsetResponse();
                response.Offset = hsmResponse.Item("offset");

                return response;
            }
            public IGeneratePinOffsetResponse GeneratePVV(string encryptedPin, string accountNumber, string cardPan)
            {
                if (string.IsNullOrEmpty(encryptedPin)) throw new ArgumentNullException("encryptedPin");
                if (string.IsNullOrEmpty(accountNumber)) throw new ArgumentNullException("accountNumber");
                if (accountNumber.Length > 12) throw new ArgumentException("accountNumber is longer than 12-digits");
                //string pinValidationData = cardPan.Substring(0, cardPan.Length - 6); //AHMED
                string pinValidationData = cardPan.Substring(0, 10);
                pinValidationData = string.Format("{0}N{1}", pinValidationData, cardPan.Last());
                //configure Parameters
                var parameters = new List<string>()
                 {
                     PinConfigurationManager.HsmConfig.PinVerificationKeyPVV,
                     encryptedPin,
                     accountNumber.PadLeft(12,'0'),
                     "0", //minPinLength.ToString().PadLeft(2,'0'),
                     
                    // PinConfigurationManager.HsmConfig.DecimalisationTable,
                    // pinValidationData
                 };

                //configure Response Format
                var responseFormat = new MessageFields();
                responseFormat.Fields.Add(new MessageField() { Name = "offset", Length = 4 });

                var hsmResponse = _theHsm.Send("DG", parameters, responseFormat);

                IGeneratePinOffsetResponse response = new GeneratePinOffsetResponse();
                response.Offset = hsmResponse.Item("offset");

                return response;
            }

            public IGeneratePinOffsetResponse GeneratePinOffsetFromPinBlock(string encryptedPinBlock, string accountNumber, PinBlockFormats encryptedPinBlockFormat)
            {
                if (string.IsNullOrEmpty(encryptedPinBlock)) throw new ArgumentNullException("encryptedPin");
                if (string.IsNullOrEmpty(accountNumber)) throw new ArgumentNullException("accountNumber");
                if (accountNumber.Length > 12) throw new ArgumentException("accountNumber is longer than 12-digits");

                //configure Parameters
                var parameters = new List<string>()
                 {
                     "001",
                     PinConfigurationManager.HsmConfig.ZPK_LMK,
                     PinConfigurationManager.HsmConfig.PinVerificationKey,
                     encryptedPinBlock,
                     ((int)encryptedPinBlockFormat).ToString().PadLeft(2,'0'),
                     "04",
                     accountNumber.PadLeft(12,'0'),
                     PinConfigurationManager.HsmConfig.DecimalisationTable,
                     PinConfigurationManager.HsmConfig.PinValidationData
                 };//5145851169N5

                //configure Response Format
                var responseFormat = new MessageFields();
                responseFormat.Fields.Add(new MessageField() { Name = "offset", Length = 12 });

                var hsmResponse = _theHsm.Send("BK", parameters, responseFormat);

                IGeneratePinOffsetResponse response = new GeneratePinOffsetResponse();
                response.Offset = hsmResponse.Item("offset");

                return response;
            }

            #endregion
        }

        public class PinVerification : IPinVerification
        {
            private ThalesHsm _theHsm;

            public PinVerification(ThalesHsm theHsm)
            {
                _theHsm = theHsm;
            }

            #region IPinVerification Members


            /// <summary>
            /// 
            /// </summary>
            /// <param name="isInterchange"></param>
            /// <param name="pinEncryptionKey">zpk_lmk</param>
            /// <param name="pinVerificationKey">pvk</param>
            /// <param name="encryptedPinBlock">encrypted pin from the hsm</param>
            /// <param name="encryptedPinBlockFormat"></param>
            /// <param name="minPinLength">5</param>
            /// <param name="accountNumber">12 digit right most digit excluding the check digit</param>
            /// <param name="pinValidationData"></param>
            /// <param name="offset"></param>
            public void VerifyPIN(bool isInterchange, string pinEncryptionKey, string pinVerificationKey, string encryptedPinBlock, PinBlockFormats encryptedPinBlockFormat, int minPinLength, string accountNumber, string pinValidationData, string offset)
            {
                if (string.IsNullOrEmpty(pinVerificationKey)) throw new ArgumentNullException("pinVerificationKey");
                if (string.IsNullOrEmpty(pinEncryptionKey)) throw new ArgumentNullException("pinEncryptionKey");
                if (string.IsNullOrEmpty(encryptedPinBlock)) throw new ArgumentNullException("encryptedPinBlock");
                if (string.IsNullOrEmpty(accountNumber)) throw new ArgumentNullException("accountNumber");
                if (accountNumber.Length > 12) throw new ArgumentException("accountNumber is longer than 12-digits");
                if (pinValidationData.Length != 12) throw new ArgumentException("pinValidationData is not 12-digits");

                //string decTable = new PinTranslation(new ThalesHsm()).TranslateDecimalizationTableFromOldToNewLMK();

                //configure Parameters
                var parameters = new List<string>()
                 {
                     pinEncryptionKey,
                     pinVerificationKey,
                     "12",//(minPinLength+8).ToString().PadLeft(2,'0'),
                     encryptedPinBlock,
                     ((int)encryptedPinBlockFormat).ToString().PadLeft(2,'0'),
                     minPinLength.ToString().PadLeft(2,'0'),
                     accountNumber.PadLeft(12,'0'),
                    // decTable,
                     PinConfigurationManager.HsmConfig.DecimalisationTable,
                     pinValidationData,
                     offset
                 };

                //configure Response Format
                var responseFormat = new MessageFields();

                var hsmResponse = _theHsm.Send(isInterchange ? "EA" : "DA", parameters, responseFormat);
            }

            #endregion
        }

        #endregion

        #region Responses

        public class GeneratePinEncryptionKeyResponse : IGeneratePinEncryptionKeyResponse
        {
            #region IGeneratePinEncryptionKeyResponse Members

            public string PinEncryptionKeyUnderExchangeKey
            {
                get;
                set;
            }

            public string PinEncryptionKeyUnderStorageKey
            {
                get;
                set;
            }

            public string CheckValue
            {
                get;
                set;
            }

            #endregion
        }

        public class TranslatePinEncryptionKeyResponse : ITranslatePinEncryptionKeyResponse
        {
            #region ITranslatePinEncryptionKeyResponse Members

            public string TranslatedPinEncryptionKey
            {
                get;
                set;
            }

            public string KeyCheckValue
            {
                get;
                set;
            }

            #endregion
        }

        public class PinGeneratorResponse : IPinGenerationResponse
        {
            #region IPinGeneratorResponse Members

            public string EncryptedPin
            {
                get;
                set;
            }

            #endregion
        }

        public class GeneratePinOffsetResponse : IGeneratePinOffsetResponse
        {

            #region IGeneratePinOffsetResponse Members

            public string Offset
            {
                get;
                set;
            }

            #endregion
        }




        #endregion
        public class CvvGeneration : ICvvGeneration
        {
            private ThalesHsm _theHsm;

            public CvvGeneration(ThalesHsm theHsm)
            {
                _theHsm = theHsm;
            }
            public IGenerateCVVResponse GenerateCvv(string pan, string expiryDate)
            {
                #region Refactored
                /*
                if (string.IsNullOrEmpty(pan)) throw new ArgumentNullException("pan");
                if (pan.Length < 16 || pan.Length > 19) throw new ArgumentException("pan is shorter than 16-digits or greater that 19-digits");
                string serviceCode = "000"; // to be printed behind the card


                //configure Parameters
                var parameters = new List<string>()
                 {
                     Configuration.ConfigurationManager.HsmConfig.CardVerificationKey,
                     pan,
                     ";",
                     expiryDate,
                     serviceCode
                 };

                //configure Response Format
                var responseFormat = new MessageFields();
                responseFormat.Fields.Add(new MessageField() { Name = "cvv", Length = 3 });

                var hsmResponse = _theHsm.Send("CW", parameters, responseFormat);

                IGenerateCVVResponse response = new GenerateCVVResponse();
                response.Cvv = hsmResponse.Item("cvv");
                */
                #endregion

                IGenerateCVVResponse response = GenerateCvv(pan, expiryDate, PinConfigurationManager.HsmConfig.CardVerificationKey, "000");

                return response;
            }

            public IGenerateCVVResponse GenerateCvv(string pan, string expiryDate, string cardVerificationKey, string serviceCode)
            {
                if (string.IsNullOrEmpty(pan)) throw new ArgumentNullException("pan");
                if (pan.Length < 16 || pan.Length > 19) throw new ArgumentException("pan is shorter than 16-digits or greater that 19-digits");
                //string serviceCode = "000"; // to be printed behind the card


                //configure Parameters
                var parameters = new List<string>()
                 {
                     cardVerificationKey,
                     pan,
                     ";",
                     expiryDate,
                     serviceCode
                 };

                //configure Response Format
                var responseFormat = new MessageFields();
                responseFormat.Fields.Add(new MessageField() { Name = "cvv", Length = 3 });

                var hsmResponse = _theHsm.Send("CW", parameters, responseFormat);

                IGenerateCVVResponse response = new GenerateCVVResponse();
                response.Cvv = hsmResponse.Item("cvv");

                return response;
            }


        }



    }
    public class GenerateCVVResponse : IGenerateCVVResponse
    {
        public string Cvv
        {
            get;
            set;
        }
    }
}
