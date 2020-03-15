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

using System;
using System.Reflection;
using Trx.Utilities;
using log4net;
using Trx.Messaging.FlowControl;
using Trx.Messaging;
using PrimeUtility.Configuration; 

namespace PinIssuance.Net.Bridge
{

	/// <summary>
	/// This class combines a <see cref="Peer"/> and a <see cref="Server"/>
	/// to implement a multiplexor.
	/// </summary>
	public class MyBridge : IMessageProcessor {
        
        //private Peer _externalPeer = null;
        //private Peer _internalPeer = null;
        private Peer _postBridgeNodePeer = null;
        private Trx.Messaging.FlowControl.Server _internalServer = null;
        private Trx.Messaging.FlowControl.Server _externalServer = null;
		private bool _withDoneEvent = false;
		private IMessageProcessor _nextMessageProcessor = null;
        private int _timeout = 0;
		private ILog _logger = null;

		#region Constructors
		/// <summary>
		/// Initializes a new instance of this class.
		/// </summary>
		public MyBridge() {
            _timeout = PinConfigurationManager.FepConfig.RequestTimeout;
		}
		#endregion

		#region Properties

        public Peer PostBridgeNodePeer
        {
            get
            {
                return _postBridgeNodePeer;
            }

            set
            {
                _postBridgeNodePeer = value;
            }
        }

        public IMessageSource PosRequestPeer
        {
            get;
            set;
        }

		/// <summary>
		/// It returns or sets the server used by the forwarder.
		/// </summary>
        public Trx.Messaging.FlowControl.Server InternalServer
        {

			get {

				return _internalServer;
			}

			set {

				_internalServer = value;
			}
		}

        public Trx.Messaging.FlowControl.Server ExternalServer
        {

            get
            {

                return _externalServer;
            }

            set
            {

                _externalServer = value;
            }
        }


		/// <summary>
		/// It returns or sets the logger associated to the channel.
		/// </summary>
		public ILog Logger {

			get {

				if ( _logger == null) {
					_logger = LogManager.GetLogger(
						MethodBase.GetCurrentMethod().DeclaringType);
				}

				return _logger;
			}

			set {

				if ( value == null) {
					_logger = LogManager.GetLogger(
						MethodBase.GetCurrentMethod().DeclaringType);
				} else {
					_logger = value;
				}
			}
		}

		/// <summary>
		/// It returns or sets the logger name associated to the channel.
		/// </summary>
		public string LoggerName {

			set {

				if ( StringUtilities.IsNullOrEmpty( value)) {
					Logger = null;
				} else {
					Logger = LogManager.GetLogger( value);
				}
			}

			get {

				return this.Logger.Logger.Name;
			}
		}

		/// <summary>
		/// It returns or sets the timeout for the requests sent to the
		/// forwarder peer.
		/// </summary>
		public int Timeout {

			get {

				return _timeout;
			}

			set {

				_timeout = value;
			}
		}

		/// <summary>
		/// It returns or sets the next messages processor.
		/// </summary>
		public IMessageProcessor NextMessageProcessor {

			get {

				return _nextMessageProcessor;
			}

			set {

				_nextMessageProcessor = value;
			}
		}
		#endregion

		#region Methods
		/// <summary>
		/// It's called to process the indicated message.
		/// </summary>
		/// <param name="source">
		/// It's the source of the message.
		/// </param>
		/// <param name="message">
		/// It's the message to be processed.
		/// </param>
		/// <returns>
		/// A logical value the same to true, if the messages processor
		/// processeced it, otherwise it returns false.
		/// </returns>
		/// <remarks>
		/// If the messages processor doesn't process it, the system
		/// delivers it to the next processor in the list, and so on until
		/// one process it, or there aren't other processors.
		/// 
		/// This function handles all the messages received by the server.
		/// </remarks>
		public bool Process( IMessageSource source, Message message) {

            try {
                //if ((_externalPeer == null) || !_externalPeer.IsConnected)
                //{
                //    return false;
                //}
                //PeerRequest request = new PeerRequest(_externalPeer, message);
                //if (source is Peer)
                //{
                //    if (((source as Peer).Channel as Trx.Messaging.Channels.TcpChannel).Port == 8583)
                //    {
                //        request = new PeerRequest(_internalPeer, message);
                //    }
                //}
                //request.Payload = source;
                //request.Send( _timeout );

                return true;
            }
            catch ( Exception e ) {
                Logger.Error( e );
                return false;
            }
		}

		/// <summary>
		/// Traps and handles the peer RequestDone event.
		/// </summary>
		/// <param name="sender">
		/// The <see cref="Peer"/> sending the event.
		/// </param>
		/// <param name="e">
		/// The event arguments.
		/// </param>
		private void OnPeerRequestDone( object sender, PeerRequestDoneEventArgs e) {

			if ( ( e.Request.Payload != null) && ( e.Request.Payload is IMessageSource)) {

				IMessageSource source = ( IMessageSource)( e.Request.Payload);

				if ( source.IsConnected) {

					source.Send( e.Request.ResponseMessage);
				}
			}
		}

		/// <summary>
		/// Traps and handles the peer Connected event.
		/// </summary>
		/// <param name="sender">
		/// The <see cref="Peer"/> sending the event.
		/// </param>
		/// <param name="e">
		/// The event arguments.
		/// </param>
		/// <remarks>
		/// If the forwarder has a <see cref="Server"/>, start it to
		/// listen new connections.
		/// </remarks>
		private void OnPeerConnected( object sender, EventArgs e) {

            if (_internalServer != null && !_internalServer.Listener.Listening)
            {
				_internalServer.Listener.Start();
			}
            if (_externalServer != null && !_externalServer.Listener.Listening)
            {
                _externalServer.Listener.Start();
            }
		}

		/// <summary>
		/// Traps and handles the peer Disconnected event.
		/// </summary>
		/// <param name="sender">
		/// The <see cref="Peer"/> sending the event.
		/// </param>
		/// <param name="e">
		/// The event arguments.
		/// </param>
		/// <remarks>
		/// When the <see cref="Peer"/> is disconnected from the remote system, the
		/// forwarder doesn't accept any new clients, and closes the existing
		/// connections.
		/// </remarks>
		private void OnPeerDisconnected( object sender, EventArgs e) {

            if (_internalServer != null)
            {
                _internalServer.Listener.Stop();
                PeerCollection peers = null;
                foreach (Peer peer in _internalServer.Peers)
                {
                    if (peers == null)
                    {
                        peers = new PeerCollection();
                    }
                    peers.Add(peer);
                }
                if (peers != null)
                {
                    foreach (Peer peer in peers)
                    {
                        try
                        {
                            peer.Close();
                        }
                        catch
                        {
                        }
                    }
                }
            }
		}
		#endregion
	}
}
