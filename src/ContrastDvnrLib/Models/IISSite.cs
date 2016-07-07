using System.Collections.Generic;

namespace ContrastDvnrLib.Models
{

    public class IISSiteBinding
    {
        public string Protocol { get; set; }

        public int? Port { get; set; }

        public string IpAddress { get; set; }

        public string Hostname { get; set; }

        public string BindingInformation { get; set; }

    }

    public class IISSite
    {

        public string Name { get; set; }

        public List<IISApplication> Applications { get; set; }

        public string DefaultAppPoolName { get; set; }

        public List<IISSiteBinding> Bindings { get; set; }

        public List<HttpModule> Modules { get; set; }

    }
}
