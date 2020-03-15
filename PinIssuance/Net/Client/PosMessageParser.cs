using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PinIssuance.Net.Client.Pos.Request;
using System.Reflection;

namespace PinIssuance.Net.Client.Pos
{
    public class PosMessageParser
    {
        public static T ParseRequestMessage<T>(string message)
        {
            if (string.IsNullOrEmpty(message)) return default(T);

            string header = message.Substring(0, 1);
            string body = string.Empty;
            if (message.StartsWith("C") || message.StartsWith("E"))
            {
                switch (header)
                {
                    case "C":
                        body = message.Substring(2);
                        break;
                    case "E":
                        body = DecryptMessage(message.Substring(2));
                        break;
                }
            }
            else
            {
                body = message;
            }
            string[] messageSplit = body.Split(',');
            string[] subSplit = null;

            T request = default(T);
            PropertyInfo[] properties = typeof(T).GetProperties();
            request = Activator.CreateInstance<T>();

            foreach (var property in properties)
            {
                foreach (var split in messageSplit)
                {
                    subSplit = split.Split('=');
                    if (subSplit != null && subSplit.Length == 2)
                    {
                        if (subSplit[0].ToLower() == property.Name.ToLower())
                        {
                            property.SetValue(request, subSplit[1], null);
                        }
                    }
                }
            }
            return request;
        }

        private static string DecryptMessage(string message)
        { 
            // run decript procedure here
            return message;
        }
    }
}
