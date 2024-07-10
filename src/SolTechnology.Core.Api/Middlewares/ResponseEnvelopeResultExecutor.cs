using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SolTechnology.Core.CQRS;

namespace SolTechnology.Core.Api.Middlewares;

//FILTER is preferred
public class ResponseEnvelopeResultExecutor : ObjectResultExecutor
{
    public ResponseEnvelopeResultExecutor(OutputFormatterSelector formatterSelector, IHttpResponseStreamWriterFactory writerFactory, ILoggerFactory loggerFactory, IOptions<MvcOptions> mvcOptions) : base(formatterSelector, writerFactory, loggerFactory, mvcOptions)
    {
    }

    public override Task ExecuteAsync(ActionContext context, ObjectResult result)
    {
        Result<object> response;

        var resultValue = result.Value;
        var resultValueType = result.Value?.GetType();

        if (resultValueType is { IsGenericType: true } && resultValueType.GetGenericTypeDefinition() == typeof(Result<>))
        {
            response = new Result<object>
            {
                IsSuccess = (bool)resultValueType.GetProperty(nameof(Result<object>.IsSuccess))?.GetValue(resultValue)!,
                Data = resultValueType.GetProperty(nameof(Result<object>.Data))?.GetValue(resultValue)
            };
        }
        else if (resultValue is Result resultAsBase)
        {
            response = new Result<object>
            {
                IsSuccess = resultAsBase.IsSuccess,
                Error = resultAsBase.Error
            };
        }
        else
        {
            response = new Result<object>
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