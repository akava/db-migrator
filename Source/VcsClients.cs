using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Xml;

namespace DbMigrator
{
    public interface IVcsClient
    {
        string GetRevisionFor(string scriptPath);
    }

    public class VcsClients
    {
        public static IVcsClient CreateForDir(string path)
        {
            var client = new SvnClient(path);
            if (client.IsVcsDir == false)
                return null;

            return client;
        }
    }

    public class SvnClient: IVcsClient
    {
        public bool IsVcsDir { get { return (_revisionsMap != null); } }
        private readonly Dictionary<string, string> _revisionsMap;

        public SvnClient(string path)
        {
            try
            {
                var svnOutput = runSvnList(path);
                if (svnOutput.Contains("svn: E155007"))
                    throw new ApplicationException(path +" is not a svn working copy");

                _revisionsMap = readRevisions(svnOutput);
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        private static string runSvnList(string path)
        {
            var process = new Process();
            var startInfo = new ProcessStartInfo
            {
                WindowStyle = ProcessWindowStyle.Hidden,
                FileName = "svn",
                Arguments = @"list --xml -R " + path,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                CreateNoWindow = true,
            };
            process.StartInfo = startInfo;

            var result = new StringBuilder();
            process.Start();
            while (!process.StandardOutput.EndOfStream)
            {
                result.AppendLine(process.StandardOutput.ReadLine());
            }
            return result.ToString();
        }

        private Dictionary<string, string> readRevisions(string xml)
        {
            var xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(xml);

            var map = new Dictionary<string, string>();
            foreach (XmlNode entry in xmlDoc.SelectNodes("//entry[@kind='file']"))
            {
                var name = entry.FirstChild.InnerText;
                var revision = entry.SelectSingleNode("commit/@revision").InnerText;

                map.Add(name, revision);
            }

            return map;
        }

        public string GetRevisionFor(string path)
        {
            return _revisionsMap[path];
        }
    }
}
