using NLua;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using YamlDotNet.Serialization;

namespace ILR
{

    public class ToolFunc
    {
        private static Dictionary<int, Thread> thr = new Dictionary<int, Thread>();
        #region TOOLAPI
        public sqlite.sql CreateSqlite(string path)
        {
            return new sqlite.sql(path);
        }
        public void WriteAllText(string path, string contenst)
        {
            File.WriteAllText(path, contenst);
        }
        public void AppendAllText(string path, string contenst)
        {
            File.AppendAllText(path, contenst);
        }
        public string ReadAllText(string path)
        {
            return File.ReadAllText(path);
        }
        public string WorkingPath()
        {
            return AppDomain.CurrentDomain.BaseDirectory;
        }
        public string ToMD5(string word)
        {
            string md5output = "";
            MD5 md5 = new MD5CryptoServiceProvider();
            byte[] date = Encoding.Default.GetBytes(word);
            byte[] date1 = md5.ComputeHash(date);
            md5.Clear();
            for (int i = 0; i < date1.Length - 1; i++)
            {
                md5output += date1[i].ToString("X");
            }
            return md5output;
        }
        public string HttpPost(string Url, string postDataStr)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(Url);
            request.Method = "POST";
            request.ContentType = "application/x-www-form-urlencoded";
            request.ContentLength = Encoding.UTF8.GetByteCount(postDataStr);
            Stream myRequestStream = request.GetRequestStream();
            StreamWriter myStreamWriter = new StreamWriter(myRequestStream, Encoding.GetEncoding("gb2312"));
            myStreamWriter.Write(postDataStr);
            myStreamWriter.Close();
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            Stream myResponseStream = response.GetResponseStream();
            StreamReader myStreamReader = new StreamReader(myResponseStream, Encoding.GetEncoding("utf-8"));
            string retString = myStreamReader.ReadToEnd();
            myStreamReader.Close();
            myResponseStream.Close();
            return retString;
        }
        public string HttpGet(string Url)
        {
            /*
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(Url + (postDataStr == "" ? "" : "?") + postDataStr);
            request.Method = "GET";
            request.ContentType = "text/html;charset=UTF-8";
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            Stream myResponseStream = response.GetResponseStream();
            StreamReader myStreamReader = new StreamReader(myResponseStream, Encoding.GetEncoding("utf-8"));
            string retString = myStreamReader.ReadToEnd();
            myStreamReader.Close();
            myResponseStream.Close();
            return retString;
            */
            var web = new WebClient();
            byte[] outputb = web.DownloadData(Url);
            return Encoding.UTF8.GetString(outputb);
        }
        public void CreateDir(string path)
        {
            Directory.CreateDirectory(path);
        }
        public bool IfFile(string path)
        {
            return File.Exists(path);
        }
        public bool IfDir(string path)
        {
            return Directory.Exists(path);
        } 
        public void ThrowException(string msg)
        {
            throw new ArgumentOutOfRangeException(msg);
        }
        public Task TaskRun(LuaFunction func) 
        {
           return Task.Run(() => func.Call());
        }
        public int Schedule(LuaFunction func, int delay, int cycle) 
        {
            var t = new Thread(() =>
            {
                for (int i = 0; i < cycle; i++)
                {
                    func.Call();
                    Thread.Sleep(delay);
                }
            });
            t.Start();
            int id = t.ManagedThreadId;
            thr.Add(id, t);
            new Thread(() =>
            {
                t.Join();
                if (thr.ContainsKey(id))
                    thr.Remove(id);
            }).Start();
            return id;
        }
        public int Schedule(LuaFunction func, int delay) 
        {
            return Schedule(func, delay, 1);
        }
        public bool Cancel(int id)
        {
            if (!thr.ContainsKey(id))
                return false;
            thr[id].Abort();
            thr.Remove(id);
            return true;
        }
        public void RunCode(string code)
        {
            IronLuaRunner.IronLuaRunner.lua.DoString(code);
        }
        public string[] getMemberInfo(dynamic obj)
        {
            List<string> list = new List<string>();
            foreach (var i in obj.GetType().GetMembers())
                list.Add(i.Name);
            return list.ToArray();
        }
        public HttpServer.Http LocalHttpListen(string ip,LuaFunction Get,LuaFunction POST)
        {
            return new HttpServer.Http(ip,Get,POST);
        }
        public string GetProperties(string key)
        {
            FileStream fs = new FileStream("server.properties", FileMode.Open, FileAccess.Read);
            StreamReader read = new StreamReader(fs, Encoding.UTF8);
            string strReadline;
            while ((strReadline = read.ReadLine()) != null)
            {
                if (strReadline.StartsWith(key+"="))
                {
                    string[] sArray = strReadline.Split(new char[] { '=' });
                    return sArray[1];
                }
            }
            fs.Close();
            read.Close();
            return null;
        }
        public string ILREncrypt(string pToEncrypt)
        {
            string sKey = "3cb156ab";
            using (DESCryptoServiceProvider des = new DESCryptoServiceProvider())
            {
                byte[] inputByteArray = Encoding.UTF8.GetBytes(pToEncrypt);
                des.Key = ASCIIEncoding.ASCII.GetBytes(sKey);
                des.IV = ASCIIEncoding.ASCII.GetBytes(sKey);
                System.IO.MemoryStream ms = new System.IO.MemoryStream();
                using (CryptoStream cs = new CryptoStream(ms, des.CreateEncryptor(), CryptoStreamMode.Write))
                {
                    cs.Write(inputByteArray, 0, inputByteArray.Length);
                    cs.FlushFinalBlock();
                    cs.Close();
                }
                string str = Convert.ToBase64String(ms.ToArray());
                ms.Close();
                return str;
            }
        }
        public object newthread(LuaFunction function,object data)
        {
            var re = new Object[] { true };
            try
            {
                new Thread(() =>
                {
                    try
                    {
                        re = (dynamic)function.Call(data);

                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("[ILR] [Thread ERROR] " + e.Message);
                    }
                }).Start();
            }
            catch (Exception e)
            {
                Console.WriteLine("[ILR] [Thread ERROR] " + e.Message);
            }               
            return re.Length == 0 || (bool)re[0];
        }
        public void sleep(int time)
        {
            Thread.Sleep(1000 * time);
        }
        public string YamlToJson(string yaml)
        {
            try
            {
                var r = new StringReader(yaml);
                var deserializer = new DeserializerBuilder().Build();
                var yamlObject = deserializer.Deserialize(r);
                var serializer = new SerializerBuilder()
                    .JsonCompatible()
                    .Build();
                var json = serializer.Serialize(yamlObject);
                return json;
            }
            catch(Exception e)
            {
                Console.WriteLine(e.ToString());
            }
            return null;
        }
        public static bool  HttpDownload(string url, string path)
        {
            string tempPath = System.IO.Path.GetDirectoryName(path) + @"\temp";
            System.IO.Directory.CreateDirectory(tempPath);  //创建临时文件目录
            string tempFile = tempPath + @"\" + System.IO.Path.GetFileName(path) + ".temp"; //临时文件
            //Stopwatch sw = new Stopwatch();
            if (System.IO.File.Exists(tempFile))
            {
                System.IO.File.Delete(tempFile);    //存在则删除
            }
            try
            {
                FileStream fs = new FileStream(tempFile, FileMode.Append, FileAccess.Write, FileShare.ReadWrite);
                // 设置参数
                HttpWebRequest request = WebRequest.Create(url) as HttpWebRequest;
                //发送请求并获取相应回应数据
                HttpWebResponse response = request.GetResponse() as HttpWebResponse;
                //直到request.GetResponse()程序才开始向目标网页发送Post请求
                Stream responseStream = response.GetResponseStream();
                //创建本地文件写入流
                //Stream stream = new FileStream(tempFile, FileMode.Create);
                byte[] bArr = new byte[1024];
                int size = responseStream.Read(bArr, 0, (int)bArr.Length);
                while (size > 0)
                {
                    //stream.Write(bArr, 0, size);
                    fs.Write(bArr, 0, size);
                    size = responseStream.Read(bArr, 0, (int)bArr.Length);
                }
                //stream.Close();
                fs.Close();
                responseStream.Close();
                System.IO.File.Move(tempFile, path);
                return true;
            }
            catch (Exception ex)
            {
                //Console.WriteLine(sw.ElapsedTicks);
                return false;
            }
        }
        public int ToInt(object number)
        {
            try
            {
                return Convert.ToInt32(number);
            }
            catch
            {
                return 0;
            }
            
        }
        public bool IsNullOrEmpty(string str)
        {
            return string.IsNullOrEmpty(str);
        }
        public string NewGuid()
        {
            return Guid.NewGuid().ToString();
        }
        #endregion
    }
}
