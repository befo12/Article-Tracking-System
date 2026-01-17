using System.Web.Http;

namespace WebApplication1
{
    public static class WebApiConfig
    {
        public static void Register(HttpConfiguration config)
        {
            // JSON formatı (opsiyonel ama faydalı)
            config.Formatters.Remove(config.Formatters.XmlFormatter);

            // Attribute routing
            config.MapHttpAttributeRoutes();

            // /api/{controller}/{action}
            config.Routes.MapHttpRoute(
                name: "ApiWithAction",
                routeTemplate: "api/{controller}/{action}"
            );
        }
    }
}
