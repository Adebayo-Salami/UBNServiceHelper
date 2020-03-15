using PrimeUtility.Configuration;
using System;
using Trx.Messaging;
using Trx.Messaging.Channels;
using Trx.Messaging.FlowControl;
using Trx.Messaging.Iso8583;

namespace PinIssuance.Net.Bridge
{
    public class Engine
    {
        private const int Field11Trace = 11;
        private const int Field41TerminalCode = 41;
        private VolatileStanSequencer _sequencer;

        private MyBridge _forwarder;

        /// <summary>
        /// Initializes a new instance of this class.
        /// </summary>
        /// <param name="hostName">
        /// The server hosting the acquirer.
        /// </param>
        /// <param name="localInterface">
        /// The ip address of the local interface to listen.
        /// </param>
        public Engine()
        {
            try
            {
                _forwarder = new MyBridge();
                _sequencer = new VolatileStanSequencer();

                // Create a server to listen internal clients requests (internal clients are clients that connect 
                //from viacard itself e.g loading, settlement, reversal. etc.).
                TcpListener inlistener = new TcpListener(PinConfigurationManager.FepConfig.InternalServerPort);
                inlistener.LocalInterface = PinConfigurationManager.FepConfig.BridgeHostIp;
                _forwarder.InternalServer = new Trx.Messaging.FlowControl.Server("InternalServer",
                    inlistener, new BasicServerPeerManager());

                // Create a server to listen external clients requests (external clients are the node connections).
                TcpListener exlistener = new TcpListener(PinConfigurationManager.FepConfig.ExternalServerPort);
                exlistener.LocalInterface = PinConfigurationManager.FepConfig.BridgeHostIp;
                _forwarder.ExternalServer = new Trx.Messaging.FlowControl.Server("ExternalServer",
                    exlistener, new BasicServerPeerManager2());


                // Configure the internal server to accept up to 4 clients (i.e 4 connections from ViaCard at a time).
                //_forwarder.InternalServer.Listener.ChannelPool = new BasicChannelPool(
                //    new TwoBytesNboHeaderChannel(new Iso8583Ascii1987BinaryBitmapMessageFormatter()), 
                //    1000);

                //_forwarder.ExternalServer.Listener.ChannelPool = new BasicChannelPool(
                //    new TwoBytesNboHeaderChannel(new Iso8583Ascii1987BinaryBitmapMessageFormatter()),
                //    1000);
                _forwarder.InternalServer.Listener.ChannelPool = new BasicChannelPool(
                    new TwoBytesNboHeaderChannel(new Iso8583Ascii1987BinaryBitmapMessageFormatter()),
                    1000);

                _forwarder.ExternalServer.Listener.ChannelPool = new BasicChannelPool(
                    new TwoBytesNboHeaderChannel(new Iso8583Ascii1987BinaryBitmapMessageFormatter()),
                    1000);


                // Instruct the server peer manager to deliver all the received
                // messages (from all its peers) to the forwarder.
                _forwarder.InternalServer.PeerManager.MessageProcessor = new InternalServerProcessor(_forwarder);
                _forwarder.ExternalServer.PeerManager.MessageProcessor = new ExternalServerProcessor(_forwarder);
            }
            catch (Exception ex) { new PANE.ERRORLOG.Error().LogToFile(ex); }
        }

        void ExternalPeer_Receive(object sender, ReceiveEventArgs e)
        {
            new PANE.ERRORLOG.Error().LogInfo("Received external message");
        }

        public void Start()
        {
            try
            {
                _forwarder.InternalServer.Listener.Start();
                _forwarder.ExternalServer.Listener.Start();
            }
            catch (Exception ex) { new PANE.ERRORLOG.Error().LogToFile(ex); }
        }

        /// <summary>
        /// Stop forwarder activity.
        /// </summary>
        public void Stop()
        {
            try
            {
                _forwarder.InternalServer.Listener.Stop();
                _forwarder.ExternalServer.Listener.Stop();
            }
            catch (Exception ex) { new PANE.ERRORLOG.Error().LogToFile(ex); }
        }

    }
}
