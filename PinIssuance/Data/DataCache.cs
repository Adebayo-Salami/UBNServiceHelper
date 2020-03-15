using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Remoting.Messaging;
using PinIssuance.Core;
using System.Net.Sockets;
using System.Net;
using PinIssuance.Net.Client.Pos;

namespace PinIssuance.Data
{
    public class PinIssuanceRequestCache
    {
        private const string PIN_ISSUANCE_REQ_DATA = "::PIN:ISSUANCE:REQUEST:DATA::";


        // during implemenetation, u need to implement real db for this
        protected static Dictionary<long, PinRequestCache> theRequestCache
        {
            get
            {
                if (!CacheLayer.Exists(PIN_ISSUANCE_REQ_DATA))
                {
                    CacheLayer.Add<Dictionary<long, PinRequestCache>>(new Dictionary<long, PinRequestCache>(), PIN_ISSUANCE_REQ_DATA);
                }

                return CacheLayer.Get<Dictionary<long, PinRequestCache>>(PIN_ISSUANCE_REQ_DATA);
            }
            set
            {
                CacheLayer.Add<Dictionary<long, PinRequestCache>>(value, PIN_ISSUANCE_REQ_DATA);
            }
        }

        public static IList<PinRequestCache> ThePinIssuanceRequestCache
        {
            get
            {
                return theRequestCache.Values.ToList();
            }

        }

        public static void LogPinIssuanceRequest(string requestData, TcpClient clientConnected)
        {
            PinRequestCache request = new PinRequestCache();
            request.Issuer = 1;
            request.PosClientIp = ((IPEndPoint)clientConnected.Client.RemoteEndPoint).Address.ToString();
            request.PosClientPort = ((IPEndPoint)clientConnected.Client.RemoteEndPoint).Port.ToString();
            request.ClientID = request.PosClientIp.Replace(".", "") + request.PosClientPort;
            request.PosClientRequestData = requestData;
            request.RequestedDate = DateTime.Now;
            request.Status = PinIssuanceRequestStatus.Pending;
            request.ID = theRequestCache.Count == 0 ? 1 : theRequestCache.Values.OrderBy(x => x.ID).Last().ID + 1;

            theRequestCache.Add(request.ID, request);
        }

        public static string UpdatePinIssuanceRequest(PinRequestCache request)
        {
            if (theRequestCache.ContainsKey(request.ID))
            {
                theRequestCache[request.ID] = request;
            }

            return request.ClientID;
        }

        public static PinRequestCache Get(long requestId)
        {
            return theRequestCache[requestId];
        }
    }

    public class ConnectedTerminalCache
    {
        private const string LOADED_TERMINALS = "::LOADED:TERMINALS::"; 
        protected static Dictionary<string, ConnectedTerminal> theTerminalCache
        {
            get
            {
                if (!CacheLayer.Exists(LOADED_TERMINALS))
                {
                    CacheLayer.Add<Dictionary<string, ConnectedTerminal>>(new Dictionary<string, ConnectedTerminal>(), LOADED_TERMINALS);
                }

                return CacheLayer.Get<Dictionary<string, ConnectedTerminal>>(LOADED_TERMINALS);
            }
            set
            {
                CacheLayer.Add<Dictionary<string, ConnectedTerminal>>(value, LOADED_TERMINALS);
            }
             
        }

        public static IList<ConnectedTerminal> TheConnectedTerminalCache
        {
            get
            {
                return theTerminalCache.Values.ToList();
            }
        }

        public static void Add(TcpClient clientConnected)
        {
            ConnectedTerminal terminal = new ConnectedTerminal();
            terminal.ClientSocket = clientConnected;
            terminal.IpAddress = ((IPEndPoint)clientConnected.Client.RemoteEndPoint).Address.ToString();
            terminal.Port = ((IPEndPoint)clientConnected.Client.RemoteEndPoint).Port.ToString();
            terminal.ClientID = terminal.IpAddress.Replace(".", "") + terminal.Port;
            if (!theTerminalCache.ContainsKey(terminal.ClientID)) theTerminalCache.Add(terminal.ClientID, terminal);
        }

        public static ConnectedTerminal Get(string clientID)
        {
            return theTerminalCache[clientID];
        }

        public static void Remove(string clientID)
        {
            if (theTerminalCache.ContainsKey(clientID))
            {
                theTerminalCache.Remove(clientID);
            }
        }
    }
}
