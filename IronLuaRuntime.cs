using CSR;
using IronPythonRunner;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NLua;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using ILR;

namespace IronLuaRunner
{
    public class WebM
    {
        public bool load { get; set; }
        public List<string> message { get; set; }
        public string version { get; set; }
    }
    public class Lib
    {
        public DateTime time { get; set; }
        public List<string> libs { get; set; }
    }
    class IronLuaRunner
    {
        public static Lua lua;
        public static Dictionary<string, object> CONFIGINI = new Dictionary<string, object>();
        public static Dictionary<string, IntPtr> ptr = new Dictionary<string, IntPtr>();
        public static string version = "Release0313fix";
        public class MCLUAAPI
        {
            private MCCSAPI api { get; set; }
            private Dictionary<string, int> TPFuncPtr { get; set; }

            public MCLUAAPI(MCCSAPI api)
            {
                this.api = api;
                TPFuncPtr = new Dictionary<string, int>
                {
                    { "1.16.200.2", 0x00C82C60 },
                    { "1.16.201.2", 0x00C82C60 },
                    { "1.16.201.3", 0x00C82C60 },
                    {"1.16.210.05", 0x007BA190 },
                    {"1.16.210.06", 0x007B1D20 }
                };
            }
            #region MCLUAAPI
            public void Listen(string key, LuaFunction fun)
            {
                api.addBeforeActListener(key, x =>
                {
                    var tmp = BaseEvent.getFrom(x);
                    var re = new Object[] { true };
                    try
                    {
                        re = fun.Call(tmp);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.Message);
                    }
                    return re.Length == 0 || (bool)re[0];
                });
            }
            public void teleport(string uuid, float x, float y, float z, int did)
            {
                IntPtr player = IntPtr.Zero;
                int _ptr = 0;
                if (TPFuncPtr.TryGetValue(api.VERSION, out _ptr) &&
                    ptr.TryGetValue(uuid, out player))
                {
                    var temp = new Vec3
                    {
                        x = x,
                        y = y,
                        z = z
                    };
                    Hook.tp(api, _ptr, player, temp, did);
                }
                else
                {
                    Console.WriteLine("[ILUAR] Hook[Teleport]未适配此版本。");
                }
            }
            public bool changeSeed(int fakeseed)
            {
                return HideSeed.Fakeseed.init(api,fakeseed);
            }
            public string LibPATH {
                get { return (string)CONFIGINI["LIBPATH"]; }
            }
            public string LuaPATH
            {
                get { return (string)CONFIGINI["LUAPATH"]; }
            }
            public CsPlayer createPlayerObject(string uuid)
            {
                try
                {
                    var pl = ptr[uuid];
                    return new CsPlayer(api, pl);
                }
                catch (Exception e)
                {
                    Console.WriteLine("[ILUAR] " + e.Message);
                    return null;
                }
            }
            public CsPlayer createEntityObject(IntPtr ptr)
            {
                try
                {
                    return new CsPlayer(api, ptr);
                }
                catch (Exception e)
                {
                    Console.WriteLine("[ILUAR] " + e.Message);
                    return null;
                }
            }
            public void setCommandDescribeEx(string key, string des, int level, int f1, int f2)
            {
                api.setCommandDescribeEx(key, des, (MCCSAPI.CommandPermissionLevel)level, (byte)f1, (byte)f2);
            }
            public CsActor getActorFromUniqueid(ulong uniqueid)
            {
                return CsActor.getFromUniqueId(api, uniqueid);
            }
            public CsPlayer getPlayerFromUniqueid(ulong uniqueid)
            {
                return (CsPlayer)CsPlayer.getFromUniqueId(api, uniqueid);
            }
            public CsActor[] getFromAABB(int did, float x1, float y1, float z1, float x2, float y2, float z2)
            {
                var temp = new List<CsActor>();
                var raw = CsActor.getsFromAABB(api, did, x1, y1, z2, x2, y2, z2);
                foreach (var i in raw)
                {
                    temp.Add((CsActor)i);
                }
                return temp.ToArray();
            }
            public CsPlayer convertActorToPlayer(CsActor ac)
            {
                return (CsPlayer)ac;
            }
            public string GetUUID(string playername)
            {
                var json = JArray.Parse(api.getOnLinePlayers());
                foreach (var i in json)
                {
                    if (i["playername"].ToString() == playername)
                        return i["uuid"].ToString();
                }
                Console.WriteLine("[ILUAR] 无法找到对应玩家的UUID：" + playername);
                return null;
            }
            public string GetXUID(string playername)
            {
                var json = JArray.Parse(api.getOnLinePlayers());
                foreach (var i in json)
                {
                    if (i["playername"].ToString() == playername)
                        return i["xuid"].ToString();
                }
                Console.WriteLine("[ILUAR] 无法找到对应玩家的XUID：" + playername);
                return null;
            }
            public GUIS.GUIBuilder createGUI(string title)
            {
                return new GUIS.GUIBuilder(api, title);
            }
            #endregion
        }
        public static string ILRDecrypt(string pToDecrypt)
        {
            byte[] outputb = Convert.FromBase64String("M2NiMTU2YWI=");
            string sKey = Encoding.Default.GetString(outputb);
            byte[] inputByteArray = Convert.FromBase64String(pToDecrypt);
            using (DESCryptoServiceProvider des = new DESCryptoServiceProvider())
            {
                des.Key = ASCIIEncoding.ASCII.GetBytes(sKey);
                des.IV = ASCIIEncoding.ASCII.GetBytes(sKey);
                MemoryStream ms = new MemoryStream();
                using (CryptoStream cs = new CryptoStream(ms, des.CreateDecryptor(), CryptoStreamMode.Write))
                {
                    cs.Write(inputByteArray, 0, inputByteArray.Length);
                    cs.FlushFinalBlock();
                    cs.Close();
                }
                string str = Encoding.UTF8.GetString(ms.ToArray());
                ms.Close();
                return str;
            }
        }

        public static string GetHasa(string hasa)
        {
            byte[] outputb = Convert.FromBase64String(hasa);
            return Encoding.UTF8.GetString(outputb);
        }
        public static void RunLua(MCCSAPI api)
        {
            Console.ForegroundColor = ConsoleColor.White;
            logo.logooo();
            List<IntPtr> uuid = new List<IntPtr>();
            string plugins = "plugins/";
            string settingdir = plugins + "settings/";          // 固定配置文件目录
            string settingpath = settingdir + "IronLua.ini";      // 固定配置文件
            string setingversion = "1.0.3";
            string CONFIG = $@"#配置文件版本，请勿乱动
CONFIGVERSION={setingversion}
#插件加载文件夹，若不存在需要自行创建
LUAPATH=./plugins/ilua/
#库文件所在文件夹，ILR会自动补全lua的库
LIBPATH=./plugins/ilua/Lib/
#是否自动更新
AUTOUPDATE=True
#调试模式是否开启
DBG=False";
            if (!File.Exists(settingpath))
            {
                Directory.CreateDirectory(settingdir);
                File.WriteAllText(settingpath, CONFIG);
            }
            string[] ini = File.ReadAllLines(settingpath);
            foreach (string i in ini)
            {
                if(!i.StartsWith("#"))
                {
                    CONFIGINI[i.ToString().Split('=')[0]] = i.ToString().Split('=')[1];
                    Console.WriteLine("[ILUAR] {0} >> {1}", i.ToString().Split('=')[0], i.ToString().Split('=')[1]);
                }
            }
            if (CONFIGINI["CONFIGVERSION"] != setingversion)
            {
                File.WriteAllText(settingpath, $"#配置文件版本，请勿乱动\nCONFIGVERSION={setingversion}\n#插件加载文件夹，若不存在需要自行创建\nLUAPATH={CONFIGINI["LUAPATH"]}\n#库文件所在文件夹，ILR会自动补全lua的库\nLIBPATH={CONFIGINI["LIBPATH"]}\n#是否自动更新\nAUTOUPDATE={CONFIGINI["AUTOUPDATE"]}\n#调试模式是否开启\nDBG={CONFIGINI["DBG"]}");
            }
            
            new Thread(() =>
            {
                var htool = new ILR.ToolFunc();
                var libupde = JsonConvert.DeserializeObject<Lib>(htool.HttpGet(GetHasa("aHR0cDovL3dpa2kuc2Jhb29yLmNvb2wvTGliL2xpYnMuanNvbg==")));
                foreach (string i in libupde.libs)
                {
                    if (!File.Exists((string)CONFIGINI["LIBPATH"] + i.ToString()))
                        File.WriteAllText((string)CONFIGINI["LIBPATH"] + i.ToString(), htool.HttpGet(GetHasa("aHR0cDovL3dpa2kuc2Jhb29yLmNvb2wvTGliLw==") + i.ToString()));
                }
                Console.WriteLine("[ILUAR] lua库更新检查完成");
                Console.WriteLine("[ILUAR] 云端库最后更新时间：" + libupde.time);
            }).Start();
            new Thread(() =>
            {
                while (true)
                {
                    Thread.Sleep(300000);
                    Console.WriteLine(GetHasa("5qyi6L+O5L2/55SoSXJvbkx1YVJ1bm5lciDkvZzogIXvvJpTYmFvb3IgZ2l0aHViOmh0dHBzOi8vZ2l0aHViLmNvbS9TYmFvb3ItZmx5L0NTUi1Jcm9uTHVhUnVubmVy"));
                    Console.WriteLine(GetHasa("SXJvbkx1YVJ1bm5lciBsb2FkZWQhIGF1dGhvcjogU2Jhb29yIGdpdGh1YjpodHRwczovL2dpdGh1Yi5jb20vU2Jhb29yLWZseS9DU1ItSXJvbkx1YVJ1bm5lcg=="));
                }
            }).Start();
            if(!Directory.Exists((string)CONFIGINI["LUAPATH"]))
            {
                Directory.CreateDirectory((string)CONFIGINI["LUAPATH"]);
            }
            if (!Directory.Exists((string)CONFIGINI["LIBPATH"]))
            {
                Directory.CreateDirectory((string)CONFIGINI["LIBPATH"]);
            }
            lua = new Lua();
            lua.State.Encoding = Encoding.UTF8;
            Console.OutputEncoding = Encoding.UTF8;
            lua["tool"] = new ILR.ToolFunc();
            lua["mc"] = api;
            lua["luaapi"] = new MCLUAAPI(api);
            lua.LoadCLRPackage();
            DirectoryInfo Allfolder = new DirectoryInfo((string)CONFIGINI["LUAPATH"]);
            Console.WriteLine("[ILUAR] Load! version = " + version);
            Console.WriteLine("[ILUAR] 本平台基于AGPL协议发行。");
            Console.WriteLine("[ILUAR] Reading Plugins...");
            foreach (FileInfo file in Allfolder.GetFiles("*.net.lua"))
            {
                try
                {
                    Console.WriteLine("[ILUAR] Load "+ (string)CONFIGINI["LUAPATH"] + file.Name);
                    lua.DoFile(file.FullName);
                    Console.Write("[ILUAR] ");
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    Console.WriteLine(file.Name + " load success");
                    Console.ForegroundColor = ConsoleColor.White;
                }
                catch (Exception e)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("[ILUAR] " + e.Message);
                    Console.WriteLine("[ILUAR] Filed to load " + file.Name);
                    Console.ForegroundColor = ConsoleColor.White;
                }
            }
            foreach (FileInfo file in Allfolder.GetFiles("*.ilp.lua"))
            {
                try
                {
                    Console.WriteLine("[ILUAR] Load " + (string)CONFIGINI["LUAPATH"] + file.Name);
                    string text = ILRDecrypt(File.ReadAllText(file.FullName));
                    lua.DoString(text);
                    Console.Write("[ILUAR] ");
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    Console.WriteLine(file.Name + " load success");
                    Console.ForegroundColor = ConsoleColor.White;
                }
                catch (Exception e)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("[ILUAR] " + e.Message.Replace("Base-64", "ILR-Protect"));
                    Console.WriteLine("[ILUAR] Filed to load " + file.Name);
                    Console.ForegroundColor = ConsoleColor.White;
                }
            }
            api.addBeforeActListener(EventKey.onLoadName, x =>
            {
                var a = BaseEvent.getFrom(x) as LoadNameEvent;
                ptr.Add(a.uuid, a.playerPtr);
                return true;
            });

            api.addBeforeActListener(EventKey.onPlayerLeft, x =>
            {
                var a = BaseEvent.getFrom(x) as PlayerLeftEvent;
                ptr.Remove(a.uuid);
                return true;
            });

        }
    }
}

namespace CSR
{
    partial class Plugin
    {
        public static void onStart(MCCSAPI api)
        {
            try
            {
                IronLuaRunner.IronLuaRunner.RunLua(api);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }            
            Console.WriteLine("[ILUAR]IronLuaRunner 装载完成");
        }
    }
}
