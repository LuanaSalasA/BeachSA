using APIRESTful.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace APIRESTful.Controllers
{
    [ApiController]
    [Route("[controller]")] //Manejo de la ruta completa de TipoPago
    public class TipoPagoController : ControllerBase
    {
        //La variable de DBContextBeachSA
        private readonly DbContextBeachSA _context = null;

        public TipoPagoController(DbContextBeachSA pContextBeachSA)
        {
            _context = pContextBeachSA;
        }

        //Método encargado de mostrar el listado de tipos de pago
        [HttpGet]
        [Route("ListTipoPago")]
        [Authorize(Roles = "Admin,Empleado,Cliente")]
        public List<TipoPago> ListTipoPago()
        {
            //Se devuelve la lista de tipos de pago ya almacenados en nuestra base de datos
            return _context.TiposPago.ToList();
        }
        //Cierre del Método "ListTipoPago"
    }
}
