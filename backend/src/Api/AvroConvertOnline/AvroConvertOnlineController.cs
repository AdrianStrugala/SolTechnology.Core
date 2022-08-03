using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SolTechnology.Avro;

namespace DreamTravel.Api.AvroConvertOnline
{
    public class AvroConvertOnlineController : Controller
    {
        private readonly ILogger<AvroConvertOnlineController> _logger;

        public AvroConvertOnlineController(ILogger<AvroConvertOnlineController> logger)
        {
            _logger = logger;
        }

        [HttpPost]
        [Route("api/avro/generateModel")]
        public IActionResult PostSchema(GenerateModelRequest request)
        {
            try
            {
                var result = AvroConvert.GenerateModel(request.Schema);
                return new OkObjectResult(result);
            }
            catch (Exception e)
            {
                _logger.LogError(e, $"Exception from schema: {request.Schema}");
                return BadRequest(e.Message);
            }
        }
    }
}
