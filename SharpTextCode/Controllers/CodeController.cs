using Microsoft.CodeDom.Providers.DotNetCompilerPlatform;
using Newtonsoft.Json.Linq;
using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;

namespace SharpTextCode.Controllers
{
    public class CodeController : ApiController
    {
        const string Token = "Group token";
        const string VerifyCode = "Code for verify server";

        static List<uint> WhiteList;
        static void InitializeWhiteList()
        {
            WhiteList = new List<uint>
            {
                //Added ID to a page.
            };
        }

        public HttpResponseMessage Get()
        {
            return new HttpResponseMessage()
            {
                StatusCode = HttpStatusCode.MethodNotAllowed
            };
        }
        
        [HttpPost]
        public async Task<HttpResponseMessage> Post()
        {
            if (WhiteList == null)
                InitializeWhiteList();

            StringBuilder response = new StringBuilder();
            string data = await Request.Content.ReadAsStringAsync();
            JObject jEntity = JObject.Parse(data);
            string type = (string)jEntity["type"];
            switch (type)
            {
                case "confirmation":
                    response.Append(VerifyCode);
                    break;
                case "message_new":
                    new Thread(Invoke).Start(jEntity);
                    break;
            }
            if (type != "confirmation")
                response.Append("ok");
            return new HttpResponseMessage()
            {
                Content = new StringContent(
                response.ToString(),
                Encoding.UTF8,
                "text/html")
            };
        }

        private void Invoke(object obj)
        {
            JObject entity = (JObject)obj;
            uint fromID = (uint)entity["object"]["from_id"];
            string peerID = (string)entity["object"]["peer_id"];
            string body = (string)entity["object"]["text"];

            if (!WhiteList.Exists(x => x == fromID))
                return;

            string message = "";

            if (body.IndexOf("FE") == 0)
                message = Execute(body.Remove(0, 2));
            else if (body.IndexOf("FF") == 0)
                message = Execute(body.Remove(0, 2), true);
            else if (body.IndexOf("WA") == 0)
            {
                uint id = uint.Parse(body.Remove(0, 2).Replace("\n", "").Replace(" ", ""));
                WhiteList.Add(id);
                message = $"User id {id} added to whitelist.";
            }
            else if (body.IndexOf("WR") == 0)
            {
                uint id = uint.Parse(body.Remove(0, 2).Replace("\n", "").Replace(" ", ""));
                WhiteList.Remove(id);
                message = $"User id {id} removed of whitelist.";
            }
            else if (body.IndexOf("WG") == 0)
                foreach (uint id in WhiteList)
                    message += $"{id}\n";
            else
                return;

            if (message == "")
                message = "Response empty.";

            Random random = new Random();
            string request = "https://api.vk.com/method/messages.send?" +
                $"message={message}&peer_id={peerID}&random_id={random.Next(1, 2147483647)}&access_token={Token}&v=5.87";
            WebRequest.Create(request).GetResponse();
        }

        readonly string CFirst = "using System;\n" +
            "class Executor\n" +
            "{\n" +
            "public static string FHRS = \"\";\n" +
            "public static void Write(object obj){FHRS += obj;}\n" +
            "public static string Execute()\n" +
            "{\n";
        readonly string CLast = "return FHRS;\n}\n}";
        private string Execute(string code, bool codeFull = false)
        {
            StringBuilder codeForCompile = new StringBuilder();
            if (!codeFull)
                codeForCompile.Append(CFirst);
            codeForCompile.Append(code);
            if (!codeFull)
                codeForCompile.Append(CLast);

            CompilerParameters options = new CompilerParameters
            {
                GenerateInMemory = true
            };
            CompilerResults result = new CSharpCodeProvider().CompileAssemblyFromSource(options, codeForCompile.ToString());
            if (!result.Errors.HasErrors)
                return (string)result.CompiledAssembly.GetType("Executor").InvokeMember("Execute", BindingFlags.Static | BindingFlags.InvokeMethod | BindingFlags.Public, null, null, null);
            else
            {
                StringBuilder errs = new StringBuilder();
                foreach (CompilerError error in result.Errors)
                    errs.Append($"{error.ErrorText}\n");
                return errs.ToString();
            }
        }
    }
}
