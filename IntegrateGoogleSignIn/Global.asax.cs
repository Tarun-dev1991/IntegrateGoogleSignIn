using System;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using FluentScheduler;
using IntegrateGoogleSignIn.Services;


namespace IntegrateGoogleSignIn
{
    public class MvcApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();
            GlobalFilters.Filters.Add(new UserAuthenticationFilter());
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            UnityFactory.Initialize();

            JobManager.Initialize(new JobScheduler());
           
        }

        protected void Application_Error(object sender, EventArgs e)
        {
            Exception exception = Server.GetLastError();
            Response.Clear();

            HttpException httpException = exception as HttpException;

            if (httpException != null)
            {
                string action;

                switch (httpException.GetHttpCode())
                {

                    case 404:
                        // page not found
                        action = "HttpError404";
                        break;
                    case 500:
                        // server error
                        action = "HttpError500";
                        break;
                    default:
                        action = "General";
                        break;
                }

                // clear error on server
                Server.ClearError();

                    Response.Redirect(String.Format("~/Error/{0}/?message={1}", action, exception.Message));
            }
        }
    }
}
