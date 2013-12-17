using System;
using System.Collections.Generic;
using System.Data.OracleClient;
using System.Linq;
using System.Text.RegularExpressions;
using Ionic.Zip;

#pragma warning disable 612,618
namespace DbMigrator
{
    public class DbMigrator
    {
        private const string SELECT_APPLIED_MIGRATIONS_SQL = @"select NUM, NAME, APPLIED_DATE from  APPLIED_MIGRATIONS";

        private const string CREATE_APPLIED_MIGRATIONS_TABLE_SQL = @"create table APPLIED_MIGRATIONS (
                                                                      NUM int,
                                                                      NAME varchar(100),
                                                                      APPLIED_DATE timestamp(6)
                                                                    )";

        private const string INSERT_MIGRATION_SQL = @"insert into applied_migrations(NUM, NAME, APPLIED_DATE) 
                                                        values (:num, :name, :applied_date)";
        private const int TABLE_DOES_NOT_EXIST = 942;


        private readonly string _connString;
        private readonly List<Migration> _migrations;
        private readonly List<Migration> _appliedMigrations;
        private readonly bool _dbInitialized = true;

        public string ConnString
        {
            get { return _connString; }
        }

        public DbMigrator(string connString, IEnumerable<Migration> migrations)
        {
            _connString = connString;
            _migrations = new List<Migration>(migrations);

            try
            {
                _appliedMigrations = retrieveAppliedMigrations();
            }
            catch (OracleException ex)
            {
                if (ex.Code == TABLE_DOES_NOT_EXIST)
                {
                    _dbInitialized = false;
                    _appliedMigrations = new List<Migration>();
                    return;
                }

                throw;
            }
        }

        public int Init(int skipUpTo)
        {
            if (_dbInitialized)
                throw new BadStateException("Database already initialized for migrations");

            createAppliedMigrationsTable();

            var migrationsToSkip = _migrations.Where(m => m.Num <= skipUpTo).ToList();
            foreach (var migration in migrationsToSkip)
            {
                registerMigration(migration);
            }

            PrintStatus();

            return migrationsToSkip.Count;
        }

        public void Migrate()
        {
            if (_dbInitialized == false)
                throw new BadStateException("Migration table does not exist. Run \"init\" command first");

            var toBeApplied = _migrations.Where(m => !_appliedMigrations.Contains(m)).ToList();
            if (!toBeApplied.Any())
            {
                Console.WriteLine("Nothing to apply");
                return ;
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

                    executeNonQuery(applyingScript);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error occurred applying script from migration {0}.\n\n"+
                                  "Error: {1}\n"+
                                  "Error part is: \n{2}\n", migration, ex.Message, applyingScript);

                throw new ApplicationException("Execution terminated");
            }

            Console.WriteLine("Script {0} applied successful", migration);
            registerMigration(migration);
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

        private List<Migration> retrieveAppliedMigrations()
        {
            var appliedMigrations = new List<Migration>();

            using (var conn = new OracleConnection(_connString))
            using (var command = conn.CreateCommand())
            {
                command.CommandText = SELECT_APPLIED_MIGRATIONS_SQL;
                conn.Open();

                using (var reader = command.ExecuteReader())
                    while (reader.Read())
                        appliedMigrations.Add(
                            new Migration(reader.GetInt32(0), reader.GetString(1), reader.GetDateTime(2)));
            }
            
            return appliedMigrations;
        }

        private void createAppliedMigrationsTable()
        {
            executeNonQuery(CREATE_APPLIED_MIGRATIONS_TABLE_SQL);
        }

        private void registerMigration(Migration migration)
        {
            using (var conn = new OracleConnection(_connString))
            using (var command = conn.CreateCommand())
            {
                command.CommandText = INSERT_MIGRATION_SQL;
                command.Parameters.AddWithValue(":num", migration.Num);
                command.Parameters.AddWithValue(":name", migration.Name);
                command.Parameters.AddWithValue(":applied_date", DateTime.Now);

                conn.Open();
                command.ExecuteNonQuery();
            }
            _appliedMigrations.Add(migration);
        }

        private void executeNonQuery(string sql)
        {
            using (var conn = new OracleConnection(_connString))
            using (var command = conn.CreateCommand())
            {
                command.CommandText = sql;
                conn.Open();

                command.ExecuteNonQuery();
            }
        }
    }
}
