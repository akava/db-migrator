using System;
using System.Collections.Generic;
using System.Data.OracleClient;
using System.Data.SqlClient;

namespace DbMigrator
{
    public static class DbProviders
    {
        public static IDbProvider Create(string dbType, string connString)
        {
            switch (dbType.ToLower())
            {
                case "oracle":
                    return new OracleProvider(connString);
                case "mssql":
                    return new MssqlProvider(connString);                
                default: 
                    throw new ArgumentException(string.Format(
                            "Db type must be 'oracle' or 'mssql' but '{0}' provided.", dbType));
            }
        }
    }

    public interface IDbProvider
    {
        bool DbInitialized { get; }
        string ConnString { get; }
        List<Migration> RetrieveAppliedMigrations();
        void CreateAppliedMigrationsTable();
        void RegisterMigration(Migration migration, bool isNew);
        void ExecuteNonQuery(string sql);
    }

    public class OracleProvider : IDbProvider
    {
#pragma warning disable 618
        private const string SELECT_APPLIED_MIGRATIONS_SQL = @"select NUM, HASH, NAME, APPLIED_DATE from  APPLIED_MIGRATIONS";

        private const string CREATE_APPLIED_MIGRATIONS_TABLE_SQL = @"create table APPLIED_MIGRATIONS (
                                                                      NUM varchar(20),
                                                                      HASH varchar(32),
                                                                      NAME varchar(100),
                                                                      APPLIED_DATE timestamp(6)
                                                                    )";
        private const string UNIQUE_MIGRATIONS_CONSTRAINT_SQL = @"alter table applied_migrations
                                                                    add constraint mig_unique_num_name unique (num, name)";

        private const string INSERT_MIGRATION_SQL = @"insert into applied_migrations(NUM, HASH, NAME, APPLIED_DATE) 
                                                        values (:num, :hash, :name, :applied_date)";
        private const string UPDATE_MIGRATION_SQL = @"update applied_migrations
                                                        set HASH = :hash, APPLIED_DATE = :applied_date 
                                                        where NUM = :num and NAME = :name";
        private const int TABLE_DOES_NOT_EXIST = 942;


        private readonly string _connString;
        private bool _dbInitialized = true;
        public bool DbInitialized { get { return _dbInitialized; } }
        public string ConnString { get { return _connString; } }

        public OracleProvider(string connString)
        {
            _connString = connString;
        }

        public List<Migration> RetrieveAppliedMigrations()
        {
            try
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
                                Migration.MakeAppliedMigration(reader.GetString(0), 
                                reader.GetString(2), reader.GetString(1), reader.GetDateTime(3)));
                }

                return appliedMigrations;
            }
            catch (OracleException ex)
            {
                if (ex.Code != TABLE_DOES_NOT_EXIST) throw;
                
                _dbInitialized = false;
                return new List<Migration>();
            }
        }

        public void CreateAppliedMigrationsTable()
        {
            ExecuteNonQuery(CREATE_APPLIED_MIGRATIONS_TABLE_SQL);
            ExecuteNonQuery(UNIQUE_MIGRATIONS_CONSTRAINT_SQL);
        }

        public void RegisterMigration(Migration migration, bool isNew)
        {
            using (var conn = new OracleConnection(_connString))
            using (var command = conn.CreateCommand())
            {
                command.CommandText = isNew ? INSERT_MIGRATION_SQL : UPDATE_MIGRATION_SQL;
                command.Parameters.AddWithValue(":num", migration.Num);
                command.Parameters.AddWithValue(":hash", migration.Hash);
                command.Parameters.AddWithValue(":name", migration.Name);
                command.Parameters.AddWithValue(":applied_date", DateTime.Now);

                conn.Open();
                command.ExecuteNonQuery();
            }
        }

        public void ExecuteNonQuery(string sql)
        {
            using (var conn = new OracleConnection(_connString))
            using (var command = conn.CreateCommand())
            {
                command.CommandText = sql;
                conn.Open();

                command.ExecuteNonQuery();
            }
        }
#pragma warning restore 618
    }

    public class MssqlProvider : IDbProvider
    {
        private const string SELECT_APPLIED_MIGRATIONS_SQL = @"select num, hash, name, applied_date from  applied_migrations";

        private const string CREATE_APPLIED_MIGRATIONS_TABLE_SQL = @"create table applied_migrations (
                                                                      num varchar(20),
                                                                      hash varchar(32),
                                                                      name varchar(100),
                                                                      applied_date datetime2
                                                                    )";

        private const string UNIQUE_MIGRATIONS_CONSTRAINT_SQL = @"alter table applied_migrations
                                                                    add constraint mig_unique_num_name unique (num, name)";

        private const string INSERT_MIGRATION_SQL = @"insert into applied_migrations(num, hash, name, applied_date) 
                                                        values (@num, @hash, @name, @applied_date)";
        private const string UPDATE_MIGRATION_SQL = @"update applied_migrations
                                                        set HASH = @hash, APPLIED_DATE = @applied_date 
                                                        where NUM = @num and NAME = @name";
        private const int TABLE_DOES_NOT_EXIST = -2146232060;


        private readonly string _connString;
        private bool _dbInitialized = true;
        public bool DbInitialized { get { return _dbInitialized; } }
        public string ConnString { get { return _connString; } }

        public MssqlProvider(string connString)
        {
            _connString = connString;
        }

        public List<Migration> RetrieveAppliedMigrations()
        {
            try
            {
                var appliedMigrations = new List<Migration>();

                using (var conn = new SqlConnection(_connString))
                using (var command = conn.CreateCommand())
                {
                    command.CommandText = SELECT_APPLIED_MIGRATIONS_SQL;
                    conn.Open();

                    using (var reader = command.ExecuteReader())
                        while (reader.Read())
                            appliedMigrations.Add(
                                Migration.MakeAppliedMigration(reader.GetString(0), 
                                                               reader.GetString(2), reader.GetString(1), reader.GetDateTime(3)));
                }

                return appliedMigrations;
            }
            catch (SqlException ex)
            {
                if (ex.ErrorCode != TABLE_DOES_NOT_EXIST) throw;

                Console.WriteLine(ex.Message);
                _dbInitialized = false;
                return new List<Migration>();
            }
        }

        public void CreateAppliedMigrationsTable()
        {
            ExecuteNonQuery(CREATE_APPLIED_MIGRATIONS_TABLE_SQL);
            ExecuteNonQuery(UNIQUE_MIGRATIONS_CONSTRAINT_SQL);
        }

        public void RegisterMigration(Migration migration, bool isNew)
        {
            using (var conn = new SqlConnection(_connString))
            using (var command = conn.CreateCommand())
            {
                command.CommandText = isNew ? INSERT_MIGRATION_SQL : UPDATE_MIGRATION_SQL;
                command.Parameters.AddWithValue("@num", migration.Num);
                command.Parameters.AddWithValue("@hash", migration.Hash);
                command.Parameters.AddWithValue("@name", migration.Name);
                command.Parameters.AddWithValue("@applied_date", DateTime.Now);

                conn.Open();
                command.ExecuteNonQuery();
            }
        }

        public void ExecuteNonQuery(string sql)
        {
            using (var conn = new SqlConnection(_connString))
            using (var command = conn.CreateCommand())
            {
                command.CommandText = sql;
                conn.Open();

                command.ExecuteNonQuery();
            }
        }
    }
}
