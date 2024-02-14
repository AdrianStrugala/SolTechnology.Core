using Microsoft.AspNetCore.Http;
using System.Net;

namespace SolTechnology.Core.Api.Middlewares
{
    public class ExceptionHandlerMiddleware
    {
        private readonly RequestDelegate _next;

        public ExceptionHandlerMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception exception)
            {
                var response = context.Response;
                response.StatusCode = GetStatusCode(exception);

                var responseEnvelope = new ResponseEnvelope
                {
                    Error = exception.Message,
                    IsSuccess = false
                };
                await response.WriteAsJsonAsync(responseEnvelope);
            }
        }

        public int GetStatusCode(Exception exception)
        {
            int code;
            switch (exception)
            {

                case TaskCanceledException:
                    code = 499;
                    break;
                default:
                    code = (int)HttpStatusCode.BadRequest;
                    break;
            }
            return code;
        }
    }
}
