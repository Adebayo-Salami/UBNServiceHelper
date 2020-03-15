using System;
using System.Text;
using System.Security.Cryptography;


namespace ViaCard.Base.Common.Utility
{
    /// <summary>
    /// Summary description for MD5Password
    /// </summary>
    public class MD5Password
    {
        public MD5Password()
        {
            //
            // TODO: Add constructor logic here
            //
        }

        public string CreateSecurePassword(string clear)
        {
            byte[] clearBytes;
            byte[] computedHash;

            clearBytes = ASCIIEncoding.ASCII.GetBytes(clear);
            computedHash = new MD5CryptoServiceProvider().ComputeHash(clearBytes);

            return ByteArrayToString(computedHash);
        }
        public string GetPasswordInClear(string secure)
        {
            throw new Exception("One way encryption service");
        }

        private string ByteArrayToString(byte[] array)
        {

            StringBuilder output = new StringBuilder(array.Length);

            for (int index = 0; index < array.Length; index++)
            {
                output.Append(array[index].ToString("X2"));
            }
            return output.ToString();
        }
    }
}