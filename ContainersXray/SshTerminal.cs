using LiteDB;
using Renci.SshNet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace ContainersXray
{
    class SshTerminal
    {
        public static string hostName { get; set; }
        public static string loginUserName { get; set; }
        public static string loginPassword { get; set; }
        public static string execUserName { get; set; }
        public static string execPassword { get; set; }
        public static string containerID { get; set; }
        public static string containerName { get; set; }
        public static string imageName { get; set; }
        public static SshClient client { get; set; }
        public static ShellStream shellStream { get; set; }

        public static bool login()
        {
            try
            {
                client = new SshClient(hostName, loginUserName, loginPassword);
                client.Connect();
            }
            catch(Exception e)
            {
                return false;
            }
            shellStream = client.CreateShellStream("xterm", 5000, 5000, 800, 600, 1024);
            exec("whoami");
            if (loginUserName != execUserName)
            {
                return switchUser();
            }
            exec("whoami");
            return true;
        }

        public static void logout()
        {
            client.Disconnect();
        }

        public static void containerLogin()
        {
            shellStream.WriteLine("docker exec -it " + containerName + " /bin/sh");
            string prompt = shellStream.Expect(new Regex(@"[$#>]"));
            Console.WriteLine(prompt);
            exec("whoami");
        }

        public static bool switchUser()
        {
            return switchUser(execUserName, execPassword);
        }

        public static bool switchUser(string username, string password)
        {
            try
            {
                string prompt = shellStream.Expect(new Regex(@"[$#>]"));
                Console.WriteLine(prompt);

                shellStream.WriteLine("su - " + username);
                prompt = shellStream.Expect(new Regex(@"[:]"));
                Console.WriteLine(prompt);

                if (prompt.Contains(":"))
                {
                    shellStream.WriteLine(password);
                    prompt = shellStream.Expect(new Regex(@"[$#>]"));
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
            shellStream.WriteLine(cmd + "; echo this-is-the-end");
            while (shellStream.Length == 0)
            {
                Thread.Sleep(500);
            }
        }

        private static string ReadStream()
        {
            StringBuilder result = new StringBuilder();

            string line;
            while ((line = shellStream.ReadLine()) != "this-is-the-end") {
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

        public static void ls(string path)
        {
            using (LiteDatabase db = new LiteDatabase(@"Filename=ContainersXray.db;connection=shared"))
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
                    fe.parentPath = path;
                    fe.containerId = containerID;
                    fe.path = path + "/" + strs[8];
                    for(var i = 8; i < strs.Length; i++)
                    {
                        fe.name += strs[i] + " ";
                    }
                    fe.name = fe.name.Trim();
                    fe.size = strs[4];
                    fe.updatedAt = strs[5] + "/" + strs[6] + " " + strs[7];
                    fe.permission = strs[0];
                    fe.owner = strs[2] + " : " + strs[3];
                    if(!fe.path.EndsWith(".") && !fe.path.EndsWith(".."))
                    {
                        entries.Add(fe);
                    }
                }
                var delList = collection.Query()
                    .Where(rec => rec.parentPath == path)
                    .ToList();
                foreach(var delRec in delList)
                {
                    collection.Delete(delRec.id);
                }
                collection.InsertBulk(entries);
            }
        }

        public static void lsRoot()
        {
            using (LiteDatabase db = new LiteDatabase(@"Filename=ContainersXray.db;connection=shared"))
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
                    fe.parentPath = null;
                    fe.containerId = containerID;
                    fe.path = "/" + strs[8];
                    for (var i = 8; i < strs.Length; i++)
                    {
                        fe.name += strs[i] + " ";
                    }
                    fe.name = fe.name.Trim();
                    fe.size = strs[4];
                    fe.updatedAt = strs[5] + "/" + strs[6] + " " + strs[7];
                    fe.permission = strs[0];
                    fe.owner = strs[2] + " : " + strs[3];
                    if (!fe.path.EndsWith(".") && !fe.path.EndsWith(".."))
                    {
                        entries.Add(fe);
                    }
                }
                var delList = collection.Query()
                    .Where(rec => rec.parentPath == null)
                    .ToList();
                foreach (var delRec in delList)
                {
                    collection.Delete(delRec.id);
                }
                collection.InsertBulk(entries);
            }
        }
    }
}
