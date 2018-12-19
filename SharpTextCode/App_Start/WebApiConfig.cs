using System.Web.Http;

namespace SharpTextCode
{
    public static class WebApiConfig
    {
        public static void Register(HttpConfiguration config)
        {
            Config.Initialize();

            config.MapHttpAttributeRoutes();

            config.Routes.MapHttpRoute(
                name: "DefaultApi",
                routeTemplate: "api/{controller}/{id}",
                defaults: new { id = RouteParameter.Optional }
            );
        }
    }
}
