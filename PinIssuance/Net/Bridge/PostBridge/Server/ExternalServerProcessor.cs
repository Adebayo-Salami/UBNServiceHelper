#region Copyright (C) 2004-2006 Diego Zabaleta, Leonardo Zabaleta
//
// Copyright © 2004-2006 Diego Zabaleta, Leonardo Zabaleta
//
// This program is free software; you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation; either version 2 of the License, or
// (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program; if not, write to the Free Software
// Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA
#endregion

using log4net;
using PrimeUtility.Configuration;
using System;
using System.Reflection;
using Trx.Messaging;
using Trx.Messaging.FlowControl;
using Trx.Messaging.Iso8583;
using Trx.Utilities;


namespace PinIssuance.Net.Bridge
{

    /// <summary>
    /// This class combines a <see cref="Peer"/> and a <see cref="Server"/>
    /// to implement a multiplexor.
    /// </summary>
    public class ExternalServerProcessor : IMessageProcessor
    {

        private MyBridge _theBridge = null;

        private DateTime lastKeyExchangeDate = DateTime.MinValue;
        private IMessageProcessor _nextMessageProcessor = null;
        private int _timeout = 0;
        private ILog _logger = null;

        #region Constructors
        /// <summary>
        /// Initializes a new instance of this class.
        /// </summary>
        public ExternalServerProcessor(MyBridge theBridge)
        {
            this._theBridge = theBridge;
            this._timeout = PinConfigurationManager.FepConfig.RequestTimeout;
        }
        #endregion

        #region Properties

        public MyBridge TheBridge
        {
            get { return _theBridge; }
            set { _theBridge = value; }
        }

        /// <summary>
        /// It returns or sets the logger associated to the channel.
        /// </summary>
        public ILog Logger
        {

            get
            {

                if (_logger == null)
                {
                    _logger = LogManager.GetLogger(
                        MethodBase.GetCurrentMethod().DeclaringType);
                }

                return _logger;
            }

            set
            {

                if (value == null)
                {
                    _logger = LogManager.GetLogger(
                        MethodBase.GetCurrentMethod().DeclaringType);
                }
                else
                {
                    _logger = value;
                }
            }
        }

        /// <summary>
        /// It returns or sets the logger name associated to the channel.
        /// </summary>
        public string LoggerName
        {

            set
            {

                if (StringUtilities.IsNullOrEmpty(value))
                {
                    Logger = null;
                }
                else
                {
                    Logger = LogManager.GetLogger(value);
                }
            }

            get
            {

                return this.Logger.Logger.Name;
            }
        }
        #endregion

        #region Methods

        #endregion

        #region IMessageProcessor Members

        public bool Process(IMessageSource source, Message message)
        {
            try
            {
                string postBridgeNodeIp = (source as ServerPeer).Channel.Name;
                Iso8583Message theMsg = message as Iso8583Message;
                if (TheBridge.PostBridgeNodePeer != null && TheBridge.PostBridgeNodePeer.Channel != null &&
                    postBridgeNodeIp == TheBridge.PostBridgeNodePeer.Channel.Name && !theMsg.IsRequest())
                {
                    if (_theBridge.PosRequestPeer.IsConnected)
                    {
                        _theBridge.PosRequestPeer.Send(message);
                        return true;
                    }
                    else return false;
                }
                bool isSignOnMessage = false;
                new PANE.ERRORLOG.Error().LogInfo("Request message: " + theMsg.ToString());
                PostBridge.Server.Messages.Message requestMessage = MessageParser.Read(theMsg, out isSignOnMessage);
                if (requestMessage.Validate())
                {
                    Iso8583Message msg = requestMessage.Execute();
                    new PANE.ERRORLOG.Error().LogInfo("About to send Response message: " + msg.ToString());
                    source.Send(msg);

                    //If is first sign on message, send Keyexchange.
                    if (!PinConfigurationManager.FepConfig.KeyExchangeOnConnect && lastKeyExchangeDate.Date != DateTime.Today && isSignOnMessage)
                    {
                        new BasicServerPeerManager2().DoKeyExchange(null);
                        lastKeyExchangeDate = DateTime.Now;
                    }
                }

                return true;
            }
            catch (Exception e)
            {
                new PANE.ERRORLOG.Error().LogToFile(e);
                Logger.Error(e);
                return false;
            }


        }

        public IMessageProcessor NextMessageProcessor
        {
            get
            {
                return _nextMessageProcessor;
            }
            set
            {
                _nextMessageProcessor = value;
            }
        }

        #endregion
    }
}
