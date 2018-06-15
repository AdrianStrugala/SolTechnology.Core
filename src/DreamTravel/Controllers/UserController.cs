using System;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using DapperExtensions;
using DreamTravel.Models;

namespace DreamTravel.Controllers
{
    public class UserController : Controller
    {
        private readonly DbConnectionFactory _dbConnectionFactory;

        public UserController(DbConnectionFactory dbConnectionFactory)
        {
            _dbConnectionFactory = dbConnectionFactory;
        }

        [HttpPost]
        public IActionResult Add(string name, string password)
        {
            try
            {
                User toAdd = new User
                {
                    Name = name,
                    Password = password,
                    Happiness = 0
                };

                using (var connection = _dbConnectionFactory.CreateDbConnection())
                {
                    connection.Open();
                    connection.Insert(toAdd);
                }
            }
            catch (Exception e)
            {
                return BadRequest();
            }

            return Ok();
        }
    }
}
