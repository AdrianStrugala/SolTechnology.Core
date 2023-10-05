using Microsoft.AspNetCore.Mvc;
using SolTechnology.Core.CQRS;

namespace SolTechnology.Core.Api;

public abstract class BaseController : ControllerBase
{
    [NonAction]
    public async Task<IActionResult> Return<T>(Task<T> handle)
    {
        var response = new ResponseEnvelope<T>();
        try
        {
            response.Data = await handle;
            response.IsSuccess = true;

            return new OkObjectResult(response);
        }
        catch (Exception e)
        {
            response.Error = e.Message;
            response.IsSuccess = false;

            return new BadRequestObjectResult(response);
        }
    }

    [NonAction]
    public async Task<IActionResult> Invoke(Task handle)
    {
        var response = new ResponseEnvelope();
        try
        {
            await handle;
            response.IsSuccess = true;

            return new OkObjectResult(response);
        }
        catch (Exception e)
        {
            response.IsSuccess = false;
            response.Error = e.Message;

            return new BadRequestObjectResult(response);
        }
    }

    [NonAction]
    public async Task<IActionResult> Invoke(Task<CommandResult> handle)
    {
        var response = new ResponseEnvelope();
        try
        {
            var result = await handle;
            response.IsSuccess = result.IsSuccess;

            if (response.IsSuccess)
            {
                return new OkObjectResult(response);
            }
            else
            {
                response.Error = result.ErrorMessage;
                return new BadRequestObjectResult(response);
            }

        }
        catch (Exception e)
        {
            response.IsSuccess = false;
            response.Error = e.Message;

            return new BadRequestObjectResult(response);
        }
    }

    [NonAction]
    public async Task<IActionResult> Invoke<T>(Task<CommandResult<T>> handle)
    {
        var response = new ResponseEnvelope<T>();
        try
        {
            var result = await handle;
            response.IsSuccess = result.IsSuccess;

            if (response.IsSuccess)
            {
                response.Data = result.Data;
                return new OkObjectResult(response);
            }
            else
            {
                response.Error = result.ErrorMessage;
                return new BadRequestObjectResult(response);
            }

        }
        catch (Exception e)
        {
            response.IsSuccess = false;
            response.Error = e.Message;

            return new BadRequestObjectResult(response);
        }
    }
}