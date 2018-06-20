using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using DreamTravel.Models;

namespace DreamTravel.Controllers
{
    public class UserController : Controller
    {
        private readonly UserRepository _userRepository;
        public UserController(DbConnectionFactory dbConnectionFactory)
        {
            _userRepository = new UserRepository(dbConnectionFactory);
        }

        [HttpPost]
        public async Task<IActionResult> Add(string name, string password)
        {
            try
            {
                User toAdd = new User
                {
                    Name = name,
                    Password = password,
                    Happiness = 0
                };

                await _userRepository.Add(toAdd);

            }
            catch (Exception e)
            {
                return BadRequest();
            }

            return Ok();
        }

        [HttpGet]
        [Route("Get/{name}")]
        public async Task<IActionResult> Get(string name)
        {
            User user;
            try
            {
                user = await _userRepository.Get(name);
            }
            catch (Exception e)
            {
                return BadRequest();
            }

            if (user == null)
            {
                return NotFound();
            }
            return Content(Newtonsoft.Json.JsonConvert.SerializeObject(user));
        }
    }
}
