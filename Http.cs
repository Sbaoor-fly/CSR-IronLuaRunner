using NLua;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace HttpServer
{
    public class Http
    {
        public static Dictionary<String, HttpListener> https = new Dictionary<string, HttpListener>();
        private HttpListener http = new HttpListener();
        private LuaFunction get { get; set; }
        private LuaFunction post { get; set; }
        public string uid { get; set; }
        public Http(string ip, LuaFunction GET, LuaFunction POST)
        {
            http.Prefixes.Add(ip);
            http.TimeoutManager.EntityBody = TimeSpan.FromSeconds(30);
            http.TimeoutManager.RequestQueue = TimeSpan.FromSeconds(30);
            http.Start();
            http.BeginGetContext(ContextReady, null);
            get = GET;
            post = POST;
            uid = Guid.NewGuid().ToString();
        }
        private void ContextReady(IAsyncResult ar)
        {
            http.BeginGetContext(ContextReady, null);
            AcceptAsync(http.EndGetContext(ar));
        }
        private void AcceptAsync(HttpListenerContext context)
        {
            try
            {
                HttpListenerRequest request = context.Request;
                HttpListenerResponse response = context.Response;
                response.ContentEncoding = Encoding.UTF8;
                response.ContentType = "charset=UTF-8";
                string re = string.Empty;
                int? status = 200;
                switch (request.HttpMethod)
                {
                    case "GET":
                        var luaret = get.Call(request);
                        re = (luaret[0] as string) ?? "ok";
                        status = luaret[1] as int?;
                        break;
                    case "POST":
                        var luaret1 = post.Call(request);
                        re = (luaret1[0] as string) ?? "ok";
                        status = luaret1[1] as int?;
                        break;
                }
                context.Response.StatusCode = status ?? 200;
                var data = Encoding.UTF8.GetBytes(re);
                System.IO.Stream output = response.OutputStream;
                output.Write(data, 0, data.Length);
                response.StatusCode = 200;
                output.Close();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
         }
        public static bool stopHttpListner(string uid)
        {
            if (https.ContainsKey(uid))
            {
                https[uid].Stop();
                return true;
            }
            return false;
        }
    }
}
