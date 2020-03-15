using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PinIssuance.Core
{
    public class PosParameter 
    {
        public virtual string LocalIP { get; set; }
        public virtual string GatewayIP { get; set; }
        public virtual string NetMaskIP { get; set; }
        public virtual string DnsIP { get; set; }
        public virtual string TerminalID { get; set; }
        public virtual string TerminalSerial { get; set; }
        public virtual string RemoteIP { get; set; }
        public virtual int RemotePort { get; set; }
        public virtual string Version { get; set; }
        public virtual long Timeout { get; set; }
        public virtual string AdminPin { get; set; }
        public virtual long TheBranchID { get; set; }
        public virtual bool PinSelectionMenu { get; set; }
        public virtual bool PinChangemenu { get; set; }
        public virtual bool ConfirmPin { get; set; } 
    }
}