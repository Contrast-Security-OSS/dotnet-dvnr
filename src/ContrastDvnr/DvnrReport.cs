using ContrastDvnrLib.Models;
using System.Collections.Generic;

namespace ContrastDvnr
{
    public class DvnrReport
    {
        public MachineInformation MachineInformation { get; set; }

        public List<IISSite> Sites { get; set; }

        public List<IISAppPool> AppPools { get; set; }

        public List<DotnetLibrary> GacLibraries { get; set; }

    }
}
