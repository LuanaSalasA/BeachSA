using APIRESTful.Models;

namespace APIRESTful.Services
{
    public class AuditoriaService
    {
        private readonly DbContextBeachSA _context;

        public AuditoriaService (DbContextBeachSA context)
        {
            _context = context;
        }

        //Para registrar una auditoria basandonos en el usuario ID y los detalles de la auditoria 
        public async Task Registro (int usuario_id, string detalles)
        {
            var auditoria = new Auditoria
            {
                usuario_id = usuario_id,
                detalles = detalles
            };

            _context.Auditorias.Add(auditoria);

            await _context.SaveChangesAsync();               
        }
    }
}
