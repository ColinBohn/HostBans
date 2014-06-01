using System;
using System.Data;
using System.IO;
using System.Collections.Generic;
using System.ComponentModel;
using Terraria;
using TShockAPI;
using TShockAPI.DB;
using MySql.Data.MySqlClient;
using Mono.Data.Sqlite;

namespace HostBans
{
    public class SQL
    {
        public static SqlTableEditor SQLEditor;
        public static SqlTableCreator SQLWriter;
        private static IDbConnection db;

        public static void SetupDB()
        {
            switch (TShock.Config.StorageType.ToLower())
            {
                case "mysql":
                    string[] host = TShock.Config.MySqlHost.Split(':');
                    db = new MySqlConnection()
                    {
                        ConnectionString = string.Format("Server={0}; Port={1}; Database={2}; Uid={3}; Pwd={4};",
                        host[0],
                        host.Length == 1 ? "3306" : host[1],
                        TShock.Config.MySqlDbName,
                        TShock.Config.MySqlUsername,
                        TShock.Config.MySqlPassword)
                    };
                    break;
                case "sqlite":
                    string sql = Path.Combine(TShock.SavePath, "hostbans.sqlite");
                    db = new SqliteConnection(string.Format("uri=file://{0},Version=3", sql));
                    break;
            }
            SQLEditor = new SqlTableEditor(TShock.DB, TShock.DB.GetSqlType() == SqlType.Sqlite ? (IQueryBuilder)new SqliteQueryCreator() : new MysqlQueryCreator());
            SQLWriter = new SqlTableCreator(TShock.DB, TShock.DB.GetSqlType() == SqlType.Sqlite ? (IQueryBuilder)new SqliteQueryCreator() : new MysqlQueryCreator());
            var table = new SqlTable("hostbans",
             new SqlColumn("ID", MySqlDbType.Int32) { Primary = true, AutoIncrement = true },
             new SqlColumn("Host", MySqlDbType.Text),
             new SqlColumn("Username", MySqlDbType.Text),
             new SqlColumn("Reason", MySqlDbType.Text),
             new SqlColumn("Admin", MySqlDbType.Text),
             new SqlColumn("Date", MySqlDbType.Text)
            );
            SQLWriter.EnsureExists(table);
            var table2 = new SqlTable("hostbanscache",
             new SqlColumn("IP", MySqlDbType.VarChar, 16) { Unique = true },
             new SqlColumn("Host", MySqlDbType.Text)
            );
            SQLWriter.EnsureExists(table2);
        }
        public static bool InsertCacheEntry(string host, string ip)
        {
            try
            {
                return db.Query("INSERT INTO hostbanscache (IP, Host) VALUES (@0, @1);", ip, host) != 0;
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
            }
            return false;
        }
        public static string GetHostFromCache(string ip)
        {
            try
            {
                using (QueryResult reader = db.QueryReader("SELECT Host FROM hostbanscache WHERE IP=@0", ip))
                {
                    if (reader.Read())
                        return reader.Get<string>("Host");
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
            }
            return null;
        }
        public static HostBan GetBanByHost(string host)
        {
            try
            {
                using (QueryResult reader = db.QueryReader("SELECT * FROM hostbans WHERE @0 REGEXP Host", host))
                {
                    if (reader.Read())
                        return new HostBan(reader.Get<int>("ID"), reader.Get<string>("Host"), reader.Get<string>("Username"), reader.Get<string>("Reason"), reader.Get<string>("Admin"), Convert.ToDateTime(reader.Get<string>("Date")));
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
            }
            return null;
        }
        public static bool AddBan(string host, string username, string reason, string admin, DateTime date)
        {
            try
            {
                return db.Query("INSERT INTO hostbans (Host, Username, Reason, Admin, Date) VALUES (@0, @1, @2, @3, @4);", host, username, reason, admin, DateTime.UtcNow) != 0;
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
            }
            return false;
        }
        public static bool DeleteBanByUsername(string name)
        {
            try
            {
                return db.Query("DELETE FROM hostbans where Username = @0", name) != 0;
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
            }
            return false;
        }
        public class HostBan
        {
            public int ID { get; set; }
            public string Host { get; set; }
            public string Username { get; set; }
            public string Reason { get; set; }
            public string Admin { get; set; }
            public DateTime Date { get; set; }
            public HostBan(int id, string host, string username, string reason, string admin, DateTime date)
            {
                ID = id;
                Host = host;
                Username = username;
                Reason = reason;
                Admin = admin;
                Date = date;
            }
            public HostBan()
            {
                ID = 0;
                Host = "";
                Username = "";
                Reason = "";
                Admin = "";
                Date = DateTime.MinValue;
            }

        }
    }
}
