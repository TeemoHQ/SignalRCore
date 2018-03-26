using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;

namespace Client
{
    [JsonObject(MemberSerialization.OptIn)]
    public class TestArg
    {

        public string Url { get; set; }

        public string Transport { get; set; }

        public int BatchSize { get; set; }

        public int ConnectInterval { get; set; }

        [JsonProperty]
        public int Connections { get; set; }

        public int ConnectTimeout { get; set; }

        public int MinServerMBytes { get; set; }

        public int SendBytes { get; set; }

        public int SendInterval { get; set; }

        public int SendTimeout { get; set; }

        public string ControllerUrl { get; set; }

        public int NumClients { get; set; }

        public string LogFile { get; set; }

        public int SampleInterval { get; set; }

        public string SignalRInstance { get; set; }

        [JsonProperty]
        public string RedisConnectString { get; set; }

        [JsonProperty]
        public int MessageRate { get; set; }

        [JsonProperty]
        public int SendPollTime { get; set; }

    }
}
