using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using Microsoft.AspNetCore.Cors;

namespace WebAPI.Controllers
{
    #region snippet
    [EnableCors]
    [Route("api/[controller]")]
    [ApiController]
    public class ValuesController : ControllerBase
    {
        // GET api/values
        [HttpGet]
        public ActionResult<IEnumerable<string>> Get()
        {
            return new string[] { "value1", "value2" };
        }

        // GET api/values/5
        [HttpGet("{id}")]
        public ActionResult<string> Get(int id)
        {
            switch (id)
            {
                case 1:
                    return "1st Val";
                case 2:
                    return "2nd val";
                default:
                    return NotFound();
            }
        }
       
    }
    #endregion
}
