using PinIssuance.Net.Bridge.PostBridge.Client.DTO;
using PinIssuance.Net.Bridge.PostBridge.Client.Exceptions;
using PinIssuance.Net.Bridge.PostBridge.Client.Messages;
using PinIssuance.Net.Bridge.PostBridge.Client.Response;
using PinIssuance.Net.Bridge.PostBridge.Utilities;
using PrimeUtility.Configuration;
using System;
using System.Threading;
using Trx.Messaging;
using Trx.Messaging.Channels;
using Trx.Messaging.FlowControl;
using Trx.Messaging.Iso8583;


namespace PinIssuance.Net.Bridge.PostBridge.Client
{
    public class Engine
    {
        private Peer _clientPeer;

        private string _hostname;
        private const string Client_NAME = "PinIssuance.Net.Bridge.PostBridge.Client";

        public int maxNoRetries = 3;
        public int serverTimeout = 10000;
        private int _port;
        private string _transactionID;
        private CardAcceptor _terminal;
        //private CardDetails _theCard;
        private Iso8583Message _lastMessageSent;

        public Iso8583Message LastMessageSent
        {
            get { return _lastMessageSent; }
            set { _lastMessageSent = value; }
        }

        //public CardDetails TheCard
        //{
        //    get { return _theCard; }
        //    set { _theCard = value; }
        //}
        public CardAcceptor TheTerminal
        {
            get { return _terminal; }
            set { _terminal = value; }
        }
        public string TransactionID
        {
            get
            {
                return _transactionID;
            }
            set
            {
                _transactionID = value;
            }
        }

        public Engine(string serverHost, int serverPort, CardAcceptor terminal, string transactionID)
        {
            serverTimeout = PinConfigurationManager.FepConfig.RequestTimeout;
            _hostname = serverHost;
            _port = serverPort;
            _transactionID = transactionID;
            _terminal = terminal;
        }

        public Engine(string serverHost, int serverPort, string transactionID)
        {
            serverTimeout = PinConfigurationManager.FepConfig.RequestTimeout;
            _hostname = serverHost;
            _port = serverPort;
            _transactionID = transactionID;
        }
        public bool Connect()
        {
            // Create a client peer to connect to remote system. The messages
            // will be matched using fields 41 and 11.
            if (_clientPeer == null)
            {
                _clientPeer = new ClientPeer(Client_NAME, new TwoBytesNboHeaderChannel(
                    new Iso8583Ascii1987BinaryBitmapMessageFormatter(), _hostname, _port),
                    new BasicMessagesIdentifier(FieldNos.F11_Trace));

            }
            _clientPeer.Connect();
            Thread.Sleep(1000);

            int retries = 0;
            while (retries < maxNoRetries)
            {
                lock (this)
                {
                    if (_clientPeer.IsConnected)
                    {
                        break;
                    }
                    else
                    {
                        _clientPeer.Close();
                        retries++;
                        _clientPeer.Connect();
                    }
                }
                Thread.Sleep(1000);
            }

            return _clientPeer.IsConnected;
        }



        public KeyExchangeResponse DoKeyExchange()
        {
            KeyExchangeResponse response = null;
            lock (this)
            {
                if (Connect())
                {
                    Console.WriteLine("connect");
                    KeyExchange keMsg = new KeyExchange(_transactionID);
                    new PANE.ERRORLOG.Error().LogInfo("About to send key Exchange Message: " + keMsg.ToString());
                    Trx.Messaging.Message responseMessage = ProcessRequest(keMsg);
                    response = new KeyExchangeResponse(responseMessage);

                    new PANE.ERRORLOG.Error().LogInfo("Recieved Exchange Message: " + responseMessage.ToString());
                    response.TransactionID = _transactionID;
                }
                else
                {
                    Console.WriteLine("not connected");
                }
            }
            return response;
        }

        private Trx.Messaging.Message ProcessRequest(Iso8583Message msg)
        {

            LastMessageSent = msg;
            PeerRequest request = new PeerRequest(_clientPeer, msg);
           // new PANE.ERRORLOG.Error().LogInfo(string.Format("Before sending {0}", _clientPeer.Channel.Name.ToString()));

            Console.WriteLine("The request: " + request.RequestMessage);

            new PANE.ERRORLOG.Error().LogInfo(string.Format("The request: " + request.RequestMessage));
            request.Send();
            request.WaitResponse(serverTimeout);

            if (request.Expired)
            {
                Console.WriteLine("Timed out");
                new PANE.ERRORLOG.Error().LogInfo("ProcessRequest Timed Out!");
                throw new ConnectionTimedOutException(msg);
            }
            else
            {
                Trx.Messaging.Message clo = request.ResponseMessage.Clone() as Trx.Messaging.Message;
                if (clo.Fields.Contains(127))
                {
                    (clo.Fields[127].Value as Trx.Messaging.Message).Parent = null;
                }
                if (clo.Fields.Contains(125))
                {
                    Console.WriteLine("ZPK: " + clo.Fields[125].Value);
                    new PANE.ERRORLOG.Error().LogInfo(string.Format("ZPK: " + clo.Fields[125].Value));
                    
                }

                Console.WriteLine("The response: " + request.RequestMessage);
                new PANE.ERRORLOG.Error().LogInfo(string.Format("The response: " + request.ResponseMessage));
            }

            return request.ResponseMessage;
        }

        public void Close()
        {
            lock (this)
            {
                _clientPeer.Close();
            }
        }

        public ChangePINResponse DoChangePIN(CardDetails theCard, Account acct, string seq_nr)
        {
            ChangePINResponse response = null;
            lock (this)
            {
                if (_clientPeer.IsConnected)
                {
                    ChangePIN cpMsg = new ChangePIN(_terminal, acct, theCard, _transactionID, seq_nr);
                    new PANE.ERRORLOG.Error().LogInfo("Pin Change Request: " + cpMsg.ToString());

                    Trx.Messaging.Message responseMessage = ProcessRequest(cpMsg);
                    new PANE.ERRORLOG.Error().LogInfo("Pin Change Response: " + responseMessage.ToString());
                    response = new ChangePINResponse(responseMessage);
                    response.TransactionID = _transactionID;
                }
            }
            return response;
        }

        public ChangePINResponse DoBalanceEnquiry(CardDetails theCard, Account acct, string seq_nr)
        {
            ChangePINResponse response = null;
            lock (this)
            {
                if (_clientPeer.IsConnected)
                {
                    BalanceEnquiryISO cpMsg = new BalanceEnquiryISO(acct, theCard, _transactionID, seq_nr);

                    Trx.Messaging.Message responseMessage = ProcessRequest(cpMsg);
                    response = new ChangePINResponse(responseMessage);
                    response.TransactionID = _transactionID;
                }
            }
            return response;
        }
    }
}
