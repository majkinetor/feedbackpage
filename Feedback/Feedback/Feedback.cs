using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net.Mail;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Web;
using System.Xml;
using Templates;

namespace Feedback
{
    public class FeedbackPanel
    {
        internal HttpApplication application;
        internal HttpRequest request { get { return application.Context.Request; } }

        static void WriteScriptResourceToFile(string path)
        {
            string resourceName = "Feedback.js";
            string fileName = path + resourceName;
            if (File.Exists(fileName)) return;

            string asmname = Assembly.GetExecutingAssembly().FullName;
            string resname = String.Format("{0}.{1}", asmname.Substring(0, asmname.IndexOf(',')), resourceName);
            Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resname);

            FileStream f = new FileStream(fileName, FileMode.OpenOrCreate);
            Byte[] buffer = new Byte[stream.Length];
            stream.Read(buffer, 0, (int)stream.Length);
            f.Write(buffer, 0, (int)stream.Length);
            f.Close();
        }
        internal static Config config = ConfigurationManager.GetSection(Config.Namespace) as Config ?? new Config();
        static readonly FeedbackWriter[] feedbackWriters = CreateWriters();
        static FeedbackWriter[] CreateWriters()
        {
            FeedbackWriter[] res = new FeedbackWriter[config.writers.Count(x => x.enabled)];
            int i = 0;
            foreach (Config.Writer writer in config.writers)
            {
                if (!writer.enabled) continue;

                Type t = Type.GetType(writer.type);
                res[i] = (FeedbackWriter)t.GetConstructor(new Type[0]).Invoke(null);
                if (writer.args != null) res[i].args = writer.args;   //args can never be null
                i++;
            }
            if (res.Length == 0) res = new FeedbackWriter[] { new DefaultWriters.TextWriter() };
            return res;
        }

        #region Template
        [Template]
        protected static string cssTemplate = config.style;
              
        protected static string _template = @"
            [cssTemplate]
            
            <div id='[#]body' style='display: none'>

                <div id='[#]caption'><div id='[#]title'>[FeedbackTitle]</div><div id='[#]icons'><a href='javascript:Feedback.toggle()'>_</a><a href='javascript:Feedback.hide()'>x</a></div></div>
                
                <div id='[#]header' style='display: none'>[FeedbackHeader]</div>

                <div id='[#]content' style='display: none'>
                    <form enctype='multipart/form-data' action='[url]' method='post' id='[#]form' onsubmit=""return AIM.submit(this, { 'onComplete' : Feedback.finish } )"">
                        <textarea name='[#]text' id='[#]text'></textarea>
                        <input name='[#]file' id='[#]file' type='file'/>
		                <input id='[#]submit'type='submit' value='submit' />
                    </form>
                </div>
            </div>
            <script type='text/javascript' src='[res]'></script>
            <script>
                    Feedback.init('[#]', '[FeedbackDockType]');                                                
            </script>
    ";
        #endregion HTML

        #region Public Properties

        public string FeedbackDockType  = config.dock;
        public string FeedbackTitle     = config.title;
        public string FeedbackHeader    = config.header;
        public string FeedbackPrefix    = config.prefix;
        public bool   FeedbackEnabled   = config.enabled;
        
        #endregion properties

        internal void Save()
        {
            if (!IsPostBack()) return;
            
            Input i = new Input
            {
                Description = request.Form[FeedbackPrefix + "text"],
                Attachment = request.Files[FeedbackPrefix + "file"],
                Time = DateTime.Now,

                //                  Request = context.Handler is IRequiresSessionState ? context.Session[FeedbackPrefix + "Request"] as HttpRequest : null,
                Browser = request.Browser,
                Path = request.Path,
                ApplicationPath = request.ApplicationPath,
                PhysicalPath = request.PhysicalPath,
                PhysicalApplicationPath = request.PhysicalApplicationPath
            };

            RunWriters(i);
            application.Response.Write("OK");
            application.Response.End();      
        }
        internal void Load()
        {
            if (IsPostBack()) return;
            string ext = VirtualPathUtility.GetExtension(request.FilePath).ToLower();

            if (!FeedbackEnabled || IsExcluded(request.Url.ToString()) || !IsValidExtension(ext)) return;
            
            //if (context.Handler is IRequiresSessionState)
            //    context.Session[FeedbackPrefix + "Request"] = request;

            Create();
        }
        internal void Create()
        {
            if (String.IsNullOrEmpty(FeedbackTitle)) FeedbackTitle = request.ApplicationPath.Substring(1);

            Template t = new Template { Delimiters = "[ ]" };
            t["#"] = FeedbackPrefix;
            t["url"] = request.Path;
            t["res"] = "/Feedback.js";
            string result = t.Parse(this);//, TOption.DelBlanks, TOption.Compact);

            application.Context.Response.Write(result);
        }
        
        internal static void RunWriters(Input user) {
            foreach (FeedbackWriter writer in feedbackWriters) writer.WriteIssue(user);
        }

        internal bool IsPostBack()
        {
            return !String.IsNullOrEmpty(request.Form[FeedbackPrefix + "text"]);
        }
        internal bool IsExcluded(string url)
        {
            if (config.exclude == null) return false;

            foreach (string u in config.exclude)
            {
                if (u.EndsWith("*") && url.StartsWith(u.Remove(u.Length - 1), StringComparison.OrdinalIgnoreCase)) return true;
                if (u.Equals(url, StringComparison.OrdinalIgnoreCase)) return true;
            }
            return false;
        }        
        internal bool IsValidExtension(string ext)
        {
            string[] extensions = config.ext.Split(';');
            foreach( string e in extensions )
                if (ext == e) return true;

            return false;
        }
    }
    internal class Config : IConfigurationSectionHandler
    {
        public readonly static string Namespace = MethodBase.GetCurrentMethod().DeclaringType.Namespace;
        public struct Writer
        {
            public bool enabled;
            public string type;
            public Dictionary<string, string> args;
        }

        public string title     = String.Empty;
        public string header    = String.Empty;
        public string dock      = "right";
        public bool   enabled   = true;
        public string width     = "300px";
        public string prefix    = Namespace + "_";
        public string ext       = ".asp;.aspx;.htm;.html;";
        public string[] exclude;
        public Writer[] writers = new Writer[0];
        #region Style 
        public string style     = @"
        <style>
          #body {
	          width: [config.width]; 
	          position: absolute; 
              zIndex: 10000;
          }
          #caption {
	          background: #fff8dc;
	          border:1px #bbbbbb solid;
	          padding: 3px;
	          overflow: hidden;
          }
          #title {
	          font-weight: bold;
	          float: left;
	          background:transparent;
	          width:85%;
          }
          #icons {
	          text-align: right;
	          background:transparent;
          }
          #icons  a {
	          padding-right: 10px; 
	          text-decoration: none;
          }
          #header {
	          width: 100%;
	          padding-top: 10px;		
          }
          #form{
	          display: inline;
          }
          #text {
	          width: 98%; 
	          height: 300px;	 
	          border: 1px;
	          background: #fffff4;
          }
          #file {
	          width: 100%;
          }
          #submit {
	          margin-top: 10px;
	          text-align: center;	
	          width:99%;
	          border: 1px solid;
          }                         
        </style>";
        #endregion style

        public Config()
        {
            Regex r = new Regex(@"^\s*#", RegexOptions.Multiline);
            style = r.Replace(style, "#[#]"); 
        }
        public object Create(object parent, object configContext, XmlNode section)
        {
            Get(section); 
            return this;
        }
        void Get(XmlNode s) {
            Regex r = new Regex(@"^\s*#", RegexOptions.Multiline);
            XmlAttributeCollection a = s.Attributes;
            header  = s["header"]  != null ? s["header"].InnerText   : header;
            title   = s["title"]   != null ? s["title"].InnerText    : title;
            dock    = a["dock"]    != null ? a["dock"].InnerText     : dock;
            prefix  = a["prefix"]  != null ? a["prefix"].InnerText   : prefix;
            width   = a["width"]   != null ? a["width"].InnerText    : width;
            enabled = a["enabled"] != null ? a["enabled"].InnerText.ToLower() == "true" : enabled;
            style  =  s["style"]   != null ? "<style>" + r.Replace(s["style"].InnerText , "#[#]") + "</style>" : style;
            ext     = s["ext"]     != null ? s["ext"].InnerText.ToLower() : ext;

            string e = s["exclude"] != null ? s["exclude"].InnerText : String.Empty;

            e = e.Replace(" ", "").Replace("\t", "");
            exclude = e.Split(new string[]{"\r\n"}, StringSplitOptions.RemoveEmptyEntries);   //!!! not cross platform, new line string

            XmlNode w = s["writers"];
            if (w == null || w.ChildNodes.Count == 0) return;          
            
            XmlNodeList xmlWriters = w.SelectNodes("writer");
            writers = new Writer[xmlWriters.Count];
            
            for(int i=0; i < xmlWriters.Count; i++)
            {
                if (xmlWriters[i].Attributes["type"] == null) throw new Exception("<writer> tag must have 'type' attribute");

                writers[i].type     = xmlWriters[i].Attributes["type"].InnerText;
                writers[i].enabled = xmlWriters[i].Attributes["enabled"] != null ? !(xmlWriters[i].Attributes["enabled"].InnerText.ToLower() == "false") : true;

                if (xmlWriters[i].ChildNodes.Count > 0) writers[i].args = new Dictionary<string, string>(5); else continue;
                foreach (XmlNode arg in xmlWriters[i])
                    if (arg.InnerText.Trim() != String.Empty )              //make sure writers don't have to check IsNullOrEmpty, but only for null
                        writers[i].args.Add(arg.Name, arg.InnerText);            
            }
        }
    }
  
    public class Input
    {
        public string Description               { get; internal set; }
        public HttpPostedFile Attachment        { get; internal set; }
        public DateTime Time                    { get; internal set; }

        public HttpBrowserCapabilities Browser  { get; internal set; }
        public string Parameters                { get; internal set; }
        public string PhysicalPath              { get; internal set; }
        public string PhysicalApplicationPath   { get; internal set; }
        public string ApplicationPath           { get; internal set; }
        public string Path                      { get; internal set; }
//        public HttpRequest Request              { get; internal set; }     //I can't save it if sesionstate is false so it can be null, hence some upper fields that can be also obtained via this field.
    }
    public abstract class FeedbackWriter
    {
        public Dictionary<string, string> args = new Dictionary<string,string>(3);
        public abstract void WriteIssue(Input user);
    }
    namespace DefaultWriters
    {
        static class DefaultTemplate
        {
            public static readonly string template =
@"Page: {Path}
Time: {Time}
Browser: {Browser.Browser} v{Browser.Version}
Attachment: {Attachment}                             

{Description}";
        }

        public class TextWriter : FeedbackWriter
        {
            string DataFolder;

            public override void WriteIssue(Input user)
            {
                SetConfig(user);
                HttpPostedFile f = user.Attachment;
                string fileName = null;
                if (f != null && f.ContentLength != 0)
                {
                    fileName = DataFolder + Directory.GetFiles(DataFolder).Length + ". " + Path.GetFileName(f.FileName);
                    f.SaveAs(fileName);
                }

                System.IO.TextWriter tw = new StreamWriter(DataFolder + "Issues.txt", true);
                Template t = new Template(DefaultTemplate.template);

                t["Attachment"] = fileName != null ? String.Format("{0} ({1:F2} KB)", fileName, f.ContentLength / 1024.0) : String.Empty;
                tw.WriteLine(t.Parse(user));
                tw.WriteLine("-".PadLeft(40,'-'));
                tw.Close();
            }

            void SetConfig(Input user)
            {
                DataFolder = args.ContainsKey("DataFolder") ? args["DataFolder"] : user.PhysicalApplicationPath + "Feedback";
                if (!DataFolder.EndsWith(Path.DirectorySeparatorChar.ToString())) DataFolder += Path.DirectorySeparatorChar;
                if (!Directory.Exists(DataFolder)) Directory.CreateDirectory(DataFolder);
            }
        }

        public class EmailWriter : FeedbackWriter
        {
            string Smtp;
            string Email;
            string Sender = Config.Namespace + "@localhost";
            int Timeout = 30;

            public override void WriteIssue(Input user)
            {
                SetConfig();

                MailMessage message = new MailMessage();
                message.To.Add(this.Email);
                message.Subject = user.ApplicationPath.Substring(1) + Config.Namespace;
                message.From = new MailAddress(this.Sender);

                HttpPostedFile f = user.Attachment;
                if (f != null && f.ContentLength != 0) message.Attachments.Add(new Attachment(f.InputStream, f.FileName));

                Template t = new Template(DefaultTemplate.template);
                t["Attachment"] = (f != null && f.ContentLength != 0) ? String.Format("{0} ({1:F2} KB)", f.FileName, f.ContentLength / 1024.0) : String.Empty;
                message.Body = t.Parse(user);

                SmtpClient smtp = new SmtpClient { Host = this.Smtp, Timeout = this.Timeout * 1000 };

                smtp.Send(message);
            }
            void SetConfig()
            {
                if (!args.ContainsKey("Smtp")) throw new Exception("EmailWriter mandatory argument missing: Smtp");
                Smtp  = args["Smtp"];
                
                if (!args.ContainsKey("Email")) throw new Exception("EmailWriter mandatory argument missing: Email");
                Email = args["Email"];

                if (args.ContainsKey("Sender")) Sender = args["Sender"];

                if (args.ContainsKey("Timeout")) int.TryParse(args["Timeout"], out Timeout);
            }
        }
    }

    public class FeedbackPage : System.Web.UI.Page
    {
        private FeedbackPanel f = new FeedbackPanel();
        public FeedbackPanel Feedback { get { return f; } }

        public FeedbackPage()
        {
            f.application = Context.ApplicationInstance;
        }
                     
        protected override void OnPreInit(EventArgs e)
        {
            base.OnPreInit(e);
            f.Save();
        }
        protected override void OnUnload(EventArgs e)
        {
            base.OnUnload(e);
            f.Load();
        }
    }
    public class FeedbackModule : IHttpModule
    {
        FeedbackPanel f = new FeedbackPanel();
        public String ModuleName  {   get { return Config.Namespace + "Module"; }   }

        public void Init(HttpApplication application)
        {
            application.BeginRequest += (new EventHandler(this.Application_BeginRequest));
            application.EndRequest += (new EventHandler(this.Application_EndRequest));
            f.application = application;
        }

        private void Application_BeginRequest(Object source, EventArgs e)
        {                
            f.Save();
        }
        private void Application_EndRequest(Object source, EventArgs e)
        {
            f.Load();
        }

        public void Dispose() {   
            //remove javascript resource
        }
    }
}


