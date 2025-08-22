using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using System.Web.Security;
using System.Web.SessionState;
using Telerik.Sitefinity.Abstractions;
using Telerik.Sitefinity.Configuration;
using Telerik.Sitefinity.Data;
using Telerik.Sitefinity.Modules.Forms.Events;
using Telerik.Sitefinity.Services.Search.Configuration;
using Telerik.Sitefinity.Services;
using System.Web.Hosting;
using Telerik.Sitefinity.Modules.Libraries.Configuration;

namespace SitefinityWebApp
{
    public class Global : System.Web.HttpApplication
    {

        protected void Application_Start(object sender, EventArgs e)
        {

            MvcHandler.DisableMvcResponseHeader = true;

            Bootstrapper.Initialized += new EventHandler<Telerik.Sitefinity.Data.ExecutedEventArgs>(Bootstrapper_Initialized);

        }

        private void Bootstrapper_Initialized(object sender, ExecutedEventArgs e)
        {
            if (e.CommandName == "Bootstrapped")
            {
                ConfigManager configManager = ConfigManager.GetManager();
                using (new ElevatedModeRegion(configManager))
                {
                    var config = configManager.GetSection<SearchConfig>();
                    configManager.SaveSection(config);
                }
            }
        }

        protected void Session_Start(object sender, EventArgs e)
        {

        }

        protected void Application_BeginRequest(object sender, EventArgs e)
        {
            if (!HttpContext.Current.Request.RawUrl.StartsWith("/Sitefinity"))
            {
                HttpContext.Current.Response.AddHeader("X-Frame-Options", "SAMEORIGIN");
                HttpContext.Current.Response.Headers.Remove("Server");
                HttpContext.Current.Response.Headers.Remove("X-AspNet-Version");
                HttpContext.Current.Response.Headers.Remove("X-SourceFiles");
            }
        }

        protected void Application_AuthenticateRequest(object sender, EventArgs e)
        {

        }

        protected void Application_Error(object sender, EventArgs e)
        {

        }

        protected void Session_End(object sender, EventArgs e)
        {

        }

        protected void Application_End(object sender, EventArgs e)
        {

        }
    }
}