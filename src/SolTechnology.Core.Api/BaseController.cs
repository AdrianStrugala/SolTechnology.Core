using Microsoft.AspNetCore.Mvc;

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
}