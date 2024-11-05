using HospitalManagementAPI.Models;
using Microsoft.AspNetCore.Mvc;

namespace HospitalManagementAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UserController : ControllerBase
    {
        private readonly DatabaseService _databaseService;

        public UserController(DatabaseService databaseService)
        {
            _databaseService = databaseService;
        }

        [HttpPost("Authenticate")]
        public async Task<IActionResult> Authenticate([FromBody] string pin)
        {
            var user = await _databaseService.AuthenticateUserAsync(pin);
            if (user == null)
            {
                return Unauthorized("Niepoprawny PIN lub użytkownik nieaktywny.");
            }

            return Ok(user);
        }

    }
}
