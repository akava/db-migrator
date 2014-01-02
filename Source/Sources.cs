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
        public static readonly string OBJECTS_DIR = Path.Combine(Environment.CurrentDirectory, "objects");
        public static readonly string CONFIG = Path.Combine(Environment.CurrentDirectory, "db.config");

        public static List<Migration> readMigrationsFromFolders()
        {
            var migrations = new List<Migration>();

            var changesets = readChangesetsFromFolder();
            if (changesets != null)
                migrations.AddRange(changesets);

            var objectMigrations = readObjectsFromFolder();
            if (objectMigrations != null)
                migrations.AddRange(objectMigrations);

            return migrations;
        }

        private static List<Migration> readChangesetsFromFolder()
        {
            if (!Directory.Exists(CHANGSETS_DIR))
                return null;

            var changesets = new List<Migration>();
            foreach (var scriptPath in Directory.GetFiles(CHANGSETS_DIR, "*.sql"))
            {
                var fileName = Path.GetFileNameWithoutExtension(scriptPath);
                var content = File.ReadAllText(scriptPath);

                try
                {
                    changesets.Add(Migration.MakeChangeset(int.Parse(fileName.Substring(0, 4)), fileName.Substring(4 + 1), content));
                }
                catch (Exception ex)
                {
                    throw new ArgumentException(string.Format(
                        "Filename has invalid format. Given: {0} Expected: '0000_text_text_text.sql'. Inner message: {1}",
                        fileName, ex.Message), ex);
                }
            }

            return changesets;
        }

        private static List<Migration> readObjectsFromFolder()
        {
            if (!Directory.Exists(OBJECTS_DIR))
                return null;

            var vcsClient = VcsClients.CreateForDir(OBJECTS_DIR);
            if (vcsClient == null)
                throw new ApplicationException(
                    "Objects dir is exist but not stored under source countrol (svn is supported only)");

            var migrations = new List<Migration>();
            foreach (var scriptPath in Directory.GetFiles(OBJECTS_DIR, "*.sql", SearchOption.AllDirectories))
            {
                var content = File.ReadAllText(scriptPath);
                var revision = vcsClient.GetRevisionFor(scriptPath.Replace(OBJECTS_DIR + "\\", ""));

                migrations.Add(Migration.MakeObjectMigration(revision, Path.GetFileName(scriptPath), content));
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
                    try
                    {
                        var num = int.Parse(script.FileName.Substring(0, 4));
                        var name = script.FileName.Substring(4 + 1).Split('.')[0];
                        var content = readContent(script);
                        migrations.Add(Migration.MakeChangeset(num, name, content));
                    }
                    catch (Exception ex)
                    {
                        throw new ArgumentException(string.Format(
                            "Filename has invalid format. Given: {0} Expected: '0000_text_text_text.sql'. Inner message: {1}",
                            script.FileName, ex.Message), ex);
                    }
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

        public static string extractConnString(string env)
        {
            if (!File.Exists(CONFIG))
                throw new FileNotFoundException("DB config (" + CONFIG + ") is not found");

            try
            {
                var config = new XmlDocument();
                config.Load(CONFIG);

                var connStringNode = config.SelectSingleNode(string.Format("//db[@env='{0}']", env));
                if (connStringNode == null)
                    throw new FormatException("Connection string node is not found for type: " + env);
                if (connStringNode.Attributes == null)
                    throw new FormatException("Connection string attribute is not found for type: " + env);

                var connString = connStringNode.Attributes["connString"].InnerText;
                return connString;
            }
            catch (Exception ex)
            {
                throw new FormatException(CONFIG + " has invalid format", ex);
            }
        }

        public static string extractProviderType()
        {
            if (!File.Exists(CONFIG))
                throw new FileNotFoundException("DB config (" + CONFIG + ") is not found");

            try
            {
                var config = new XmlDocument();
                config.Load(CONFIG);

                var root = config.DocumentElement;
                var connString = root.Attributes["type"].InnerText;
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
