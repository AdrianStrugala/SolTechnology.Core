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

            if (resultValueType is { IsGenericType: true } && resultValueType.GetGenericTypeDefinition() == typeof(Result<>))
            {
                return;
            }

            if (resultValue is Result)
            {
                return;
            }

            context.Result = new ObjectResult(new Result<object?>
            {
                Data = resultValue,
                IsSuccess = true
            })
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