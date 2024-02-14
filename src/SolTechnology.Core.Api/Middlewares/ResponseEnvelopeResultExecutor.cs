using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace SolTechnology.Core.Api.Middlewares
{
    public class ResponseEnvelopeResultExecutor : ObjectResultExecutor
    {
        public ResponseEnvelopeResultExecutor(OutputFormatterSelector formatterSelector, IHttpResponseStreamWriterFactory writerFactory, ILoggerFactory loggerFactory, IOptions<MvcOptions> mvcOptions) : base(formatterSelector, writerFactory, loggerFactory, mvcOptions)
        {
        }

        public override Task ExecuteAsync(ActionContext context, ObjectResult result)
        {
            var response = new ResponseEnvelope<object>
            {
                Data = result.Value,
                IsSuccess = true
            };

            TypeCode typeCode = Type.GetTypeCode(result.Value?.GetType());
            if (typeCode == TypeCode.Object)
                result.Value = response;

            return base.ExecuteAsync(context, result);
        }
    }
}
