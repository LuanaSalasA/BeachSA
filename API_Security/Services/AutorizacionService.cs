using API_Security.Models;
using API_Security.DTOs;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace API_Security.Services
{
    public class AutorizacionService : IAutorizacionService
    {
        private readonly SecurityDbContext _context;
        private readonly IConfiguration _config;

        public AutorizacionService(SecurityDbContext context, IConfiguration config)
        {
            _context = context;
            _config = config;
        }

        // ==========================
        //        REGISTER
        // ==========================
        public async Task<string> Register(RegisterRequest model)
        {
            if (await _context.Usuarios.AnyAsync(x => x.username == model.username))
                return "El username ya está registrado.";

            if (await _context.Usuarios.AnyAsync(x => x.email == model.email))
                return "El email ya está registrado.";

            var passwordHash = HashPassword(model.password);

            var usuario = new Usuario
            {
                username = model.username,
                email = model.email,
                password_hash = passwordHash,
                estado = "A",
                creado_en = DateTime.UtcNow
            };

            _context.Usuarios.Add(usuario);
            await _context.SaveChangesAsync();

            // 📌 Asignar rol correctamente
            var rol = new UsuarioRol
            {
                usuario_id = usuario.usuario_id,
                rol_id = model.rol_id
            };

            _context.UsuariosRoles.Add(rol);

            _context.Auditorias.Add(new Auditoria
            {
                usuario_id = usuario.usuario_id,
                fecha = DateTime.UtcNow,
                detalles = $"Usuario registrado con rol {model.rol_id}"
            });

            await _context.SaveChangesAsync();

            return "Usuario registrado correctamente.";
        }

        // ==========================
        //          LOGIN
        // ==========================
        public async Task<AutorizacionResponse> Login(LoginRequest model)
        {
            var usuario = await _context.Usuarios
                .FirstOrDefaultAsync(x => x.username == model.username);

            if (usuario == null)
                return null;

            if (!VerifyPassword(model.password, usuario.password_hash))
                return null;

            var rolBD = await _context.UsuariosRoles
                .Include(ur => ur.Rol)
                .FirstOrDefaultAsync(ur => ur.usuario_id == usuario.usuario_id);

            string rol = rolBD?.Rol?.nombre ?? "Cliente";

            var jwt = GenerateJwt(usuario, rol);

            var refreshToken = new RefreshToken
            {
                token_id = Guid.NewGuid(),
                usuario_id = usuario.usuario_id,
                token = Guid.NewGuid().ToString(),
                expira_en = DateTime.UtcNow.AddDays(3),
                revocado = false,
                creado_en = DateTime.UtcNow
            };

            _context.RefreshTokens.Add(refreshToken);

            _context.Auditorias.Add(new Auditoria
            {
                usuario_id = usuario.usuario_id,
                fecha = DateTime.UtcNow,
                detalles = "Login exitoso."
            });

            await _context.SaveChangesAsync();

            return new AutorizacionResponse
            {
                Jwt = jwt,
                RefreshToken = refreshToken.token,
                Rol = rol
            };
        }

        // ==========================
        //         REFRESH
        // ==========================
        public async Task<AutorizacionResponse> Refresh(RefreshRequest model)
        {
            var tokenBD = await _context.RefreshTokens
                .FirstOrDefaultAsync(t => t.token == model.refreshToken && !t.revocado);

            if (tokenBD == null)
                return null;

            if (tokenBD.expira_en < DateTime.UtcNow)
                return null;

            var usuario = await _context.Usuarios.FindAsync(tokenBD.usuario_id);

            var rolBD = await _context.UsuariosRoles
                .Include(ur => ur.Rol)
                .FirstOrDefaultAsync(ur => ur.usuario_id == usuario.usuario_id);

            string rol = rolBD?.Rol?.nombre ?? "Cliente";

            var nuevoJwt = GenerateJwt(usuario, rol);

            _context.Auditorias.Add(new Auditoria
            {
                usuario_id = usuario.usuario_id,
                fecha = DateTime.UtcNow,
                detalles = "Refresh token usado."
            });

            await _context.SaveChangesAsync();

            return new AutorizacionResponse
            {
                Jwt = nuevoJwt,
                RefreshToken = model.refreshToken,
                Rol = rol
            };
        }

        // ==========================
        //     HELPER METHODS
        // ==========================

        private byte[] HashPassword(string password)
        {
            return SHA256.HashData(Encoding.UTF8.GetBytes(password));
        }

        private bool VerifyPassword(string input, byte[] hashBD)
        {
            var hash = SHA256.HashData(Encoding.UTF8.GetBytes(input));
            return hash.SequenceEqual(hashBD);
        }

        private string GenerateJwt(Usuario usuario, string rol)
        {
            var key = Encoding.UTF8.GetBytes(_config["JwtSettings:Key"]);

            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, usuario.usuario_id.ToString()),
                new Claim("id", usuario.usuario_id.ToString()),
                new Claim("username", usuario.username),
                new Claim(ClaimTypes.Role, rol)
            };

            var creds = new SigningCredentials(
                new SymmetricSecurityKey(key),
                SecurityAlgorithms.HmacSha256
            );

            var token = new JwtSecurityToken(
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(8),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
