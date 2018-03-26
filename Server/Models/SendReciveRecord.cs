using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Server.Models
{
    public class SendReciveRecord
    {
        public long BroadcastRecevieCount;
        public long Connected;
        public long Reconnected;
        public long Disconnected;
        public long CliendSend;
        public string Ip;
        public string Time;
    }
}
