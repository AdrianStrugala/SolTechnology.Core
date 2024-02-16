using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SolTechnology.Core.CQRS;

namespace SolTechnology.Core.Api.Middlewares
{
    public class ResponseEnvelopeResultExecutor : ObjectResultExecutor
    {
        public ResponseEnvelopeResultExecutor(OutputFormatterSelector formatterSelector, IHttpResponseStreamWriterFactory writerFactory, ILoggerFactory loggerFactory, IOptions<MvcOptions> mvcOptions) : base(formatterSelector, writerFactory, loggerFactory, mvcOptions)
        {
        }

        public override Task ExecuteAsync(ActionContext context, ObjectResult result)
        {
            ResponseEnvelope<object> response;

            var resultValue = result.Value;
            var resultValueType = result.Value?.GetType();

            if (resultValueType is { IsGenericType: true } && resultValueType.GetGenericTypeDefinition() == typeof(OperationResult<>))
            {
                response = new ResponseEnvelope<object>
                {
                    IsSuccess = (bool)resultValueType.GetProperty(nameof(OperationResult<object>.IsSuccess))?.GetValue(resultValue)!,
                    Data = resultValueType.GetProperty(nameof(OperationResult<object>.Data))?.GetValue(resultValue)
                };
            }
            else if (resultValue is OperationResult resultAsBase)
            {
                response = new ResponseEnvelope<object>
                {
                    IsSuccess = resultAsBase.IsSuccess,
                    Error = resultAsBase.ErrorMessage
                };
            }
            else
            {
                response = new ResponseEnvelope<object>
                {
                    Data = resultValue,
                    IsSuccess = true
                };
            }

            TypeCode typeCode = Type.GetTypeCode(resultValueType);
            if (typeCode == TypeCode.Object)
                result.Value = response;

            return base.ExecuteAsync(context, result);
        }
    }
}
