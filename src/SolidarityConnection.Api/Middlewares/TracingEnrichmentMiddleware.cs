using System.Diagnostics;

namespace SolidarityConnection.Api.Middlewares
{
    public class TracingEnrichmentMiddleware
    {
        private readonly RequestDelegate _next;

        public TracingEnrichmentMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var activity = Activity.Current;
            if (activity != null)
            {
                activity.SetTag("http.user_agent", context.Request.Headers["User-Agent"].ToString());
                activity.SetTag("http.request_id", context.TraceIdentifier);
                if (context.User.Identity?.IsAuthenticated == true)
                {
                    activity.SetTag("system.version", "v1");
                    activity.SetTag("user.id", context.User.Identity.Name);
                }
            }
            await _next(context);
        }
    }
}

