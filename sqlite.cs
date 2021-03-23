using System;
using System.Data.SQLite;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace sqlite
{
    public class sql
    {
        private SQLiteCommand cmd { get; set; }
        private SQLiteConnection db { get; set; }
        public sql(string path)
        {
            if (File.Exists(path))
            {
                db = new SQLiteConnection("Data Source=" + path);
                Directory.CreateDirectory("data");
                db.Open();
                cmd = db.CreateCommand();
            }
            else
            {
                db = new SQLiteConnection("Data Source=" + path);
                db.Open();
                cmd = db.CreateCommand();
                cmd.CommandText = "create table kv(k TEXT PRIMARY KEY NOT NULL,v TEXT NOT NULL)";
                cmd.ExecuteNonQuery();
            }
        }
        public void dput(string k, string v)
        {
            if (haskey(k))
            {
                cmd.CommandText = "update kv set v = '{v}' where k = '{k}'";
                cmd.ExecuteNonQuery();
            }
            else
            {
                cmd.CommandText = $"insert into kv(k,v) VALUES ('{k}','{v}')";
                cmd.ExecuteNonQuery();
            }
        }
        public string dget(string k)
        {
            cmd.CommandText = $"select * from kv where k = {k}";
            var i = cmd.ExecuteReader();
            if (i.HasRows)
            {
                i.Read();
                return i.GetString(1);
            }
            else
                return "nil";
        }
        public void ddel(string k)
        {
            if (haskey(k))
            {
                cmd.CommandText = $"delete from kv where k = {k}";
                var i = cmd.ExecuteReader();
            }
        }
        private bool haskey(string k)
        {
            cmd.CommandText = $"select * from kv where k = {k}";
            var i = cmd.ExecuteReader();
            var r = i.HasRows;
            i.Close();
            return r;
        }



    }
}
