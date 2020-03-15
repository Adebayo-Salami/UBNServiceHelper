using System;
using System.Collections.Generic;
using System.Text;

namespace PinIssuance.Net.Bridge.PostBridge.Client.DTO
{
    /// <summary>
    /// Represents a Bank Account
    /// </summary>
    public class Account
    {
        private string _Number;
        private string _Type;

        /// <summary>
        /// Initializes a new instance of the <see cref="Account"/> class.
        /// </summary>
        /// <param name="accountNumber">The account number.</param>
        /// <param name="accountType">Type of the account.</param>
        public Account(string accountNumber, string accountType)
        {
            this._Number = accountNumber;
            this._Type = accountType;
        }

        /// <summary>
        /// Gets or sets the Account number.
        /// </summary>
        /// <value>The Account number.</value>
        public string Number
        {
            get
            {
                return this._Number;
            }
            set
            {
                this._Number = value;
            }
        }

        /// <summary>
        /// Gets or sets the Account type.
        /// </summary>
        /// <value>The Account type.</value>
        public string Type
        {
            get
            {
                return this._Type;
            }
            set
            {
                this._Type = value;
            }
        }
    }
}
