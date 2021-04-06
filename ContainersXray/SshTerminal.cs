using LiteDB;
using Renci.SshNet;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace ContainersXray
{
    class SshTerminal
    {
        public static string HostName { get; set; }
        public static string LoginUserName { get; set; }
        public static string LoginPassword { get; set; }
        public static string ExecUserName { get; set; }
        public static string ExecPassword { get; set; }
        public static string ContainerID { get; set; }
        public static string ContainerName { get; set; }
        public static string ImageName { get; set; }
        public static SshClient Client { get; set; }
        public static ShellStream ShellStream { get; set; }
        public static string DbTempFile { get; set; } 
        public static string DbPersistentFile = "ContainersXray.db";

        public static bool Login()
        {
            try
            {
                Client = new SshClient(HostName, LoginUserName, LoginPassword);
                Client.Connect();
            }
            catch(Exception e)
            {
                return false;
            }
            ShellStream = Client.CreateShellStream("xterm", 5000, 5000, 800, 600, 1024);
            exec("whoami");
            if (LoginUserName != ExecUserName)
            {
                return SwitchUser();
            }
            exec("whoami");
            DbTempFile = "ContainersXray-" + Guid.NewGuid().ToString() + ".db";
            return true;
        }

        public static void Logout()
        {
            Client.Disconnect();
            File.Delete(System.Environment.CurrentDirectory + @"\" + DbTempFile);
        }

        public static void ContainerLogin()
        {
            ShellStream.WriteLine("docker exec -it " + ContainerName + " /bin/sh");
            string prompt = ShellStream.Expect(new Regex(@"[$#>]"));
            Console.WriteLine(prompt);
            exec("whoami");
        }

        public static bool SwitchUser()
        {
            return SwitchUser(ExecUserName, ExecPassword);
        }

        public static bool SwitchUser(string username, string password)
        {
            try
            {
                string prompt = ShellStream.Expect(new Regex(@"[$#>]"));
                Console.WriteLine(prompt);

                ShellStream.WriteLine("su - " + username);
                prompt = ShellStream.Expect(new Regex(@"[:]"));
                Console.WriteLine(prompt);

                if (prompt.Contains(":"))
                {
                    ShellStream.WriteLine(password);
                    prompt = ShellStream.Expect(new Regex(@"[$#>]"));
                    Console.WriteLine(prompt);
                }
                return true;
            }
            catch (Exception e)
            {
                return false;
            }
        }

        private static void WriteStream(string cmd)
        {
            ShellStream.WriteLine(cmd + "; echo this-is-the-end");
            while (ShellStream.Length == 0)
            {
                Thread.Sleep(500);
            }
        }

        private static string ReadStream()
        {
            StringBuilder result = new StringBuilder();

            string line;
            while ((line = ShellStream.ReadLine()) != "this-is-the-end") {
                if (line.EndsWith("this-is-the-end"))
                {
                    continue;
                }
                else
                {
                    result.AppendLine(line);
                }
            }

            return result.ToString();
        }

        public static string exec(string cmd)
        {
            Console.WriteLine("Exec command: \n" + cmd);
            WriteStream(cmd);
            string answer = ReadStream();
            Console.WriteLine("Command output: \n" + answer);
            return answer;
        }

        public static void Ls(string path)
        {
            using (LiteDatabase db = new LiteDatabase("Filename=" + SshTerminal.DbTempFile + ";connection=shared"))
            {
                var collection = db.GetCollection<FileEntry>("file_entry");
                
                string[] lines = Regex.Split(exec("ls -la --color=none " + path), "\n");
                var entries = new List<FileEntry>();
                foreach(string line in lines)
                {
                    var l = Regex.Replace(line, "\r", "");
                    if (string.IsNullOrEmpty(l) || l.StartsWith("total") || l.StartsWith("合計"))
                    {
                        continue;
                    }
                    string[] strs = Regex.Split(l, "[ ]{1,}");
                    var fe = new FileEntry();
                    fe.ParentPath = path;
                    fe.ContainerID = ContainerID;
                    fe.Path = path + "/" + strs[8];
                    for(var i = 8; i < strs.Length; i++)
                    {
                        fe.Name += strs[i] + " ";
                    }
                    fe.Name = fe.Name.Trim();
                    fe.Size = strs[4];
                    fe.UpdatedAt = strs[5] + "/" + strs[6] + " " + strs[7];
                    fe.Permission = strs[0];
                    fe.Owner = strs[2] + " : " + strs[3];
                    if(!fe.Path.EndsWith(".") && !fe.Path.EndsWith(".."))
                    {
                        entries.Add(fe);
                    }
                }
                var delList = collection.Query()
                    .Where(rec => rec.ParentPath == path)
                    .ToList();
                foreach(var delRec in delList)
                {
                    collection.Delete(delRec.Id);
                }
                collection.InsertBulk(entries);
            }
        }

        public static void LsRoot()
        {
            using (LiteDatabase db = new LiteDatabase("Filename=" + SshTerminal.DbTempFile + ";connection=shared"))
            {
                var collection = db.GetCollection<FileEntry>("file_entry");

                string[] lines = Regex.Split(exec("ls -la --color=none /"), "\n");
                var entries = new List<FileEntry>();
                foreach (string line in lines)
                {
                    var l = Regex.Replace(line, "\r", "");
                    if (string.IsNullOrEmpty(l) || l.StartsWith("total") || l.StartsWith("合計"))
                    {
                        continue;
                    }
                    string[] strs = Regex.Split(l, "[ ]{1,}");
                    var fe = new FileEntry();
                    fe.ParentPath = null;
                    fe.ContainerID = ContainerID;
                    fe.Path = "/" + strs[8];
                    for (var i = 8; i < strs.Length; i++)
                    {
                        fe.Name += strs[i] + " ";
                    }
                    fe.Name = fe.Name.Trim();
                    fe.Size = strs[4];
                    fe.UpdatedAt = strs[5] + "/" + strs[6] + " " + strs[7];
                    fe.Permission = strs[0];
                    fe.Owner = strs[2] + " : " + strs[3];
                    if (!fe.Path.EndsWith(".") && !fe.Path.EndsWith(".."))
                    {
                        entries.Add(fe);
                    }
                }
                var delList = collection.Query()
                    .Where(rec => rec.ParentPath == null)
                    .ToList();
                foreach (var delRec in delList)
                {
                    collection.Delete(delRec.Id);
                }
                collection.InsertBulk(entries);
            }
        }

        public static List<string> GetHostList()
        {
            var ret = new List<string>();
            using (LiteDatabase db = new LiteDatabase("Filename=" + SshTerminal.DbPersistentFile + ";connection=shared"))
            {
                var collection = db.GetCollection<HostEntry>("Host");
                var hostList = collection.Query().ToList();
                foreach(var host in hostList)
                {
                    ret.Add(host.Host);
                }
                ret.Sort();
            }
            return ret;
        }

        public static void SaveHost(string host)
        {
            using (LiteDatabase db = new LiteDatabase("Filename=" + SshTerminal.DbPersistentFile + ";connection=shared"))
            {
                var collection = db.GetCollection<HostEntry>("Host");
                bool exists = collection.Query()
                    .Where(rec => rec.Host == host)
                    .Exists();
                if (!exists)
                {
                    HostEntry he = new HostEntry();
                    he.Host = host;
                    collection.Insert(he);
                }
            }
        }

        public static List<string> GetUserList(string host)
        {
            var ret = new List<string>();
            using (LiteDatabase db = new LiteDatabase("Filename=" + SshTerminal.DbPersistentFile + ";connection=shared"))
            {
                var collection = db.GetCollection<UserPasswdEntry>("UserPasswd");
                var userList = collection.Query()
                    .Where(rec => rec.Host == host)
                    .ToList();
                foreach(var usr in userList)
                {
                    ret.Add(usr.User);
                }
                ret.Sort();
            }
            return ret;
        }

        public static string GetPasswd(string host, string user)
        {
            var ake = AESCryption.GetAESKeyEntry();
            using (LiteDatabase db = new LiteDatabase("Filename=" + SshTerminal.DbPersistentFile + ";connection=shared"))
            {
                var collection = db.GetCollection<UserPasswdEntry>("UserPasswd");
                var passwd = collection.Query()
                    .Where(rec => rec.Host == host && rec.User == user)
                    .Single()
                    .Password;
                return AESCryption.Decrypt(passwd, ake.IV, ake.Key);
            }
        }

        public static void SaveUserPasswd(string host, string user, string passwd)
        {
            var ake = AESCryption.GetAESKeyEntry();
            var encrypted = AESCryption.Encrypt(passwd, ake.IV, ake.Key);
            using (LiteDatabase db = new LiteDatabase("Filename=" + SshTerminal.DbPersistentFile + ";connection=shared"))
            {
                var collection = db.GetCollection<UserPasswdEntry>("UserPasswd");
                bool exists = collection.Query()
                    .Where(rec => rec.Host == host && rec.User == user && rec.Password == encrypted)
                    .Exists();
                if (!exists)
                {
                    var upe = new UserPasswdEntry();
                    upe.Host = host;
                    upe.User = user;
                    upe.Password = encrypted;
                    collection.Insert(upe);
                }
            }
        }
    }
}
