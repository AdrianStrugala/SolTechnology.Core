using DreamTravel.AvroConvertOnline.GenerateModel;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace DreamTravel.Api.AvroConvertOnline
{
    public class AvroConvertOnlineController : Controller
    {
        private readonly IGenerateModelHandler _generateModelHandler;
        private readonly ILogger<AvroConvertOnlineController> _logger;

        public AvroConvertOnlineController(IGenerateModelHandler generateModelHandler, ILogger<AvroConvertOnlineController> logger)
        {
            _generateModelHandler = generateModelHandler;
            _logger = logger;
        }

        [HttpPost]
        [Route("api/avro/generateModel")]
        public IActionResult PostSchema([FromBody] GenerateModelRequest request)
        {
            var response = _generateModelHandler.Handle(request);

            if (response.IsFailure)
            {
                _logger.LogError(response.Error, $"Exception from schema: {request.Schema}");
                return new BadRequestObjectResult(response.Error.Message);
            }

            return new OkObjectResult(response.Value);
        }
    }
}
