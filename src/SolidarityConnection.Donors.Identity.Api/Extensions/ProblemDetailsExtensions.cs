using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;

namespace SolidarityConnection.Donors.Identity.Api.Extensions
{
    public static class ProblemDetailsExtensions
    {
        public static ProblemDetails CreateProblemDetails(this ControllerBase controller, int? status = null, string? title = null, string? detail = null, string? type = null)
        {
            var factory = controller.HttpContext.RequestServices.GetService<ProblemDetailsFactory>();
            var statusCode = status ?? controller.Response.StatusCode;

            if (factory != null)
            {
                return factory.CreateProblemDetails(controller.HttpContext, statusCode, title, type, detail, controller.Request.Path);
            }

            return new ProblemDetails
            {
                Status = statusCode,
                Title = title,
                Detail = detail,
                Instance = controller.Request.Path
            };
        }

        public static IActionResult NotFoundProblem(this ControllerBase controller, string title, string? detail = null, string? type = null)
        {
            var pd = controller.CreateProblemDetails(StatusCodes.Status404NotFound, title, detail, type);
            return controller.NotFound(pd);
        }

        public static IActionResult BadRequestProblem(this ControllerBase controller, string title, string? detail = null, string? type = null)
        {
            var pd = controller.CreateProblemDetails(StatusCodes.Status400BadRequest, title, detail, type);
            return controller.BadRequest(pd);
        }

        public static IActionResult UnauthorizedProblem(this ControllerBase controller, string title, string? detail = null, string? type = null)
        {
            var pd = controller.CreateProblemDetails(StatusCodes.Status401Unauthorized, title, detail, type);
            return controller.Unauthorized(pd);
        }

        public static IActionResult Problem(this ControllerBase controller, int status, string title, string? detail = null, string? type = null)
        {
            var pd = controller.CreateProblemDetails(status, title, detail, type);
            return controller.StatusCode(status, pd);
        }

        public static ProblemDetails CreateProblemDetails(this HttpContext context, int? status = null, string? title = null, string? detail = null, string? type = null)
        {
            var factory = context.RequestServices.GetService<ProblemDetailsFactory>();
            var statusCode = status ?? context.Response.StatusCode;

            if (factory != null)
            {
                return factory.CreateProblemDetails(context, statusCode, title, type, detail, context.Request.Path);
            }

            return new ProblemDetails
            {
                Status = statusCode,
                Title = title,
                Detail = detail,
                Instance = context.Request.Path
            };
        }
    }
}

