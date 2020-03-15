using System;
using System.Collections.Generic;
using System.Text;

namespace PinIssuance.Net.Bridge.PostBridge.Client.DTO
{
    public class CardAcceptor
    {
        private string _ID = string.Empty;
        private string _TerminalID = string.Empty;
        private string _Location = System.Configuration.ConfigurationManager.AppSettings["BankLocation"];
        private string _City = string.Empty;
        private string _State = "LA";
        private string _Country = "NG";



        /// <summary>
        /// Initializes a new instance of the <see cref="Terminal"/> class.
        /// </summary>
        /// <param name="terminalID">The terminal ID.</param>
        public CardAcceptor(string cardAcceptorID, string terminalID)
        {
            this._ID = cardAcceptorID;
            this._TerminalID = terminalID;
        }

        
        public string ID
        {
            get
            {
                string result = _ID.PadRight(15);
                if (result.Length > 15)
                {
                    result = result.Substring(0, 15);
                }
                return result;
            }
            set
            {
                this._ID = value;
            }
        }

        /// <summary>
        /// Gets or sets the Terminal ID.
        /// </summary>
        /// <value>The Terminal ID.</value>
        public string TerminalID
        {
            get
            {
                string result = _TerminalID.PadRight(8);
                if (result.Length > 8)
                {
                    result = result.Substring(0, 8);
                }
                return result; 
            }
            set
            {
                this._TerminalID = value;
            }
        }

        public string Location
        {
            get 
            {
                string result = _Location.PadRight(23);
                if (result.Length > 23)
                {
                    result = result.Substring(0, 23);
                }
                return result; 
            }
            set 
            { 
                _Location = value; 
            }
        }
        public string City
        {
            get 
            {
                string result = _City.PadRight(13);
                if (result.Length > 13)
                {
                    result = result.Substring(0, 13);
                }
                return result; 
            }
            set 
            { 
                _City = value; 
            }
        }
        public string State
        {
            get 
            {
                string result = _State.PadRight(2);
                if (result.Length > 2)
                {
                    result = result.Substring(0, 2);
                }
                return result;  
            }
            set
            {
                _State = value;
            }
        }
        public string Country
        {
            get 
            {
                string result = _Country.PadRight(2);
                if (result.Length > 2)
                {
                    result = result.Substring(0, 2);
                }
                return result; 
            }
            set 
            { 
                _Country = value; 
            }
        }

    }
}
