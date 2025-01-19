using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Newtonsoft.Json;
using Serilog;
using System;
using System.Net;
using System.Threading.Tasks;
using ILogger = Serilog.ILogger;

namespace OnlineBookStore.Application.Common.Utility
{
    public class ExceptionHandler(RequestDelegate next, ILogger logger)
    {
        private readonly RequestDelegate _next = next;
        private readonly ILogger _logger = logger;

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                var routeData = context.GetRouteData();
                var controllerName = routeData.Values["controller"];
                var actionName = routeData.Values["action"];
                var exceptionMessage = ex.Message;
                var stackTrace = ex.StackTrace;
                var Source = ex.Source;
                var InnerExcepton = ex.InnerException;
                var HResult = ex.HResult;
                var StatusCode = context.Response.StatusCode;


                // Log the exception message and stack trace
                _logger.Error($"\n\nAn unhandled exception occurred in {controllerName}:- {actionName}. \nMessage: {exceptionMessage} \nSource: {Source} \nstacktrace:{stackTrace} \nInnerException:{InnerExcepton} \nHresult:{HResult}");

                context.Response.ContentType = "application/json";

                var response = new ExceptionDetails()
                {
                    exceptionMessage = exceptionMessage,
                    StatusCode = StatusCode,
                };

                //context.Response.Clear();
                //context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

                await context.Response.WriteAsync(JsonConvert.SerializeObject(response));
            }
        }
    }
}
