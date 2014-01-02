using System;
using System.Diagnostics;
using System.Linq;

namespace DbMigrator
{
    class Program
    {
        private static string _env = "dev";
        static void Main(params string[] args)
        {
            try
            {
                if (args.Any(a => a.ToLower() == "--debug"))
                {
                    args = args.Where(a => a.ToLower() != "--debug").ToArray();
                    Debugger.Launch();
                }

                if (args.Length == 0 || args[0].ToLower() == "--help" || args[0].ToLower() == "-h")
                {
                    printHelp();
                    return;
                }

                if (!args[0].StartsWith("-"))
                {
                    _env = args[0];
                    args = args.Skip(1).ToArray();
                }
                Console.WriteLine("Environment is set to '"+ _env + "'");

                var command = args[0].ToLower();
                if (command == "--status" || command == "-s")
                {
                    printStatus();
                    return;
                }

                if (command == "--migrate" || command == "-m")
                {
                    migrate();
                    return;
                }

                if (command == "--init" || command == "-i")
                {
                    var skipUpTo = 0;
                    if (args.Length == 3 && (args[1].ToLower() == "--skipupto" || args[1].ToLower() == "-sut"))
                        if (!int.TryParse(args[2], out skipUpTo))
                            throw new ArgumentException("SkipUpTo parameter must be a number");

                    init(skipUpTo);
                    return;
                }

                printHelp();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Console.WriteLine();
                printHelp();
            }
//            finally
//            {
//                Console.ReadLine();
//            }
        }

        private static void init(int skipUpTo)
        {
            var migrator = createMigrator();
            var runMigrations = migrator.Init(skipUpTo);

            var dbName = Sources.extractDbName(migrator.ConnString);
            Console.WriteLine("DB '{0}' successfully migrated for migrations. {1} scripts have been registered", dbName ,runMigrations);
        }

        private static void migrate()
        {
            var migrator = createMigrator();
            migrator.Migrate();
        }

        private static void printStatus()
        {
            var migrator = createMigrator();
            migrator.PrintStatus();
        }

        private static DbMigrator createMigrator()
        {
            var migrations = Sources.readMigrationsFromArchive() 
                             ?? Sources.readMigrationsFromFolders();
            if (migrations == null)
                throw new ApplicationException("Migrations are not found");

            var dbType = Sources.extractProviderType();
            var connString = Sources.extractConnString(_env);
            var provider = DbProviders.Create(dbType, connString);

            return new DbMigrator(provider, migrations);
        }

        private static void printHelp()
        {
            Console.WriteLine(
@"[env]         sets current environmend to env, default is 'dev'
-m --migrate    migrates the db to the latest version
-i --init       initialises the db for the migration process 
                (param is --skipUpTo N,-sut N)
-s --status     prints the db migration status
-h --help       prints this help

Use 'dbMigrator [env] --migrate' to upgrade your database to the latest version.
Use 'dbMigrator [env] --init' to initialize the DB for migrations.
Use 'dbMigrator [env] --init --skipupto N' to initialize the DB for migrations 
and mark scripts which number less or equal than N as already applied.

Connection string is read from {0} file. 
Update scripts are read from {1} zipped archive or {2} folder.",
                Sources.CONFIG, Sources.SCRIPTS_ARCHIVE, Sources.CHANGSETS_DIR);
        }
    }
}
