using Microsoft.AspNetCore.Mvc;

namespace SolTechnology.Core.Api;

public abstract class BaseController : ControllerBase
{
    public async Task<IActionResult> Return<T>(Task<T> handle)
    {
        var response = new Response<T>();
        try
        {
            response.Result = await handle;
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

    public async Task<IActionResult> Invoke(Task handle)
    {
        var response = new Response();
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