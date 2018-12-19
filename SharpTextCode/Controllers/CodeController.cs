using Microsoft.CodeDom.Providers.DotNetCompilerPlatform;
using Newtonsoft.Json.Linq;
using SharpTextCode.Structures;
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
        static List<ThreadInformation> Threads;
        static List<uint> WhiteList;
        static void Initialize()
        {
            WhiteList = new List<uint>();
            Threads = new List<ThreadInformation>();
        }

        [HttpGet]
        public HttpResponseMessage Get() => new HttpResponseMessage(HttpStatusCode.MethodNotAllowed);
        
        [HttpPost]
        public async Task<HttpResponseMessage> Post()
        {
            if (WhiteList == null || Threads == null)
                Initialize();

            StringBuilder response = new StringBuilder();
            string data = await Request.Content.ReadAsStringAsync();
            JObject jEntity = JObject.Parse(data);
            string type = (string)jEntity["type"];
            switch (type)
            {
                case "confirmation":
                    response.Append(Config.VerifyCode);
                    break;
                case "message_new":
                    ThreadInformation thread = new ThreadInformation(new Thread(Invoke));
                    Threads.Add(thread);
                    thread.Thr.Start(jEntity);
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

            CheckThreads();
            if (!WhiteList.Exists(x => x == fromID) && Config.AdministratorID != fromID)
                return;

            string message = "";
            try
            {
               message = ExecuteCommand(body);
                if (message == "")
                    return;
            }
            catch (Exception exc)
            {
                message = $"{exc.Message}\n\n{exc.StackTrace}";
            }

            Random random = new Random();
            string request = "https://api.vk.com/method/messages.send?" +
                $"message={message}&peer_id={peerID}&random_id={random.Next(1, 2147483647)}&access_token={Config.Token}&v=5.87";
            WebRequest.Create(request).GetResponse();
        }

        private MessageType GetCommand(string body)
        {
            string low = body.ToLower();
            if (low.IndexOf("fe") == 0)
                return MessageType.Compile;
            else if (low.IndexOf("ff") == 0)
                return MessageType.CompileFull;
            else if (low.IndexOf("wa") == 0)
                return MessageType.WhitelistAdd;
            else if (low.IndexOf("wr") == 0)
                return MessageType.WhitelistRemove;
            else if (low.IndexOf("wg") == 0)
                return MessageType.WhitelistGet;
            else if (low.IndexOf("tg") == 0)
                return MessageType.ThreadsGet;
            else if (low.IndexOf("tk") == 0)
                return MessageType.ThreadsKill;
            else if (low.IndexOf("system") == 0)
                return MessageType.System;
            else
                return MessageType.None;
        }
        private string ExecuteCommand(string body)
        {
            StringBuilder message = new StringBuilder();
            switch (GetCommand(body))
            {
                case MessageType.Compile:
                    message.Append(Execute(body.Remove(0, 2)));
                    break;
                case MessageType.CompileFull:
                    message.Append(Execute(body.Remove(0, 2), true));
                    break;
                case MessageType.WhitelistAdd:
                    {
                        uint id = uint.Parse(body.Remove(0, 2).Replace("\n", "").Replace(" ", ""));
                        WhiteList.Add(id);
                        message.Append($"User id {id} added to whitelist.");
                    }
                    break;
                case MessageType.WhitelistRemove:
                    {
                        uint id = uint.Parse(body.Remove(0, 2).Replace("\n", "").Replace(" ", ""));
                        WhiteList.Remove(id);
                        message.Append($"User id {id} removed of whitelist.");
                    }
                    break;
                case MessageType.WhitelistGet:
                    {
                        message.Append("Administrator: ");
                        message.Append(Config.AdministratorID);
                        message.Append('\n');
                        message.Append("Whitelist:\n");
                        foreach (uint id in WhiteList)
                            message.Append($"{id}\n");
                    }
                    break;
                case MessageType.ThreadsGet:
                    {
                        message.Append("Threads:\n");
                        foreach (ThreadInformation thread in Threads)
                        {
                            message.Append("----------\n");
                            message.Append($"ThreadID: {thread.Thr.ManagedThreadId}\n");
                            message.Append($"Thread state: {thread.Thr.ThreadState}\n");
                            message.Append($"Work time: {(DateTime.UtcNow - thread.StartTime).ToString()}\n");
                        }
                    }
                    break;
                case MessageType.ThreadsKill:
                    {
                        uint threadID = uint.Parse(body.Remove(0, 2).Replace("\n", "").Replace(" ", ""));
                        ThreadInformation? thread = Threads.Find(x => x.Thr.ManagedThreadId == threadID);
                        if (thread == null)
                        {
                            message.Append($"Thread {threadID} not found.");
                            break;
                        }
                        ((ThreadInformation)thread).Thr.Abort();
                        Threads.Remove((ThreadInformation)thread);
                        message.Append($"Thread {threadID} killed.");
                    }
                    break;
                case MessageType.System:
                    message.Append($"Uptime: {(DateTime.UtcNow - Config.StartTime).ToString()}\n");
                    message.Append($"OS Version: {Environment.OSVersion}\n");
                    message.Append($"Is x64 OS: {Environment.Is64BitOperatingSystem}\n");
                    break;
                case MessageType.None:
                default:
                    return "";
            }

            if (message.Length == 0)
                message.Append("Response empty.");
            return message.ToString();
        }
        private void CheckThreads()
        {
            foreach (ThreadInformation thread in Threads.ToArray())
                if (thread.Thr.ThreadState == ThreadState.Stopped)
                    Threads.Remove(thread);
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
