using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;
using Gurock.SmartInspect;

namespace Fresh.Voice
{
    public class MvcApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);

            SiAuto.Si.Connections = "tcp(host=\"davidmillerco-vm.dev.local\", reconnect=\"true\", reconnect.interval=\"30\", async.enabled=\"true\")";
            SiAuto.Si.AppName = "Fresh.Voice";
            SiAuto.Si.Enabled = true;   
        }
    }
}
