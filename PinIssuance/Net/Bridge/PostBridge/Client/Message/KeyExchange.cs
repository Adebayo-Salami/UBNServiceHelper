
using PinIssuance.Net.Bridge.PostBridge.Client.DTO;
namespace PinIssuance.Net.Bridge.PostBridge.Client.Messages
{
    public class KeyExchange : Message
    {
        public KeyExchange(string transactionID)
            : base(800, transactionID)
        {
            this.Fields.Add(70, "101");
        }
    }
}
