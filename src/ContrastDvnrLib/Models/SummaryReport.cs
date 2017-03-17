using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ContrastDvnrLib.Models.Report
{
    public class SummaryReport
    {
        public List<ApplicationInfo> Applications { get; set; }

        public List<AppPoolInfo> AppPools { get; set; }

        public List<DotnetLibrary> GacLibraryIssues { get; set; }

        public List<AppIssue> AppLibraryIssues { get; set; }
    }

    public class AppIssue
    {
        public string AppName { get; set; }

        public DotnetLibrary Library { get; set; }
    }


    public class ApplicationInfo
    {
        public string Path { get; set; }

        public string AppPool { get; set; }

        public string Site { get; set; }

        public string ClrVersion { get; set; }

        public string Bitness { get; set; }

        public string Pipeline { get; set; }

        public int NumLibs { get; set; }

    }

    public class AppPoolInfo
    {
        public string Name { get; set; }

        public string ClrVersion { get; set; }

        public string Bitness { get; set; }

        public string Pipeline { get; set; }

        public int NumApplications { get; set; }

        public string AppNames { get; set; }

        public string Rating { get; set; }

    }
}
