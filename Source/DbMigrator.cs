using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace DbMigrator
{
    public class DbMigrator
    {
        private readonly IDbProvider _provider;
        private readonly List<Migration> _migrations;
        private readonly List<Migration> _appliedMigrations;

        public string ConnString
        {
            get { return _provider.ConnString; }
        }

        public DbMigrator(IDbProvider provider, IEnumerable<Migration> migrations)
        {
            _provider = provider;

            _migrations = new List<Migration>(migrations);
            _appliedMigrations = _provider.RetrieveAppliedMigrations();
        }

        public int Init(int skipUpTo)
        {
            if (_provider.DbInitialized)
                throw new ApplicationException("Database already initialized for migrations");

            _provider.CreateAppliedMigrationsTable();

            var migrationsToSkip = _migrations.Where(m => m.Num <= skipUpTo).ToList();
            foreach (var migration in migrationsToSkip)
            {
                _provider.RegisterMigration(migration);
                _appliedMigrations.Add(migration);
            }

            PrintStatus();

            return migrationsToSkip.Count;
        }

        public void Migrate()
        {
            if (_provider.DbInitialized == false)
                throw new ApplicationException("Migration table does not exist. Run \"init\" command first");

            var toBeApplied = _migrations.Where(m => !_appliedMigrations.Contains(m)).ToList();
            if (toBeApplied.Count == 0)
            {
                Console.WriteLine("Nothing to apply");
                return;
            }

            // TODO: warn on unknown and missing migrations


            foreach (var migration in toBeApplied.OrderBy(m => m.Num))
            {
                applyMigration(migration);
            }

            PrintStatus();
        }

        private void applyMigration(Migration migration)
        {
            var scripts = migration.Content.Split(new[] { "\n;\n", "\n;\r\n" }, StringSplitOptions.RemoveEmptyEntries);

            var applyingScript = "";
            try
            {
                foreach (var script in scripts)
                {
                    applyingScript = cleanupScript(script);
                    if (string.IsNullOrWhiteSpace(applyingScript))
                        continue;

                    if (applyingScript.ToLower().IndexOf("create or replace function") != -1 || applyingScript.ToLower().IndexOf("create or replace trigger") != -1 || applyingScript.ToLower().IndexOf("create or replace procedure") != -1)
                    {
                        // Ensure scripts have closing ";"
                        if (applyingScript.TrimEnd().LastIndexOf(';') < applyingScript.TrimEnd().Length - 5)
                            throw new ApplicationException("Function's and trigger's and procedure's scripts must to be closed with ';'");
                    }

                    _provider.ExecuteNonQuery(applyingScript);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error occurred applying script from migration {0}.\n\n"+
                                  "Error: {1}\n"+
                                  "Error part is: \n{2}\n", migration, ex.Message, applyingScript);

                throw new ApplicationException("Execution terminated");
            }

            _provider.RegisterMigration(migration);
            _appliedMigrations.Add(migration);
            Console.WriteLine("Script {0} applied successful", migration);
        }


        private static readonly Regex REMOVE_COMMENTS_AT_LINE_START = new Regex("^-- .*$", RegexOptions.Multiline);
        private static string cleanupScript(string script)
        {
            script = REMOVE_COMMENTS_AT_LINE_START.Replace(script, "");

            // remove standalone "/" characters (plsql's execute now character)
            script = script.Replace("\n/\n", "\n").Replace("\n/\r\n", "\n");

            // remove '/r' chars, Oracle doesn't like them
            script = script.Replace("\r", "");
            
            return script;
        }

        public void PrintStatus()
        {
            var status = string.Format("Status: all ({0}), applied ({1})", _migrations.Count, _appliedMigrations.Count);

            Console.WriteLine(status);
        }
    }
}
