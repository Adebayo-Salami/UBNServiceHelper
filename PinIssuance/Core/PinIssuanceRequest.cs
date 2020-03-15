using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PinIssuance.Core
{
    public class PinRequestCache
    {
        public virtual long ID { get; set; }
        public virtual long Issuer { get; set; }
        public virtual long Authorizer { get; set; }
        public virtual DateTime RequestedDate { get; set; }
        public virtual DateTime ApprovedDate { get; set; }
        public virtual string PosClientIp { get; set; }
        public virtual string PosClientPort { get; set; }
        public virtual string PosClientRequestData { get; set; }
        public virtual string PosClientResponseData { get; set; }
        public virtual string ClientID { get; set; }
        public virtual PinIssuanceRequestStatus Status { get; set; } 
    }

    public enum PinIssuanceRequestStatus
    {
        Pending = 1, Successful = 2, Failed = 3, Declined = 4
    }
}
