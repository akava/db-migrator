﻿using System;
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
        void RegisterMigration(Migration migration);
        void ExecuteNonQuery(string sql);
    }

    public class OracleProvider : IDbProvider
    {
        private const string SELECT_APPLIED_MIGRATIONS_SQL = @"select NUM, REVISION, NAME, APPLIED_DATE from  APPLIED_MIGRATIONS";

        private const string CREATE_APPLIED_MIGRATIONS_TABLE_SQL = @"create table APPLIED_MIGRATIONS (
                                                                      NUM int,
                                                                      REVISION varchar(50),
                                                                      NAME varchar(100),
                                                                      APPLIED_DATE timestamp(6)
                                                                    )";

        private const string INSERT_MIGRATION_SQL = @"insert into applied_migrations(NUM, REVISION, NAME, APPLIED_DATE) 
                                                        values (:num, :revision, :name, :applied_date)";
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
                                Migration.MakeAppliedMigration(reader.GetInt32(0), reader.GetString(1), 
                                reader.GetString(2), reader.GetDateTime(3)));
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
        }

        public void RegisterMigration(Migration migration)
        {
            using (var conn = new OracleConnection(_connString))
            using (var command = conn.CreateCommand())
            {
                command.CommandText = INSERT_MIGRATION_SQL;
                command.Parameters.AddWithValue(":num", migration.Num);
                command.Parameters.AddWithValue(":revision", migration.Revision);
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
    }

    public class MssqlProvider : IDbProvider
    {
        private const string SELECT_APPLIED_MIGRATIONS_SQL = @"select num, revision, name, applied_date from  applied_migrations";

        private const string CREATE_APPLIED_MIGRATIONS_TABLE_SQL = @"create table applied_migrations (
                                                                      num int,
                                                                      revision varchar(50),
                                                                      name varchar(100),
                                                                      applied_date datetime2
                                                                    )";

        private const string INSERT_MIGRATION_SQL = @"insert into applied_migrations(num, revision, name, applied_date) 
                                                        values (@num, @revision, @name, @applied_date)";
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
                                Migration.MakeAppliedMigration(reader.GetInt32(0), reader.GetString(1), 
                                                               reader.GetString(2), reader.GetDateTime(3)));
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
        }

        public void RegisterMigration(Migration migration)
        {
            using (var conn = new SqlConnection(_connString))
            using (var command = conn.CreateCommand())
            {
                command.CommandText = INSERT_MIGRATION_SQL;
                command.Parameters.AddWithValue("@num", migration.Num);
                command.Parameters.AddWithValue("@revision", migration.Revision);
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