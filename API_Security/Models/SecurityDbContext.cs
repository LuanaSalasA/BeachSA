using Microsoft.EntityFrameworkCore;

namespace API_Security.Models
{
    public class SecurityDbContext : DbContext
    {
        public SecurityDbContext(DbContextOptions<SecurityDbContext> options)
            : base(options) { }

        public DbSet<Usuario> Usuarios { get; set; }
        public DbSet<Rol> Roles { get; set; }
        public DbSet<Permiso> Permisos { get; set; }

        public DbSet<UsuarioRol> UsuariosRoles { get; set; }   // ← CAMBIADO
        public DbSet<RolPermiso> RolesPermisos { get; set; }   // ← CAMBIADO

        public DbSet<Auditoria> Auditorias { get; set; }
        public DbSet<RefreshToken> RefreshTokens { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<UsuarioRol>()
                .HasKey(ur => new { ur.usuario_id, ur.rol_id });

            modelBuilder.Entity<RolPermiso>()
                .HasKey(rp => new { rp.rol_id, rp.permiso_id });
        }
    }
}