using LiteDB;
using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace ContainersXray
{
    class AESCryption
    {
        private static readonly string PasswordChars = "0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ";

        public static void Init()
        {
            using (LiteDatabase db = new LiteDatabase("Filename=" + SshTerminal.DbPersistentFile + ";connection=shared"))
            {
                var collection = db.GetCollection<AESKeyEntry>("AES_Key");
                bool exists = collection.Query().Exists();
                if (!exists)
                {
                    AESKeyEntry ake = new AESKeyEntry();
                    ake.IV = GeneratePassword(16);
                    ake.Key = GeneratePassword(16);
                    collection.Insert(ake);
                }
            }
        }

        public static AESKeyEntry GetAESKeyEntry()
        {
            Init();
            using (LiteDatabase db = new LiteDatabase("Filename=" + SshTerminal.DbPersistentFile + ";connection=shared"))
            {
                var collection = db.GetCollection<AESKeyEntry>("AES_Key");
                AESKeyEntry ake = collection.Query().First();
                return ake;
            }
        }

        public static string GeneratePassword(int length)
        {
            StringBuilder sb = new StringBuilder(length);
            Random r = new Random(Guid.NewGuid().ToString().GetHashCode());

            for (int i = 0; i < length; i++)
            {
                int pos = r.Next(PasswordChars.Length);
                char c = PasswordChars[pos];
                sb.Append(c);
            }

            return sb.ToString();
        }

        public static string Encrypt(string text, string iv, string key)
        {

            using (RijndaelManaged rijndael = new RijndaelManaged())
            {
                rijndael.BlockSize = 128;
                rijndael.KeySize = 128;
                rijndael.Mode = CipherMode.CBC;
                rijndael.Padding = PaddingMode.PKCS7;

                rijndael.IV = Encoding.UTF8.GetBytes(iv);
                rijndael.Key = Encoding.UTF8.GetBytes(key);

                ICryptoTransform encryptor = rijndael.CreateEncryptor(rijndael.Key, rijndael.IV);

                byte[] encrypted;
                using (MemoryStream mStream = new MemoryStream())
                {
                    using (CryptoStream ctStream = new CryptoStream(mStream, encryptor, CryptoStreamMode.Write))
                    {
                        using (StreamWriter sw = new StreamWriter(ctStream))
                        {
                            sw.Write(text);
                        }
                        encrypted = mStream.ToArray();
                    }
                }
                return (System.Convert.ToBase64String(encrypted));
            }
        }

        public static string Decrypt(string cipher, string iv, string key)
        {
            using (RijndaelManaged rijndael = new RijndaelManaged())
            {
                rijndael.BlockSize = 128;
                rijndael.KeySize = 128;
                rijndael.Mode = CipherMode.CBC;
                rijndael.Padding = PaddingMode.PKCS7;

                rijndael.IV = Encoding.UTF8.GetBytes(iv);
                rijndael.Key = Encoding.UTF8.GetBytes(key);

                ICryptoTransform decryptor = rijndael.CreateDecryptor(rijndael.Key, rijndael.IV);

                string plain = string.Empty;
                using (MemoryStream mStream = new MemoryStream(System.Convert.FromBase64String(cipher)))
                {
                    using (CryptoStream ctStream = new CryptoStream(mStream, decryptor, CryptoStreamMode.Read))
                    {
                        using (StreamReader sr = new StreamReader(ctStream))
                        {
                            plain = sr.ReadLine();
                        }
                    }
                }
                return plain;
            }
        }
    }
}
