using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Xml.Serialization;
using System.Xml;
using System.Xml.Linq;

namespace PinIssuance.Net.Bridge.PostBridge.Utilities
{
    public class XMLSerializer
    {
        /// <summary>
        /// Method to convert a custom Object to XML string
        /// </summary>
        /// <param name="pObject">Object that is to be serialized to XML</param>
        /// <returns>XML string</returns>
        public static String Serialize<T>(T entity)
        {
            try
            {
                // prepare objects for use
                String XmlizedString = null;
                MemoryStream memoryStream = new MemoryStream();
                XmlSerializer xs = new XmlSerializer(typeof(T));
                XmlTextWriter xmlTextWriter = new XmlTextWriter(memoryStream, Encoding.UTF8);
                XmlSerializerNamespaces emptyNamespace = new XmlSerializerNamespaces();

                // remove the xmlns element of the xml output
                emptyNamespace.Add(string.Empty, string.Empty);

                // deserialize
                xs.Serialize(xmlTextWriter, entity, emptyNamespace);
                memoryStream = (MemoryStream)xmlTextWriter.BaseStream;
                XmlizedString = UTF8ByteArrayToString(memoryStream.ToArray());
                return XmlizedString;
            }

            catch (Exception e)
            {
                System.Console.WriteLine(e);
                return null;
            }
        }

        /// <summary>
        /// Method to reconstruct an Object from XML string
        /// </summary>
        /// <param name="pXmlizedString"></param>
        /// <returns></returns>
        public static object Deserialize<T>(String pXmlizedString)
        {
            XmlSerializer xs = new XmlSerializer(typeof(T));
            MemoryStream memoryStream = new MemoryStream(StringToUTF8ByteArray(pXmlizedString));
            XmlTextWriter xmlTextWriter = new XmlTextWriter(memoryStream, Encoding.UTF8);

            return xs.Deserialize(memoryStream);
        }

        public static T DeserializeXML<T>(String pXmlizedString)
        {
            XmlSerializer xs = new XmlSerializer(typeof(T));
            MemoryStream memoryStream = new MemoryStream(StringToUTF8ByteArray(pXmlizedString));
            XmlTextWriter xmlTextWriter = new XmlTextWriter(memoryStream, Encoding.UTF8);

            return (T) xs.Deserialize(memoryStream);
        }

        /// <summary>
        /// To convert a Byte Array of Unicode values (UTF-8 encoded) to a complete String.
        /// </summary>
        /// <param name="characters">Unicode Byte Array to be converted to String</param>
        /// <returns>String converted from Unicode Byte Array</returns>
        private static String UTF8ByteArrayToString(Byte[] characters)
        {
            UTF8Encoding encoding = new UTF8Encoding();
            String constructedString = encoding.GetString(characters);
            return (constructedString);
        }

        /// <summary>
        /// Converts the String to UTF8 Byte array and is used in De serialization
        /// </summary>
        /// <param name="pXmlString"></param>
        /// <returns></returns>
        private static Byte[] StringToUTF8ByteArray(String pXmlString)
        {
            UTF8Encoding encoding = new UTF8Encoding();
            Byte[] byteArray = encoding.GetBytes(pXmlString);
            return byteArray;
        }

        /// <summary>
        /// Obtain the value of a given element in XML
        /// </summary>
        /// <param name="XML">the XML</param>
        /// <param name="elementName">the element</param>
        /// <returns>the value of the given element</returns>
        public static string GetXmlElement(string XML, string elementName)
        {
            string element_value = string.Empty;
            try
            {
                // reformat the xml to conform
                if (!string.IsNullOrEmpty(XML))
                {
                    int index = XML.IndexOf('<');
                    if (index > 0)
                        XML = XML.Substring(index, XML.Length - index);
                }

                element_value = XElement.Parse(XML).Elements().FirstOrDefault(x => x.Name.ToString().ToUpper() == elementName.ToUpper()).Value;
            }
            catch { }
            return element_value;
        }

        //public static string GetXMLInMessage(Trx.Messaging.Iso8583.Iso8583Message message)
        //{
        //    string xml = string.Empty;
        //    if (message != null && message.Fields != null && message.Fields.Contains(127))
        //    {
        //        Trx.Messaging.Message outer = message.Fields[127].Value as Trx.Messaging.Message;
        //        if (outer.Fields.Contains(22))
        //        {
        //            xml = outer.Fields[22].Value.ToString();
        //        }
        //    }
        //    return xml;
        //}
    }
}
