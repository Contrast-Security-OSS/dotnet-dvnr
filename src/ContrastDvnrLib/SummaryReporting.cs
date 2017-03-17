using ContrastDvnrLib.Models;
using ContrastDvnrLib.Models.Report;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ContrastDvnrLib
{
    public class SummaryReporting
    {

        public static SummaryReport GenerateCompatReport(DvnrReport report)
        {
            var apps = report.Sites.SelectMany(s => s.Applications).OrderBy(a => a.Path).ToList();

            var appPoolsDict = report.AppPools.ToDictionary(a => a.Name);

            var appList = new List<ApplicationInfo>();
            foreach (var app in apps)
            {
                var site = report.Sites.Where(s => s.Applications.Contains(app)).SingleOrDefault();
                var appPool = appPoolsDict[app.AppPoolName];
                string bitness = appPool.X64 ? "64bit" : "32bit";

                appList.Add(new ApplicationInfo
                {
                    Path = app.Path,
                    AppPool = appPool.Name,
                    Site = site?.Name,
                    ClrVersion = appPool.CLRVersion,
                    Bitness = bitness,
                    NumLibs = app.Libraries.Count,
                    Pipeline = appPool.PipelineMode
                });
            }

            var poolList = new List<AppPoolInfo>();
            foreach (var appPool in report.AppPools.OrderBy(a => a.Name))
            {
                string bitness = appPool.X64 ? "64bit" : "32bit";
                var appsInPool = apps.Where(a => a.AppPoolName == appPool.Name).OrderBy(a => a.Path).Select(a => a.Path == "/" ? "/" : a.Path.TrimStart('/')).ToArray();
                string appsInPoolDisplay = string.Join(", ", appsInPool);
                string rating = CalcAppPoolRating(appPool);

                poolList.Add(new AppPoolInfo
                {
                    Name = appPool.Name,
                    NumApplications = appPool.NumApplications,
                    AppNames = appsInPoolDisplay,
                    Bitness = bitness,
                    ClrVersion = appPool.CLRVersion,
                    Pipeline = appPool.PipelineMode,
                    Rating = rating
                });
            }

            var summaryReport = new SummaryReport
            {
                Applications = appList,
                AppPools = poolList,
                GacLibraryIssues = report.GacLibraries.Where(l => l.Issue != null).ToList(),
                AppLibraryIssues = apps.Where(a => a.Libraries.Any(l => l.Issue != null))
                    .SelectMany(a => a.Libraries.Where(l => l.Issue != null).Select(l => new AppIssue { AppName = a.Path, Library = l }))
                    .ToList()
            };

            return summaryReport;
        }

        private static string CalcAppPoolRating(IISAppPool appPool)
        {
            int rating = 5;
            if (appPool.CLRVersion == "v2.0")
                rating -= 2;
            if (appPool.PipelineMode == "Classic")
                rating -= 1;
            if (!appPool.X64)
                rating -= 1;

            return new string('*', rating);
        }

    }
}
