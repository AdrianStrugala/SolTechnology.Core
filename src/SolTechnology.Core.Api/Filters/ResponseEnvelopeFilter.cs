using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc;
using SolTechnology.Core.CQRS;

namespace SolTechnology.Core.Api.Filters;

public class ResponseEnvelopeFilter : IResultFilter
{
    public void OnResultExecuting(ResultExecutingContext context)
    {
        if (context.Result is ObjectResult objectResult)
        {
            var resultValue = objectResult.Value;
            var resultValueType = resultValue?.GetType();

            ResponseEnvelope<object> response;
            if (resultValueType is { IsGenericType: true } && resultValueType.GetGenericTypeDefinition() == typeof(Result<>))
            {
                response = new ResponseEnvelope<object>
                {
                    IsSuccess = (bool)resultValueType.GetProperty(nameof(Result<object>.IsSuccess))?.GetValue(resultValue)!,
                    Data = resultValueType.GetProperty(nameof(Result<object>.Data))?.GetValue(resultValue)
                };
            }
            else if (resultValue is Result resultAsBase)
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


            //TODO: that's not so stupid as well:
            // var envelope = new
            // {
            //     Success = objectResult.StatusCode >= 200 && objectResult.StatusCode < 300,
            //     Data = objectResult.Value
            // };
            context.Result = new ObjectResult(response)
            {
                StatusCode = objectResult.StatusCode
            };
        }
    }

    public void OnResultExecuted(ResultExecutedContext context)
    {
        // Do nothing
    }
}