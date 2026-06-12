namespace API_Security.Services
{
    public class AutorizacionResponse
    {
        public string Jwt { get; set; }
        public string RefreshToken { get; set; }
        public string Rol { get; set; }
    }
}