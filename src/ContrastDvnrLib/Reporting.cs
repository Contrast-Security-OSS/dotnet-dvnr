using ContrastDvnrLib.Models;
using Microsoft.Web.Administration;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Xml.Linq;
using System.Xml.XPath;
using GACManagerApi;
using System.Management;
using System.Linq;
using Microsoft.Win32;

namespace ContrastDvnrLib
{
    public static class Reporting
    {

        public static List<IISSite> GetSites()
        {
            var siteList = new List<IISSite>();
            
            using (var serverManager = new ServerManager())
            {

                foreach (var smSite in serverManager.Sites)
                {
                    try
                    {
                        var site = new IISSite
                        {
                            Name = smSite.Name,
                            Bindings = new List<IISSiteBinding>(),
                            Applications = new List<IISApplication>(),
                            DefaultAppPoolName = smSite.ApplicationDefaults.ApplicationPoolName
                        };
                        foreach (var smBinding in smSite.Bindings)
                        {
                            var binding = new IISSiteBinding
                            {
                                BindingInformation = smBinding.BindingInformation,
                                Protocol = smBinding.Protocol,
                                Hostname = smBinding.Host,
                                IpAddress = smBinding?.EndPoint?.Address?.ToString()?.Replace("0.0.0.0", "*") ?? "",
                                Port = smBinding?.EndPoint?.Port ?? null

                            };
                            site.Bindings.Add(binding);
                        }
                        foreach (var smApp in smSite.Applications)
                        {
                            var app = PopulateApplication(smApp);
                            if (app != null)
                                site.Applications.Add(app);
                        }
                        siteList.Add(site);
                    }
                    catch(Exception ex)
                    {
                        Trace.TraceError("Error getting site information. Error: {0}", ex.ToString());
                        Console.Error.WriteLine("Error getting site information for site {0}", ex.ToString());
                    }
                }

            }
            return siteList;
        }

        public static bool PreFlightCheck()
        {
            string iisPath = Environment.ExpandEnvironmentVariables(@"%SystemRoot%\system32\inetsrv\InetMgr.exe");
            if (!File.Exists(iisPath))
            {
                Trace.TraceInformation("IIS not found.  Program will exit.");
                Console.Error.WriteLine("IIS 7+ not found.  Please ensure IIS is installed and at least one site is defined and try again");
                return false;
            }
            else
            {
                string iisVersionString = FileVersionInfo.GetVersionInfo(iisPath)?.ProductVersion;
                var iisVersion = Convert.ToInt32(iisVersionString.Substring(0, iisVersionString.IndexOf(".")));
                if(iisVersion<7)
                {
                    Trace.TraceInformation("IIS 7+ not found.  Program will exit");
                    Console.Error.WriteLine("An older version was IIS was found.  Only IIS 7+ is supported.");
                    return false;
                }
            }

            try
            {
                var serverManager = new ServerManager();
                return serverManager.Sites.Count > 0;
            }
            catch(Exception ex)
            {
                Trace.TraceError("Could not access IIS Metadata. Error: {0}", ex.ToString());
                Console.Error.WriteLine("Could not access IIS metadata");
                return false;
            }
           
        }

        public static MachineInformation GetMachineInformation()
        {
            
            var machineInformation = new MachineInformation();

            var wmi = new ManagementObjectSearcher("select * from Win32_OperatingSystem")
                ?.Get()
                ?.Cast<ManagementObject>()
                ?.First();

            if (wmi != null)
            {
                machineInformation.OsVersion = wmi["Caption"] as string;
                machineInformation.SystemDrive = wmi["SystemDrive"] as string;
                machineInformation.OsArchitecture = wmi["OSArchitecture"] as string;
                if (wmi["TotalVisibleMemorySize"] != null)
                {
                    UInt64 totalMemory = (UInt64?)wmi["TotalVisibleMemorySize"] ?? 0;
                    machineInformation.MemoryAvailable = (long)totalMemory;
                }
            }
            else
            {
                Trace.TraceWarning("Could not retrieve operating system information");
                Console.Error.WriteLine("Could not retrieve operating system information");
            }

            var cpu = new ManagementObjectSearcher("select * from Win32_Processor")
                ?.Get()
                ?.Cast<ManagementObject>()
                ?.First();
            if (cpu != null)
            {
                machineInformation.ProcessorID = cpu["Name"] as string;
                machineInformation.ProcessorPhysicalCores = (uint?)cpu["NumberOfCores"] ?? 0;
                machineInformation.ProcessorLogicalCores = (uint?)cpu["NumberOfLogicalProcessors"] ?? 0;
            }
            else
            {
                Trace.TraceWarning("Could not retrieve processor information");
                Console.Error.WriteLine("Could not retrieve processor information");
            }

            string iisPath = Environment.ExpandEnvironmentVariables(@"%SystemRoot%\system32\inetsrv\InetMgr.exe");
            if (File.Exists(iisPath))
            {
                machineInformation.IsIISInstalled = true;
                machineInformation.IISVersion = FileVersionInfo.GetVersionInfo(iisPath)?.ProductVersion;
            }

            string[] expressPaths = {
                Environment.ExpandEnvironmentVariables(@"%PROGRAMFILES%\IIS Express\iisexpress.exe"),
                Environment.ExpandEnvironmentVariables(@"%PROGRAMFILES(X86)%\IIS Express\iisexpress.exe")
            };

            foreach(var expressPath in expressPaths)
            {
                if(File.Exists(expressPath))
                {
                    machineInformation.IsIISExpressInstalled = true;
                    machineInformation.IISExpressVersion = FileVersionInfo.GetVersionInfo(expressPath)?.ProductVersion;
                }
            }


            machineInformation.DotnetFrameworkVersions = GetDotNetVersionsFromRegistry();

            return machineInformation;
        }

        private static List<string> GetDotNetVersionsFromRegistry()
        {
            List<string> versionList = new List<string>();
            RegistryKey ndpKey;

            try
            {
                 ndpKey =
                    RegistryKey.OpenRemoteBaseKey(RegistryHive.LocalMachine, "")?.
                    OpenSubKey(@"SOFTWARE\Microsoft\NET Framework Setup\NDP\");
            }
            catch (Exception ex) when (ex is ArgumentNullException || ex is ObjectDisposedException || ex is System.Security.SecurityException)
            {
                Trace.TraceError("Could not access registry for .NET version installed. Error: {0}", ex.ToString());
                Console.Error.WriteLine("Could not access registry for .NET version installed");
                return new List<string>();
            }

            if (ndpKey == null)
                return new List<string>();
            try
            {
                // Opens the registry key for the .NET Framework entry.
                using (ndpKey)
                {
                    // As an alternative, if you know the computers you will query are running .NET Framework 4.5 
                    // or later, you can use:
                    // using (RegistryKey ndpKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, 
                    // RegistryView.Registry32).OpenSubKey(@"SOFTWARE\Microsoft\NET Framework Setup\NDP\"))
                    foreach (string versionKeyName in ndpKey.GetSubKeyNames())
                    {
                        string version = null;
                        if (versionKeyName.StartsWith("v"))
                        {

                            RegistryKey versionKey = ndpKey.OpenSubKey(versionKeyName);
                            string name = (string)versionKey.GetValue("Version", "");
                            string sp = versionKey.GetValue("SP", "").ToString();
                            string install = versionKey.GetValue("Install", "").ToString();
                            if (install == "") //no install info, must be later.
                                version = versionKeyName + " " + name;
                            else
                            {
                                if (sp != "" && install == "1")
                                {
                                    version = versionKeyName + ", " + name + " SP" + sp;
                                }

                            }
                            version = version.TrimEnd();
                            if (name != "" && !string.IsNullOrEmpty(version))
                            {
                                versionList.Add(version); continue;
                            }
                            else
                            {
                                foreach (string subKeyName in versionKey.GetSubKeyNames())
                                {
                                    string subVersion = version;
                                    RegistryKey subKey = versionKey.OpenSubKey(subKeyName);
                                    name = (string)subKey.GetValue("Version", "");
                                    if (name != "")
                                        sp = subKey.GetValue("SP", "").ToString();
                                    install = subKey.GetValue("Install", "").ToString();
                                    if (install == "") //no install info, must be later.
                                        subVersion += " " + versionKeyName + " " + name;
                                    else
                                    {
                                        if (sp != "" && install == "1")
                                        {
                                            subVersion += ", " + subKeyName + " " + name + " SP" + sp;
                                        }
                                        else if (install == "1")
                                        {
                                            subVersion += ", " + subKeyName + " " + name;
                                        }
                                    }
                                    versionList.Add(subVersion);
                                }
                            }
                        }
                    }
                }
            }
            catch(Exception ex)
            {
                Trace.TraceError("Could not retrieve .NET framework versions from registry.  Error: {0}", ex.ToString());
            }
            return versionList;
        }

        private static IISApplication PopulateApplication(Application smApp)
        {
            var app = new IISApplication();
            app.AppPoolName = smApp.ApplicationPoolName;
            app.Path = smApp.Path;
            var attributes = smApp.Attributes;
            if(attributes["preloadEnabled"]!=null && attributes["preloadEnabled"].Value != null && attributes["preloadEnabled"].Value is bool)
            {
                app.EnablePreload = (bool)attributes["preloadEnabled"].Value;
            }
            app.EnabledProtocols = attributes["enabledProtocols"]?.Value as string;
            if (smApp.VirtualDirectories.Count >= 1)
            {
                app.PhysicalPath = Environment.ExpandEnvironmentVariables(smApp.VirtualDirectories[0].PhysicalPath);
                app.SpecificUser = smApp.VirtualDirectories[0].UserName;
                app.AuthenticationLogonMethod = Enum.GetName(typeof(AuthenticationLogonMethod), smApp.VirtualDirectories[0].LogonMethod);
            }
            app.Libraries = GetLibsForApp(app.PhysicalPath);
            app.Modules = GetModulesForApp(app.PhysicalPath);

            return app;
        }

        private static List<HttpModule> GetModulesForApp(string appPath)
        {
            var moduleList = new List<HttpModule>();

            string webConfigPath = Path.Combine(appPath, "web.config");
            if (File.Exists(webConfigPath))
            {

                XDocument doc;
                try
                {
                    doc = XDocument.Load(webConfigPath);
                }
                catch(Exception ex)
                {
                    Trace.TraceError("Could not load web config file.  Error: {0}", ex.ToString());
                    Console.Error.Write("Could not load web config file at {0}", webConfigPath);
                    return moduleList;
                }

                XElement[] moduleElements = {
                    doc.XPathSelectElement("/configuration/system.web/httpModules"),
                    doc.XPathSelectElement("/configuration/system.webServer/modules")
                };

                foreach (var modElem in moduleElements.Elements("add"))
                {
                    string name = modElem?.Attribute("name")?.Value;
                    string type = modElem?.Attribute("type")?.Value;
                    if(!string.IsNullOrEmpty(name) && !string.IsNullOrEmpty(type))
                    {
                        moduleList.Add(new HttpModule
                        {
                            Name = name,
                            Type = type
                        });
                    }
                }
            }

            return moduleList;
        }

        private static List<DotnetLibrary> GetLibsForApp(string appPath)
        {
            var libs = new List<DotnetLibrary>();

            DirectoryInfo di = new DirectoryInfo(Path.Combine(appPath, "bin"));

            if (di.Exists)
            {
                foreach (FileInfo fi in di.GetFiles("*.dll"))
                {
                    if (!fi.Exists)
                        continue;

                    var lib = new DotnetLibrary();
                    lib.Filepath = fi.FullName;
                    lib.Filename = fi.Name;

                    FileVersionInfo fvi = FileVersionInfo.GetVersionInfo(lib.Filepath);
                    bool isDllFile = fvi.FileVersion != null;

                    if (!isDllFile)
                        continue;

                    lib.FileDescription = fvi.FileDescription;
                    lib.Version = fvi.FileVersion;
                    lib.ProductName = fvi.ProductName;
                    lib.ProductVersion = fvi.ProductVersion;
                    lib.Copyright = fvi.LegalCopyright;
                    lib.Language = fvi.Language;
                    lib.SHA1Hash = GetFileSHA1(lib.Filepath);
                    lib.Md5Hash = GetFileMD5(lib.Filepath);


                    //get .net metadata
                    try
                    {
                        var assembly = Assembly.LoadFile(lib.Filepath);
                        var assemblyName = assembly.GetName();
                        lib.Name = assemblyName.Name;

                        lib.Culture = assemblyName.CultureInfo.Name;
                        lib.Version = assemblyName.Version.ToString();
                        lib.ProcessorArchitecture = Enum.GetName(typeof(ProcessorArchitecture), assemblyName.ProcessorArchitecture);

                        var bytes = assembly.GetName().GetPublicKeyToken();
                        if (bytes == null || bytes.Length == 0)
                        {
                            lib.PublicKeyToken = "None";
                        }
                        else
                        {
                            string publicKeyToken = string.Empty;
                            for (int i = 0; i < bytes.GetLength(0); i++)
                                publicKeyToken += string.Format("{0:x2}", bytes[i]);
                            lib.PublicKeyToken = publicKeyToken;
                        }
                        lib.AssemblyVersion = assemblyName.Version;

                    }
                    catch(Exception ex) when (ex is BadImageFormatException || ex is FileNotFoundException)
                    {
                        Trace.TraceWarning("Could not load assembly at {0}. Error: {1}", lib.Filepath, ex.ToString());
                        Console.Error.WriteLine("Could not load assembly info for {0}", lib.Filepath);
                        continue;
                    }
                    libs.Add(lib);
                }
            }


            return libs;
        }

        private static string GetFileMD5(string filepath)
        {
            using (var md5 = new MD5CryptoServiceProvider())
            {
                return GetHash(filepath, md5);
            }
        }

        private static string GetHash(string filepath, HashAlgorithm hashAlgorithm)
        {
            try
            {
                using (FileStream fs = new FileStream(filepath, FileMode.Open, FileAccess.Read))
                using (BufferedStream bs = new BufferedStream(fs))
                {

                    byte[] hash = hashAlgorithm.ComputeHash(bs);
                    StringBuilder formatted = new StringBuilder(2 * hash.Length);
                    foreach (byte b in hash)
                    {
                        formatted.AppendFormat("{0:X2}", b);
                    }
                    return formatted.ToString();

                }
            }
            catch (Exception)
            {
                Trace.TraceWarning("Could not generate hash for {0}", filepath);
                return string.Empty;
            }
        }

        private static string GetFileSHA1(string filepath)
        {
            using (var sha1 = new SHA1Managed())
            {
                return GetHash(filepath, sha1);
            }
        }

        public static List<IISAppPool> GetAppPools()
        {
            var poolList = new List<IISAppPool>();

            using (var serverManager = new ServerManager())
            {
                try
                {
                    foreach (var smAppPool in serverManager.ApplicationPools)
                    {
                        try
                        {
                            var appPool = new IISAppPool
                            {
                                Name = smAppPool.Name,
                                X64 = !smAppPool.Enable32BitAppOnWin64,
                                CLRVersion = smAppPool.ManagedRuntimeVersion,
                                Identity = smAppPool.ProcessModel.IdentityType.ToString(),
                                UserName = smAppPool.ProcessModel.UserName,
                                PipelineMode = Enum.GetName(typeof(ManagedPipelineMode), smAppPool.ManagedPipelineMode)
                            };

                            if (string.IsNullOrEmpty(appPool.UserName))
                            {
                                if (appPool.Identity == "ApplicationPoolIdentity")
                                    appPool.UserName = appPool.Name;
                                else if (appPool.Identity == "LocalSystem")
                                    appPool.UserName = "System";
                                else if (appPool.Identity == "LocalService")
                                    appPool.UserName = "Local Service";
                                else if (appPool.Identity == "NetworkService")
                                    appPool.UserName = Environment.MachineName + "$";
                            }

                            poolList.Add(appPool);
                        }
                        catch(Exception ex)
                        {
                            Trace.TraceError("Could not retrieve app pool information. Error: {0}", ex.ToString());
                            Console.Error.WriteLine("Could not retrieve app pool information for {0} ", smAppPool.Name);
                        }
                    }
                }
                catch(Exception ex)
                {
                    Trace.TraceError("Error in getting app pool information. Error: {0}", ex.ToString());
                    Console.Error.WriteLine("Could not retrieve app pool information.");
                }
            }

            return poolList;
        }

        public static List<DotnetLibrary> GetGACLibraries()
        {
            var gacList = new List<DotnetLibrary>();

            try
            {
                var assemblyEnumerator = new AssemblyCacheEnumerator();

                var assemblyName = assemblyEnumerator.GetNextAssembly();

                while (assemblyName != null)
                {

                    var assemblyDescription = new AssemblyDescription(assemblyName);

                    string name = assemblyDescription.Name;
                    bool probablyMicrosoftPackage = (name.StartsWith("Microsoft") || name.StartsWith("System"));
                    if (!probablyMicrosoftPackage)
                    {
                        var gacLib = new DotnetLibrary
                        {
                            Culture = assemblyDescription.Culture,
                            ProcessorArchitecture = assemblyDescription.ProcessorArchitecture,
                            Name = name,
                            Version = assemblyDescription.Version,
                            Filepath = assemblyDescription.Path,

                        };
                        FileInfo fi = new FileInfo(gacLib.Filepath);
                        if (fi.Exists)
                        {

                            gacLib.Filename = fi.Name;
                            FileVersionInfo fvi = FileVersionInfo.GetVersionInfo(gacLib.Filepath);
                            bool isDllFile = fvi.FileVersion != null;
                            bool isMicrosoftCopyright = fvi.LegalCopyright != null && fvi.LegalCopyright.Contains("Microsoft Corporation");

                            if (isDllFile && !isMicrosoftCopyright)
                            {
                                gacLib.FileDescription = fvi.FileDescription;
                                gacLib.Version = fvi.FileVersion;
                                gacLib.ProductName = fvi.ProductName;
                                gacLib.ProductVersion = fvi.ProductVersion;
                                gacLib.Copyright = fvi.LegalCopyright;
                                gacLib.Language = fvi.Language;
                                gacLib.SHA1Hash = GetFileSHA1(gacLib.Filepath);
                                gacLib.Md5Hash = GetFileMD5(gacLib.Filepath);

                                gacList.Add(gacLib);
                            }
                        }
                    }

                    assemblyName = assemblyEnumerator.GetNextAssembly();
                }
            }
            catch(Exception ex)
            {
                Trace.TraceWarning("Could not load DLL list from the GAC. Error: {0}", ex.ToString());
                Console.Error.WriteLine("Could not load DLL list from the GAC.");
                return new List<DotnetLibrary>();
            }

            return gacList;
        }

    }
}
