using PinIssuance.Net.Bridge.HSM;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Services;

namespace UBNServiceHelper
{
    /// <summary>
    /// Summary description for GenerateData
    /// </summary>
    [WebService(Namespace = "http://tempuri.org/")]
    [WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
    [System.ComponentModel.ToolboxItem(false)]
    // To allow this Web Service to be called from script, using ASP.NET AJAX, uncomment the following line. 
    // [System.Web.Script.Services.ScriptService]
    public class GenerateData : System.Web.Services.WebService
    {
        #region HelperClass

        [Serializable]
        public class Response
        {
            public string ResponseCode { get; set; }

            public string ResponseMessage { get; set; }

            public string Cvv { get; set; }
        }

        #endregion

        #region ApiMethods
        [WebMethod]
        [System.Xml.Serialization.XmlInclude(typeof(Object[]))]
        public Response GenerateCardData(string pan, string expiryDate, string password)
        {
            Response response = new Response();
            try
            {
                if (String.IsNullOrWhiteSpace(pan) || String.IsNullOrWhiteSpace(expiryDate) || String.IsNullOrWhiteSpace(password))
                {
                    response.ResponseCode = "-10";
                    response.ResponseMessage = "Invalid Parameter Passed";
                    return response;
                }

                //Because of the sensivity of the cvv, i will be hardcoding the password
                if(password != "&amp&Password&21&Appzone")
                {
                    response.ResponseCode = "-20";
                    response.ResponseMessage = "Password Is Incorrect, After your third incorrect attempt, your details will be sent to the officials";
                    return response;
                }

                bool debugMode = Convert.ToBoolean(System.Configuration.ConfigurationManager.AppSettings["DebugMode"]);
                if(debugMode)
                {
                    response.ResponseCode = "00";
                    response.ResponseMessage = "Successful";
                    response.Cvv = "999";
                    return response;
                }

                IGenerateCVVResponse cvv = new ThalesHsm().CvvGenerator().GenerateCvv(pan, expiryDate);
                response.ResponseCode = "00";
                response.ResponseMessage = "Successful";
                response.Cvv = cvv.Cvv;
            }
            catch (Exception error)
            {
                response.ResponseCode = "-90";
                response.ResponseMessage = error.Message;
            }
            return response;
        }
        #endregion
    }
}
