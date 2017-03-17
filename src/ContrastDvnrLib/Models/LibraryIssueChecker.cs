using System;

namespace ContrastDvnrLib.Models
{

    public class LibraryIssueChecker
    {
        private const string unsupportedFrameworkMessage = "{0} web framework is not currently supported.  Assess rule violations may not be detected.  Defend mode will only work when the application is deployed on IIS with ASP.NET.";

        public static LibraryIssue GetIssue(DotnetLibrary library)
        {
            if (library.Name == "Ninject")
            {
                Version ninjectVersion = new Version(library.ProductVersion ?? library.Version);
                Version validVersion = new Version("3.2.2");
                if(ninjectVersion.CompareTo(validVersion) <= 0)
                    return new LibraryIssue("Ninject 3.2.2 and below have a known bug that causes a crash with .NET profilers, including Contrast.  You must upgrade your version of Ninject or use a known workaround.  More information available on our documentation at https://docs.contrastsecurity.com/user_netfaq.html#ninject");
            }
            else if (library.Name == "ServiceStack")
            {
                return new LibraryIssue(string.Format(unsupportedFrameworkMessage, "ServiceStack"));
            }
            else if (library.Name == "Nancy")
            {
                return new LibraryIssue(string.Format(unsupportedFrameworkMessage, "Nancy"));
            }
            else if (library.Name == "Castle.Monorail")
            {
                return new LibraryIssue(string.Format(unsupportedFrameworkMessage, "Castle.Monorail"));
            }
            else if (library.Name.StartsWith("Microsoft.AspNet.SignalR", StringComparison.InvariantCulture))
            {
                return new LibraryIssue("SignalR framework is not currently supported for Assess or Defend rules.");
            }


            return null;
        }
    }
}