using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;

namespace DZLP.Objects
{
    struct URLToDownload
    {
        public string URL { get; set; }
        public string File { get; set; }
        public int Size { get; set; }
    }

    struct Server
    {
        public IPAddress IP { get; set; }
        public Int32 Port { get; set; }
        public string Name { get; set; }
        public string Version { get; set; }
        public Int32 Players { get; set; }
        public Int32 MaxPlayers { get; set; }
        public Int32 FreeSlots { get { return this.MaxPlayers - this.Players; } }
    }

    class Misc
    {
    }

}
