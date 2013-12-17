using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Xml;
using Ionic.Zip;

namespace DbMigrator
{
    public class Sources
    {
        public static readonly string SCRIPTS_ARCHIVE = Path.Combine(Environment.CurrentDirectory, "db_scripts.zip");
        public static readonly string CHANGSETS_DIR = Path.Combine(Environment.CurrentDirectory, "changesets");
        public static readonly string CONFIG = Path.Combine(Environment.CurrentDirectory, "db.config");

        public static List<Migration> readMigrationsFromFolders()
        {
            if (!Directory.Exists(CHANGSETS_DIR))
                return null;

            var migrations = new List<Migration>();
            foreach (var scriptPath in Directory.GetFiles(CHANGSETS_DIR, "*.sql"))
            {
                var content = File.ReadAllText(scriptPath);
                migrations.Add(new Migration(Path.GetFileName(scriptPath), content));
            }

            return migrations;
        }

        public static List<Migration> readMigrationsFromArchive()
        {
            if (!File.Exists(SCRIPTS_ARCHIVE))
                return null;

            var migrations = new List<Migration>();
            using (var zip = ZipFile.Read(SCRIPTS_ARCHIVE))
                foreach (var script in zip)
                {
                    var content = readContent(script);

                    migrations.Add(new Migration(script.FileName, content));
                }

            return migrations;
        }

        private static string readContent(ZipEntry script)
        {
            var stream = new MemoryStream();
            script.Extract(stream);
            stream.Position = 0;
            return new StreamReader(stream).ReadToEnd();
        }

        public static string extractConnString(string type)
        {
            if (!File.Exists(CONFIG))
                throw new FileNotFoundException("DB config (" + CONFIG + ") is not found");

            try
            {
                var config = new XmlDocument();
                config.Load(CONFIG);

                var connStringNode = config.SelectSingleNode(string.Format("//db[@type='{0}']", type));
                if (connStringNode == null)
                    throw new FormatException("Connection string node is not found for type: " + type);
                if (connStringNode.Attributes == null)
                    throw new FormatException("Connection string attribute is not found for type: " + type);

                var connString = connStringNode.Attributes["connString"].InnerText;
                return connString;
            }
            catch (Exception ex)
            {
                throw new FormatException(CONFIG + " has invalid format", ex);
            }
        }

        public static string extractDbName(string connString)
        {
            var regex = new Regex(@"Data Source=([^;]+);");
            return regex.Match(connString).Groups[1].Value;
        }
    }
}
