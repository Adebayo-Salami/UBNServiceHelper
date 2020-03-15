using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PinIssuance.Net.Client.Pos.Contract;
using PrimeUtility.Configuration;

namespace PinIssuance.Net.Client.Pos.Response
{
    public class ParameterDownloadResponse : IResponse
    {
        public string LocalIP { get; set; }
        public string GetewayIP { get; set; }
        public string NetMaskIP { get; set; }
        public string DnsIP { get; set; }
        public string Tpk1 { get; set; }
        public string Tpk2 { get; set; }
        public string TerminalId { get; set; }
        public string ServerIP { get; set; }
        public string ServerPort { get; set; }
        public string RemoteIP { get; set; }
        public string RemotePort { get; set; }
        public string Version { get; set; }
        public string TimeOut { get; set; }
        public string AdminPin { get; set; }
        public string BankName { get; set; }
        public string BranchName { get; set; }
        public bool PinSelectionMenu { get; set; }
        public bool PinChangeMenu { get; set; }
        public bool ConfirmPin { get; set; }
        public string TimeDate { get; set; }

        public override string ToString()
        {
            StringBuilder result = new StringBuilder("C:");
            result.Append(string.Format("localip={0},",LocalIP));
            result.Append(string.Format("gatewayip={0},", GetewayIP));
            result.Append(string.Format("netmaskip={0},", NetMaskIP));
            result.Append(string.Format("dnsip={0},", DnsIP));
            result.Append(string.Format("tpk1={0},", PinConfigurationManager.PosConfig.Tpk1));
            result.Append(string.Format("tpk2={0},", PinConfigurationManager.PosConfig.Tpk2));
            result.Append(string.Format("terminalid={0},", TerminalId));
            result.Append(string.Format("serverip={0},", ServerIP));
            result.Append(string.Format("serverport={0},", ServerPort));
            result.Append(string.Format("remoteip={0},", RemoteIP));
            result.Append(string.Format("remoteport={0},", RemotePort));
            result.Append(string.Format("version={0},", Version));
            result.Append(string.Format("timeout={0},", TimeOut));
            result.Append(string.Format("adminpin={0},", AdminPin));
            result.Append(string.Format("bankname={0},", PinConfigurationManager.PosConfig.BankName));
            result.Append(string.Format("branchname={0},", BranchName));
            result.Append(string.Format("pinselectionmenu={0},", PinSelectionMenu ? 1 : 0));
            result.Append(string.Format("pinchangemenu={0},", PinChangeMenu ? 1 : 0));
            result.Append(string.Format("confirmpin={0},", ConfirmPin ? 1 : 0));
            result.Append(string.Format("timedate={0},", DateTime.Now.ToString("yyyyMMddHHmmss")));

            return result.ToString();
        }
    }
}