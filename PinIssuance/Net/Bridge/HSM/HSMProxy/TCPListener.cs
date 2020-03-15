using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace PinIssuance.Net.Bridge.HSM.HSMProxy
{
    public class HSMListener
    {
        TcpListener server = null;
        ArrayList socketListenersList = null;
        Thread purgingThread = null;
        Thread serverThread = null;
        bool stopServer = false;
        bool stopPurging = false;
        public HSMListener()
        {
            try
            {
                int tcpPort = 1133;
                string IpAddress = "127.0.0.1";
                if (!string.IsNullOrEmpty(System.Configuration.ConfigurationManager.AppSettings["HSMIP"]))
                {
                    IpAddress = System.Configuration.ConfigurationManager.AppSettings["HSMIP"];
                }

                System.Net.IPAddress localIP = System.Net.IPAddress.Parse(IpAddress);
                server = new TcpListener(localIP, tcpPort);

            }
            catch (Exception e)
            {
                server = null;
            }
        }

        public void StartServer()
        {
            if (server != null)
            {
                // Create a ArrayList for storing SocketListeners  
                socketListenersList = new ArrayList();

                // Start the Server and start the thread to listen client requests.
                server.Start();
                serverThread = new Thread(new ThreadStart(ServerThreadStart));
                serverThread.Start();

                // Create a low priority thread that checks and deletes client
                // SocktConnection objcts that are marked for deletion.
                purgingThread = new Thread(new ThreadStart(PurgingThreadStart));
                purgingThread.Priority = ThreadPriority.Lowest;
                purgingThread.Start();
            }
        }

        private void PurgingThreadStart()
        {
            while (!stopPurging)
            {
                ArrayList deleteList = new ArrayList();

                // Check for any clients SocketListeners that are to be
                // deleted and put them in a separate list in a thread safe fashion.
                //Monitor.Enter(m_socketListenersList);
                lock (socketListenersList)
                {
                    foreach (HSMSocketListener socketListener
                                 in socketListenersList)
                    {
                        if (socketListener.IsMarkedForDeletion())
                        {
                            deleteList.Add(socketListener);
                            socketListener.StopSocketListener();
                        }
                    }

                    // Delete all the client SocketConnection ojects which are
                    // in marked for deletion and are in the delete list.
                    for (int i = 0; i < deleteList.Count; ++i)
                    {
                        socketListenersList.Remove(deleteList[i]);
                    }
                }
                //Monitor.Exit(m_socketListenersList);

                deleteList = null;
                Thread.Sleep(10000);
            }
        }

        private void ServerThreadStart()
        {
            Socket clientSocket = null;
            HSMSocketListener socketListener = null;
            while (!stopServer)
            {
                try
                {
                    DBClient db = new DBClient("127.0.0.1", 1433);
                    db.Connect();
                    // Wait and accept client requests
                    clientSocket = server.AcceptSocket();

                    // Create a SocketListener object for the client.
                    socketListener = new HSMSocketListener(clientSocket);
                    socketListener.DBClient = db;

                    // Add the socket listener to an array list in a thread afe fashion. 
                    lock (socketListenersList)
                    {
                        socketListenersList.Add(socketListener);
                    }

                    // Start a communicating with the client in a different thread.
                    socketListener.StartSocketListener();
                }
                catch (SocketException se)
                {
                    stopServer = true;
                }
            }
        }

        public void StopServer()
        {
            if (server != null)
            {
                // It is important to Stop the server first before doing
                // any cleanup. If not so, clients might being added as
                // server is running, but supporting data structures
                // (such as m_socketListenersList) are cleared. This might cause exceptions.

                // Stop the TCP/IP Server.
                stopServer = true;
                server.Stop();

                if (serverThread != null)
                {
                    // Wait for one second for the the thread to stop.
                    serverThread.Join(1000);

                    // If still alive; Get rid of the thread.
                    if (serverThread.IsAlive)
                    {
                        serverThread.Abort();
                    }
                    serverThread = null;
                }

                stopPurging = true;
                if (purgingThread != null)
                {

                    purgingThread.Join(1000);
                    if (purgingThread.IsAlive)
                    {
                        purgingThread.Abort();
                    }
                    purgingThread = null;
                }
                // Free Server Object.
                server = null;

                // Stop All clients.
                StopAllSocketListers();
            }
        }

        private void StopAllSocketListers()
        {
            foreach (HSMSocketListener socketListener
                         in socketListenersList)
            {
                socketListener.StopSocketListener();
            }
            // Remove all elements from the list.
            socketListenersList.Clear();
            socketListenersList = null;
        }


    }
}
