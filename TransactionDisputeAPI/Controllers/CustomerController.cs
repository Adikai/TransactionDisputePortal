using Microsoft.AspNetCore.Mvc;
using TransactionDisputePortal.Shared.Models.DTO;

namespace TransactionDisputePortal.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CustomerController : ControllerBase
    {

        [HttpPost("login")]
        [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public IActionResult Login([FromBody] LoginRequestDto loginRequest)
        {
            //Keeping it simple here to save time. 
            //Normally I'd check the credentials against a database and generate a JWT token if valid.
            //Store that Token into both Redis or a secure cookie for future requests.
            if (loginRequest.Email == "john.doe@example.com" && loginRequest.Password == "AQAAAAIAAYagAAAAE...")
            {
                return Ok("dummy-token");
            }
            else
            {
                return Unauthorized();
            }
        }
    }
}
