using APIRESTful.Models;
using APIRESTful.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace APIRESTful.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class AuditoriasController : ControllerBase
    {
        //La varialbre de DBContextBeachSA
        private readonly DbContextBeachSA _context = null;
        public AuditoriasController(DbContextBeachSA pContextBeachSA)
        {
            _context = pContextBeachSA;
        }

        [HttpGet] //Se extrae
        [Route("ListaAuditoria")] //Nombre de la ruta para ver la lista de clientes
        [Authorize(Roles = "Admin")]
        public List<Auditoria> ListAuditoria()
        {
            //Se devuelve la lista de clientes ya almacenados en nuestra base de datos
            return _context.Auditorias.ToList();
        }


    }
}
