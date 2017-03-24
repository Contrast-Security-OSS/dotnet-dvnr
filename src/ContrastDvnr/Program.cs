using DocoptNet;
using ContrastDvnrLib;
using System;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Diagnostics;
using RazorEngine.Configuration;
using RazorEngine.Templating;
using ContrastDvnrLib.Models.Report;
using System.Collections.Generic;
using ContrastDvnrLib.Models;
using System.Reflection;
using System.Security.Policy;
using System.Security;
using System.Security.Permissions;

namespace ContrastDvnr
{

    public enum ReportFileType
    {
        Unknown,
        Xml,
        Json,
        Text
    }

    public class Program
    {
        private const string usage = @"ContrastDvnr 1.0.  Utility for displaying information about the IIS 7.0+ sites and applications on the current machine.  By default results are written to report.xml file in XML format.  Json or text output format can be chosen instead.

    Usage: 
        ContrastDvnr.exe [xml | json | text] [--from=<FROM_FILE>] [--screen]
        ContrastDvnr.exe (-h | --help)
        ContrastDvnr.exe --version

    Options:
        --from=<FROM_FILE>     Generate from existing dvnr report instead of current machine
        --screen          Display to standard output stream instead of file
";

        public static void Main(string[] args)
        {

            if (AppDomain.CurrentDomain.IsDefaultAppDomain())
            {
                // RazorEngine cannot clean up from the default appdomain...
                //Console.WriteLine("Switching to second AppDomain...");
                AppDomainSetup adSetup = new AppDomainSetup();
                adSetup.ApplicationBase = AppDomain.CurrentDomain.SetupInformation.ApplicationBase;
                var current = AppDomain.CurrentDomain;
                // You only need to add strongnames when your appdomain is not a full trust environment.
                var strongNames = new StrongName[0];

                var domain = AppDomain.CreateDomain(
                    "MyMainDomain", null,
                    current.SetupInformation, new PermissionSet(PermissionState.Unrestricted),
                    strongNames);
                try
                {
                    domain.ExecuteAssembly(Assembly.GetExecutingAssembly().Location, args);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error executing ContrastDvnr: " + ex.Message);
                    Trace.TraceError("Error executing ContrastDvnr: " + ex);
                    return;
                }
                return;
            }

            var traceOutput = new DvnrTraceListener("ContrastDvnr.log", "tracelog");
            Trace.Listeners.Add(traceOutput);
            Trace.AutoFlush = true;

            var arguments = new Docopt().Apply(usage, args, version: "ContrastDvnr 1.0", exit: true);

            Trace.TraceInformation("ContrastDvnr executed with arguments [{0}]", string.Join(" ", args));

            // ensure IIS is installed and we can use ServerManager
            if (!Reporting.PreFlightCheck())
            {
                return;
            }

            bool isXml = arguments["xml"].IsTrue;
            bool isJson = arguments["json"].IsTrue;
            bool isText = arguments["text"].IsTrue;

            ReportFileType reportType = ReportFileType.Unknown;

            // default to xml if no format is specified
            if (!isXml && !isJson && !isText)
                reportType = ReportFileType.Xml;
            else if (isXml)
                reportType = ReportFileType.Xml;
            else if (isJson)
                reportType = ReportFileType.Json;
            else if (isText)
                reportType = ReportFileType.Text;

            string fileName = "report.xml";

            if (reportType == ReportFileType.Xml) fileName = "report.xml";
            else if (reportType == ReportFileType.Json) fileName = "report.json";
            else if (reportType == ReportFileType.Text) fileName = "report.txt";


            DvnrReport report;
            string fromFilename = arguments.ContainsKey("--from") ? arguments["--from"]?.ToString() : "";

            if (!string.IsNullOrEmpty(fromFilename))
            {
                report = GenerateReportFromExisting(fromFilename, reportType);
            }
            else
            {
                report = GenerateReportFromCurrentMachine();
            }

            string directory = CreateOutputDirectory();

            if (arguments["--screen"].IsTrue)   // write to screen instead of file
            {
                if (reportType == ReportFileType.Xml) PrintReportXml(report, Console.Out);
                if (reportType == ReportFileType.Json) PrintReportJson(report, Console.Out);
                if (reportType == ReportFileType.Text) PrintReportText(report, Console.Out);
            }
            else
            {   // write to file
                string dvnrReportPath = Path.Combine(directory, fileName);
                Console.WriteLine("Writing DVNR report.");
                WriteDvnrReportFile(reportType, dvnrReportPath, report);

                string compatReportPath = Path.Combine(directory, "compatSummary.md");
                Console.WriteLine("Writing compatibility report.");
                WriteCompatSummary(report, compatReportPath);
            }

            Trace.TraceInformation("ContrastDvnr exited");
            Trace.Flush();
        }

        private static string CreateOutputDirectory()
        {
            string dir = string.Format("{0}_{1:yyyyMMddHHmm}", System.Net.Dns.GetHostName().Replace(' ', '_'), DateTime.Now);
            string dirPath = Path.Combine(Environment.CurrentDirectory, dir);
            if(!Directory.Exists(dirPath))
                Directory.CreateDirectory(dirPath);

            return dir;
        }

        private static void WriteDvnrReportFile(ReportFileType reportType, string dvnrFilePath, DvnrReport report)
        {
            try
            {
                string filePath = Path.Combine(Environment.CurrentDirectory, dvnrFilePath);
                File.Delete(filePath);

                using (var file = File.Open(filePath, FileMode.OpenOrCreate, FileAccess.Write))
                {
                    using (var sw = new StreamWriter(file))
                    {
                        if (reportType == ReportFileType.Xml) PrintReportXml(report, sw);
                        if (reportType == ReportFileType.Json) PrintReportJson(report, sw);
                        if (reportType == ReportFileType.Text) PrintReportText(report, sw);

                        Console.WriteLine("Report was written to: {0}", filePath);
                    }
                }
            }
            catch (Exception ex)
            {
                Trace.TraceError("Could not save report file. Error: {0}", ex.ToString());
                Console.WriteLine("Could not save report file");
            }
        }

        private static void WriteCompatSummary(DvnrReport report, string compatFilePath)
        {

            try
            {
                var summaryReport = SummaryReporting.GenerateCompatReport(report);
                List<string> connectionErrors = TeamServerConnectivityChecker.CheckForConnectionProblems(false);
                List<string> noCertConnectionErrors;
                if (connectionErrors.Count > 0)
                    noCertConnectionErrors = TeamServerConnectivityChecker.CheckForConnectionProblems(true);
                else
                    noCertConnectionErrors = new List<string>();

                var config = new TemplateServiceConfiguration();
                config.DisableTempFileLocking = true;
                config.Language = RazorEngine.Language.CSharp;
                config.EncodedStringFactory = new RazorEngine.Text.RawStringFactory();


                var templateService = RazorEngineService.Create(config);

                string filePath = Path.Combine(Environment.CurrentDirectory, compatFilePath);

                string templatePath = "MarkdownTemplate.txt";
                if(!File.Exists(templatePath))
                {
                    Console.WriteLine("Could not find compat test template at " + templatePath);
                    return;
                }
                string template = File.ReadAllText(templatePath);

                DynamicViewBag viewData = new DynamicViewBag();
                viewData.AddValue("ConnErrors", connectionErrors);
                viewData.AddValue("NoCertConnErrors", noCertConnectionErrors);
                
                var result =
                    templateService.RunCompile(template, "templateKey", null,
                    summaryReport, viewData);

                File.WriteAllText(filePath, result);
                Console.WriteLine("Compatibility report was written to " + filePath);
            }
            catch(Exception ex)
            {
                Trace.TraceError("Could not write compatibility report. Error: {0}", ex.ToString());
                Console.WriteLine("Error writing compatibility report");
            }

        }




        private static DvnrReport GenerateReportFromExisting(string fromFilename, ReportFileType reportType)
        {
            if (reportType == ReportFileType.Unknown || reportType == ReportFileType.Text)
                throw new ArgumentException("Unsupported report type", nameof(reportType));

            if (!File.Exists(fromFilename))
            {
                Trace.TraceError("Could not find specified from file at {0}", fromFilename);
                Console.WriteLine("Could not find specified from file at {0}", fromFilename);
                return null;
            }

            string dvnrReportText;
            using (var fileStream = File.Open(fromFilename, FileMode.Open, FileAccess.Read))
            {
                using (StreamReader streamReader = new StreamReader(fileStream, true))
                {
                    dvnrReportText = streamReader.ReadToEnd();
                }
            }

            if (reportType == ReportFileType.Xml)
            {
                Console.WriteLine("Parsing XML report from {0}.", fromFilename);
                return XmlUtils.XmlDeserializeFromString<DvnrReport>(dvnrReportText);
            }
            // TODO: support json file

            return null;
        }

        private static DvnrReport GenerateReportFromCurrentMachine()
        {
            Console.Out.WriteLine("Gathering machine information");
            var machineInformation = Reporting.GetMachineInformation();

            Console.Out.WriteLine("Gathering site information");
            var sites = Reporting.GetSites();

            Console.Out.WriteLine("Gathering app pool information");
            var appPools = Reporting.GetAppPools();
            // set number of apps using each pool and remove ones that aren't used
            foreach (var appPool in appPools)
            {
                appPool.NumApplications = sites.SelectMany(s => s.Applications).Select(s => s.AppPoolName).Count(name => name == appPool.Name);
            }
            appPools.RemoveAll(pool => pool.NumApplications == 0);

            Console.Out.WriteLine("Gathering GAC library information");
            var gacLibraries = Reporting.GetGACLibraries();

            var report = new DvnrReport
            {
                Version = typeof(DvnrReport).Assembly.GetName().Version.ToString(),
                MachineInformation = machineInformation,
                Sites = sites,
                AppPools = appPools,
                GacLibraries = gacLibraries
            };
            return report;
        }

        private static void PrintReportXml(DvnrReport report, TextWriter tw)
        {
            string reportXml = XmlUtils.XmlSerializeToString(report);
            tw.Write(reportXml);
        }

        private static void PrintReportJson(DvnrReport report, TextWriter tw)
        {
            DataContractJsonSerializer serializer = new DataContractJsonSerializer(report.GetType());

            using (var ms = new MemoryStream())
            {
                serializer.WriteObject(ms, report);
                string str = Encoding.Default.GetString(ms.ToArray());

                tw.Write(str);
            }
        }

        private static void PrintReportText(DvnrReport report, TextWriter tw)
        {

            var machineInfo = report.MachineInformation;
            var appPools = report.AppPools;
            var sites = report.Sites;
            var gacLibs = report.GacLibraries;

            string lineBreak = "----------------------------------------------------";
            string sectionBreak = "====================================================";


            tw.WriteLine(sectionBreak);
            tw.WriteLine("MACHINE INFORMATION");
            tw.WriteLine(sectionBreak);
            tw.WriteLine("OS: {0}", machineInfo.OsVersion);
            tw.WriteLine("OS Architecture: {0}", machineInfo.OsArchitecture);
            tw.WriteLine("Processor: {0}", machineInfo.ProcessorID);
            tw.WriteLine("Cores (Logical): {0}", machineInfo.ProcessorLogicalCores);
            tw.WriteLine("Cores (Physical): {0}", machineInfo.ProcessorPhysicalCores);
            tw.WriteLine("RAM: {0}", machineInfo.MemoryAvailable);
            tw.WriteLine("System Drive: {0}", machineInfo.SystemDrive);
            tw.WriteLine("IIS Installed: {0}", machineInfo.IsIISInstalled);
            if (machineInfo.IsIISInstalled)
                tw.WriteLine("IIS Version: {0}", machineInfo.IISVersion);
            tw.WriteLine("IIS Express Installed: {0}", machineInfo.IsIISExpressInstalled);
            if (machineInfo.IsIISExpressInstalled)
                tw.WriteLine("IIS Express Version: {0}", machineInfo.IISExpressVersion);
            tw.WriteLine(".NET Versions: {0}", string.Join(", ", machineInfo.DotnetFrameworkVersions));


            tw.WriteLine(sectionBreak);
            tw.WriteLine("APP POOLS");
            tw.WriteLine(sectionBreak);
            foreach (var appPool in appPools)
            {
                tw.WriteLine("Name: {0}", appPool.Name);
                tw.WriteLine("Pipeline Mode: {0}", appPool.PipelineMode);
                tw.WriteLine("64bit: {0}", appPool.X64);
                tw.WriteLine("Identity: {0}", appPool.Identity);
                tw.WriteLine("Username: {0}", appPool.UserName);
                tw.WriteLine("CLR Version: {0}", appPool.CLRVersion);
                tw.WriteLine(lineBreak);
            }

            tw.WriteLine(sectionBreak);
            tw.WriteLine("SITES");
            tw.WriteLine(sectionBreak);
            foreach (var site in sites)
            {
                tw.WriteLine("Name: {0}", site.Name);
                tw.WriteLine("Default AppPool: {0}", site.DefaultAppPoolName);
                tw.WriteLine(lineBreak);
                tw.WriteLine("{0} Applications ({1})", site.Name, site.Applications.Count);
                tw.WriteLine(lineBreak);
                foreach (var app in site.Applications)
                {
                    tw.WriteLine("Path: {0}", app.Path);
                    tw.WriteLine("Physical Path: {0}", app.PhysicalPath);
                    tw.WriteLine("AppPool: {0}", app.AppPoolName);
                    tw.WriteLine("Logon Method: {0}", app.AuthenticationLogonMethod);
                    tw.WriteLine("Protocols: {0}", app.EnabledProtocols);
                    tw.WriteLine("Enable Preload: {0}", app.EnablePreload);
                    tw.WriteLine("User: {0}", app.SpecificUser);
                    tw.WriteLine("Libraries ({0})", app.Libraries.Count);
                    foreach (var lib in app.Libraries)
                    {
                        tw.WriteLine("\tFilename: {0}", lib.Filename);
                        tw.WriteLine("\tName: {0}", lib.Name);
                        tw.WriteLine("\tProcessor Architecture: {0}", lib.ProcessorArchitecture);
                        tw.WriteLine("\tPublic Key Token: {0}", lib.PublicKeyToken);
                        tw.WriteLine("\tMD5 Hash: {0}", lib.Md5Hash);
                        tw.WriteLine("\tSHA1 Hash: {0}", lib.SHA1Hash);
                        tw.WriteLine("\tAssembly Version: {0}", lib.AssemblyVersion);
                        tw.WriteLine("\tFile Version: {0}", lib.Version);
                        tw.WriteLine("\tProduct Name: {0}", lib.ProductName);
                        tw.WriteLine("\tProduct Version: {0}", lib.ProductVersion);
                        tw.WriteLine("\tFile Description: {0}", lib.FileDescription);
                        tw.WriteLine("\tCopyright: {0}", lib.Copyright);

                        tw.WriteLine("\t" + lineBreak);
                    }
                    tw.WriteLine("HttpModules ({0})", app.Modules.Count);
                    foreach (var module in app.Modules)
                    {
                        tw.WriteLine("\tName: {0}", module.Name);
                        tw.WriteLine("\tType: {0}", module.Type);
                        tw.WriteLine("\t" + lineBreak);
                    }
                    tw.WriteLine(sectionBreak);
                }
                tw.WriteLine("{0} Bindings ({1})", site.Name, site.Bindings.Count);
                tw.WriteLine(sectionBreak);
                foreach (var binding in site.Bindings)
                {
                    tw.WriteLine("Protocol: {0}", binding.Protocol);
                    tw.WriteLine("Hostname: {0}", binding.Hostname);
                    tw.WriteLine("Port: {0}", binding.Port);
                    tw.WriteLine("IP Address: {0}", binding.IpAddress);
                    tw.WriteLine("Binding Information: {0}", binding.BindingInformation);
                    tw.WriteLine(lineBreak);
                }

                tw.WriteLine(sectionBreak);
                tw.WriteLine("GAC LIBRARIES ({0})", gacLibs.Count);
                tw.WriteLine(sectionBreak);

                foreach (var lib in gacLibs)
                {
                    tw.WriteLine("\tFilename: {0}", lib.Filename);
                    tw.WriteLine("\tName: {0}", lib.Name);
                    tw.WriteLine("\tProcessor Architecture: {0}", lib.ProcessorArchitecture);
                    tw.WriteLine("\tPublic Key Token: {0}", lib.PublicKeyToken);
                    tw.WriteLine("\tMD5 Hash: {0}", lib.Md5Hash);
                    tw.WriteLine("\tSHA1 Hash: {0}", lib.SHA1Hash);
                    tw.WriteLine("\tAssembly Version: {0}", lib.AssemblyVersion);
                    tw.WriteLine("\tFile Version: {0}", lib.Version);
                    tw.WriteLine("\tProduct Name: {0}", lib.ProductName);
                    tw.WriteLine("\tProduct Version: {0}", lib.ProductVersion);
                    tw.WriteLine("\tFile Description: {0}", lib.FileDescription);
                    tw.WriteLine("\tCopyright: {0}", lib.Copyright);

                    tw.WriteLine("\t" + lineBreak);
                }
            }
        }
    }
}
