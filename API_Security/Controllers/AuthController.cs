using API_Security.DTOs;
using API_Security.Services;
using Microsoft.AspNetCore.Mvc;

namespace API_Security.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAutorizacionService _authService;

        public AuthController(IAutorizacionService authService)
        {
            _authService = authService;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest model)
        {
            var result = await _authService.Register(model);

            // Si devuelve mensaje de error
            if (result != "Usuario registrado correctamente.")
                return BadRequest(result);

            return Ok(result);
        }

        
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest model)
        {
            var authResult = await _authService.Login(model);

            if (authResult == null)
                return Unauthorized("Credenciales inválidas.");

            return Ok(authResult);
        }

       
        [HttpPost("refresh")]
        public async Task<IActionResult> Refresh([FromBody] RefreshRequest model)
        {
            var authResult = await _authService.Refresh(model);

            if (authResult == null)
                return Unauthorized("Refresh token inválido o expirado.");

            return Ok(authResult);
        }
    }
}
