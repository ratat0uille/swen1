//for registration, login, profile management, authentication handling...

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MTCG.Models;

namespace MTCG.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        [HttpPost("register")]
        public IActionResult Register([FromBody] UserDto user)
        {
            //register logic
            return Ok("User registration successful!");
        }

        [HttpPost("login")]
        public IActionResult Login([FromBody] LoginDto login)
        {
            
            //login logic
            return Ok("Login successful!");
        }
    }
}
