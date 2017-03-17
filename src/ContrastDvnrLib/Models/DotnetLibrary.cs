using System;

namespace ContrastDvnrLib.Models
{
    public class DotnetLibrary
    {

        public string Name { get; set; }

        public string Filepath { get; set; }

        public string Filename { get; set; }

        public string Version { get; set; }

        public string Culture { get; set; }

        public string SHA1Hash { get; set; }

        public string Md5Hash { get; set; }

        public string PublicKeyToken { get; set; }

        public string ProcessorArchitecture { get; set; }

        public string FileDescription { get; set; }

        public string ProductName { get; set; }

        public string ProductVersion { get; set; }

        public string Copyright { get; set; }

        public string Language { get; set; }

        public Version AssemblyVersion { get; set; }

        public LibraryIssue Issue { get; set; }
    }

    public class GacLibrary
    {
        public string Name { get; set; }
        
        public string Filepath { get; set; }

        public string Filename { get; set; }

        public string Version { get; set; }

        public string Culture { get; set; }

        public string SHA1Hash { get; set; }

        public string PublicKeyToken { get; set; }

        public string ProcessorArchitecture { get; set; }
    }
}
