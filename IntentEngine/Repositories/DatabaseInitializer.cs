using System;
using System.Data;
using System.Data.SQLite;
using System.IO;

namespace IntentEngine.Repositories
{
    public class DatabaseInitializer
    {
        private static readonly object _lock = new object();
        private static bool _initialized = false;

        public string ConnectionString { get; private set; }

        public DatabaseInitializer()
        {
            string dataDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data");
            if (!Directory.Exists(dataDir))
                Directory.CreateDirectory(dataDir);

            string dbPath = Path.Combine(dataDir, "IntentEngine.db");
            ConnectionString = "Data Source=" + dbPath + ";Version=3;";
        }

        public void Initialize()
        {
            lock (_lock)
            {
                if (_initialized) return;

                using (var conn = new SQLiteConnection(ConnectionString))
                {
                    conn.Open();
                    CreateTables(conn);
                    MigrateSchema(conn);
                    SeedData(conn);
                }

                _initialized = true;
            }
        }

        private void MigrateSchema(SQLiteConnection conn)
        {
            try { Exec(conn, "ALTER TABLE Functions ADD COLUMN DataSource TEXT DEFAULT 'Config'"); } catch { }
        }

        private void CreateTables(SQLiteConnection conn)
        {
            string sql = @"
                CREATE TABLE IF NOT EXISTS Intents (
                    Id          INTEGER PRIMARY KEY AUTOINCREMENT,
                    Name        TEXT NOT NULL,
                    Description TEXT,
                    Keywords    TEXT,
                    Category    TEXT,
                    SemanticText TEXT,
                    IsActive    INTEGER DEFAULT 1,
                    CreatedAt   TEXT DEFAULT (datetime('now','localtime'))
                );

                CREATE TABLE IF NOT EXISTS Functions (
                    Id          INTEGER PRIMARY KEY AUTOINCREMENT,
                    IntentId    INTEGER NOT NULL REFERENCES Intents(Id),
                    Name        TEXT NOT NULL,
                    Description TEXT,
                    SortOrder   INTEGER DEFAULT 0,
                    DataSource  TEXT DEFAULT 'Config'
                );

                CREATE TABLE IF NOT EXISTS FlowSteps (
                    Id              INTEGER PRIMARY KEY AUTOINCREMENT,
                    FunctionId      INTEGER NOT NULL REFERENCES Functions(Id),
                    SortOrder       INTEGER DEFAULT 0,
                    StepType        TEXT NOT NULL,
                    Label           TEXT,
                    SqlText         TEXT,
                    ResultVar       TEXT,
                    DataSource      TEXT DEFAULT 'Config',
                    ExpectOperator  TEXT,
                    ExpectValue     TEXT,
                    ExpectOnFail    TEXT,
                    ExpectTarget    INTEGER,
                    ExpectMessage   TEXT,
                    DisplayTitle    TEXT,
                    DisplaySource   TEXT,
                    DisplayConfig   TEXT,
                    IsEnd           INTEGER DEFAULT 0
                );

                CREATE TABLE IF NOT EXISTS FunctionParameters (
                    Id           INTEGER PRIMARY KEY AUTOINCREMENT,
                    FunctionId   INTEGER NOT NULL REFERENCES Functions(Id),
                    Name         TEXT NOT NULL,
                    Label        TEXT,
                    ControlType  TEXT DEFAULT 'TextBox',
                    DataSource   TEXT,
                    IsRequired   INTEGER DEFAULT 0,
                    DefaultValue TEXT,
                    SortOrder    INTEGER DEFAULT 0
                );

                CREATE TABLE IF NOT EXISTS DataSourceConfig (
                    Id              INTEGER PRIMARY KEY AUTOINCREMENT,
                    Name            TEXT NOT NULL,
                    ProviderType    TEXT NOT NULL,
                    ConnectionString TEXT NOT NULL,
                    IsDefault       INTEGER DEFAULT 0
                );

                CREATE TABLE IF NOT EXISTS QueryLog (
                    Id          INTEGER PRIMARY KEY AUTOINCREMENT,
                    IntentName  TEXT,
                    FunctionName TEXT,
                    InputText   TEXT,
                    Parameters  TEXT,
                    Similarity  REAL,
                    FlowLog     TEXT,
                    ElapsedMs   INTEGER,
                    ExecutedAt  TEXT DEFAULT (datetime('now','localtime'))
                );
            ";

            using (var cmd = new SQLiteCommand(sql, conn))
            {
                cmd.ExecuteNonQuery();
            }
        }

        private void SeedData(SQLiteConnection conn) { }

        private void Exec(SQLiteConnection conn, string sql)
        {
            using (var cmd = new SQLiteCommand(sql, conn))
            {
                cmd.ExecuteNonQuery();
            }
        }

        public DataTable Query(string sql, SQLiteParameter[] parameters = null)
        {
            using (var conn = new SQLiteConnection(ConnectionString))
            {
                conn.Open();
                using (var cmd = new SQLiteCommand(sql, conn))
                {
                    if (parameters != null)
                        cmd.Parameters.AddRange(parameters);
                    using (var da = new SQLiteDataAdapter(cmd))
                    {
                        var dt = new DataTable();
                        da.Fill(dt);
                        return dt;
                    }
                }
            }
        }

        public int Execute(string sql, SQLiteParameter[] parameters = null)
        {
            using (var conn = new SQLiteConnection(ConnectionString))
            {
                conn.Open();
                using (var cmd = new SQLiteCommand(sql, conn))
                {
                    if (parameters != null)
                        cmd.Parameters.AddRange(parameters);
                    return cmd.ExecuteNonQuery();
                }
            }
        }

        public T ExecuteScalar<T>(string sql, SQLiteParameter[] parameters = null)
        {
            using (var conn = new SQLiteConnection(ConnectionString))
            {
                conn.Open();
                using (var cmd = new SQLiteCommand(sql, conn))
                {
                    if (parameters != null)
                        cmd.Parameters.AddRange(parameters);
                    return (T)Convert.ChangeType(cmd.ExecuteScalar(), typeof(T));
                }
            }
        }
    }
}
