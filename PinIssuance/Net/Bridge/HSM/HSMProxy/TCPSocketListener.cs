using System;
using System.Net.Sockets;
using System.Threading;
using System.Text;
using System.IO;
using System.Diagnostics;

namespace PinIssuance.Net.Bridge.HSM.HSMProxy
{
    public class HSMSocketListener
    {
        private Socket m_clientSocket = null;
        private bool m_stopClient = false;
        private Thread m_clientListenerThread = null;
        private bool m_markedForDeletion = false;

        private DateTime m_lastReceiveDateTime;
        private DateTime m_currentReceiveDateTime;

        public HSMSocketListener(Socket clientSocket)
        {
            m_clientSocket = clientSocket;
        }

        /// <summary>
        /// Client SocketListener Destructor.
        /// </summary>
        ~HSMSocketListener()
        {
            StopSocketListener();
        }

        /// <summary>
        /// Method that starts SocketListener Thread.
        /// </summary>
        public void StartSocketListener()
        {
            if (m_clientSocket != null)
            {
                m_clientListenerThread =
                    new Thread(new ThreadStart(SocketListenerThreadStart));

                m_clientListenerThread.Start();
            }
        }

        /// <summary>
        /// Thread method that does the communication to the client. This 
        /// thread tries to receive from client and if client sends any data
        /// then parses it and again wait for the client data to come in a
        /// loop. The recieve is an indefinite time receive.
        /// </summary>
        private void SocketListenerThreadStart()
        {
            int size = 0;
            Byte[] byteBuffer = new Byte[1024];

            m_lastReceiveDateTime = DateTime.Now;
            m_currentReceiveDateTime = DateTime.Now;

            Timer t = new Timer(new TimerCallback(CheckClientCommInterval),
                null, 15000, 15000);

            while (!m_stopClient)
            {
                try
                {
                    size = m_clientSocket.Receive(byteBuffer);
                    m_currentReceiveDateTime = DateTime.Now;
                    ProcessRequest(byteBuffer, size);
                }
                catch (SocketException se)
                {
                    Trace.TraceInformation(se.ToString());
                    m_stopClient = true;
                    m_markedForDeletion = true;
                }
            }
            t.Change(Timeout.Infinite, Timeout.Infinite);
            t = null;
        }

        /// <summary>
        /// Method that stops Client SocketListening Thread.
        /// </summary>
        public void StopSocketListener()
        {
            if (m_clientSocket != null)
            {
                m_stopClient = true;
                m_clientSocket.Close();

                // Wait for one second for the the thread to stop.
                m_clientListenerThread.Join(1000);

                // If still alive; Get rid of the thread.
                if (m_clientListenerThread.IsAlive)
                {
                    m_clientListenerThread.Abort();
                }
                m_clientListenerThread = null;
                m_clientSocket = null;
                m_markedForDeletion = true;
            }
        }

        /// <summary>
        /// Method that returns the state of this object i.e. whether this
        /// object is marked for deletion or not.
        /// </summary>
        /// <returns></returns>
        public bool IsMarkedForDeletion()
        {
            return m_markedForDeletion;
        }


        /// <summary>
        /// This is where the message is sent to the HSM and the response is returned back
        /// </summary>
        /// <param name="byteBuffer"></param>
        /// <param name="size"></param>
        private void ProcessRequest(Byte[] byteBuffer, int size)
        {
            string data = Encoding.ASCII.GetString(byteBuffer, 0, size);


            Trace.TraceInformation(data);

            try
            {
                //TODO: this part is for HSM
                //Process the request and send back

                //ThalesHsm hsm = new ThalesHsm();
                //string output = hsm.SendProxyCommand(data);
                //Byte[] sendBytes = Encoding.ASCII.GetBytes(output);
                //m_clientSocket.Send(sendBytes);


                //DBClient db = new DBClient("127.0.0.1", 8583);
                Byte[] sendBytes = DBClient.SendProxyCommand(data);
                m_clientSocket.Send(sendBytes);

            }
            catch (SocketException se)
            {
                Trace.TraceInformation(se.ToString());
            }
        }




        /// <summary>
        /// Method that checks whether there are any client calls for the
        /// last 15 seconds or not. If not this client SocketListener will be closed.
        /// </summary>
        /// <param name="o"></param>
        private void CheckClientCommInterval(object o)
        {
            if (m_lastReceiveDateTime.Equals(m_currentReceiveDateTime))
            {
                this.StopSocketListener();
            }
            else
            {
                m_lastReceiveDateTime = m_currentReceiveDateTime;
            }
        }

        internal DBClient DBClient { get; set; }
    }

    internal class DBClient
    {
        TcpClient dbClient;
        string _ip;
        int _port;

        public DBClient(string ip, int port)
        {
            _ip = ip;
            _port = port;
        }
         
        public byte[] SendProxyCommand(string sendData)
        {
             
            Stream stm = dbClient.GetStream();

            ASCIIEncoding asen = new ASCIIEncoding();
            byte[] ba = asen.GetBytes(sendData);
           
            stm.Write(ba, 0, ba.Length);

            byte[] bb = new byte[100];
            int k = stm.Read(bb, 0, 100);
             
            //for (int i = 0; i < k; i++)
            //    Console.Write(Convert.ToChar(bb[i]));
           
            return bb;
        }
        //public void StreamReceive(IAsyncResult ar);
        //public void TermClient();
         

        internal void Connect()
        {
            dbClient = new TcpClient();
            dbClient.Connect(_ip, _port);
        }
    }

}
