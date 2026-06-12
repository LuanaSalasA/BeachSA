using API_Security.DTOs;
using System.Threading.Tasks;

namespace API_Security.Services
{
    public interface IAutorizacionService
    {
        Task<string> Register(RegisterRequest model);
        Task<AutorizacionResponse> Login(LoginRequest model);
        Task<AutorizacionResponse> Refresh(RefreshRequest model);
    }
}