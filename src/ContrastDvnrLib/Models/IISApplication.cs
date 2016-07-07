using System.Collections.Generic;

namespace ContrastDvnrLib.Models
{
    public class IISApplication
    {
        public string Path { get; set; }
        
        public string PhysicalPath { get; set; }

        public string AppPoolName { get; set; }
        
        public string SpecificUser { get; set; }

        public bool EnablePreload { get; set; }

        public List<HttpModule> Modules { get; set; }

        public List<DotnetLibrary> Libraries { get; set; }

        public string EnabledProtocols { get; set; }

        public string AuthenticationLogonMethod { get; set; }
    }
}
