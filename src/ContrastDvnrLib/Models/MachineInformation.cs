using System.Collections.Generic;
using System.Xml.Serialization;

namespace ContrastDvnrLib.Models
{
    [XmlRoot("MachineInformation")]
    public class MachineInformation
    {

        public string OsVersion { get; set; }

        public string ProcessorID { get; set; }


        [XmlArray("DotnetFrameworkVersions")]
        [XmlArrayItem("Version")]
        public List<string> DotnetFrameworkVersions { get; set; }

        public bool IsIISInstalled { get; set; }

        public bool IsIISExpressInstalled { get; set; }

        public long MemoryAvailable { get; set; }

        public string IISVersion { get; set; }

        public string SystemDrive { get; set; }

        public string OsArchitecture { get; set; }

        public uint ProcessorPhysicalCores { get; set; }

        public uint ProcessorLogicalCores { get; set; }

        public string IISExpressVersion { get; set; }
    }
}
