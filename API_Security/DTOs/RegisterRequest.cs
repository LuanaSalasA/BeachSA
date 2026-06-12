namespace API_Security.DTOs
{
    public class RegisterRequest
    {
        public string username { get; set; }
        public string email { get; set; }
        public string password { get; set; }
        public int rol_id { get; set; }
    }
}