using APIRESTful.Models;
using APIRESTful.Services;
using Microsoft.AspNetCore.Authorization; // Librería para implementar rutas seguras
using Microsoft.AspNetCore.Mvc;

namespace APIPaquetes.Controllers
{
    [ApiController]
    [Route("[controller]")] //Manejo de la ruta completa de APIPaquetes
    public class PaquetesController : ControllerBase 
    {
        //La varialbre de DBContextBeachSA
        private readonly DbContextBeachSA _context = null;

        private readonly AuditoriaService _auditoria = null;
        public PaquetesController(DbContextBeachSA pContextBeachSA, AuditoriaService pAuditoria)
        {
            _context = pContextBeachSA;
            _auditoria = pAuditoria;
            
        }
        private int ObtenerUsuario()
        {
            var claim = User.Claims.FirstOrDefault(a => a.Type == "id");
            if (claim == null)
            {
                return -1; // Presenta el que el dato solicitano no existe
            }
            else
            {
                return int.Parse(claim.Value);
            }
        }

        //Método encargado de mostrar el listado, es este caso al ser paquetes muestra solo: Todo Incluido, Alimentación y Hospedaje
        [HttpGet] //Se extrae
        [Route("ListPaquete")] //Nombre de la ruta para ver la lista de paquetes
        [Authorize(Roles = "Admin,Empleado,Cliente")]
        public List<Paquete> ListPaquete()
        {
            //Se devuelve esa lista de paquetes ya almacenados en nuestra base de datos
            return _context.Paquetes.ToList();
        }
        //Cierre del Método "ListaPaquetes"

        //El método para poder llegar a generar un nuevo tipo de paquete
        [HttpPost]
        [Route("CreatePaquete")]
        [Authorize(Roles = "Admin")]
        public async Task<string> CreatePaquete(Paquete temp)
        {
            string msj = "Error";

            try
            {
                if (temp != null)
                {
                    //Espacio para poder almacenar los paquetes en la tabla correspondiente en la Base de Datos
                    _context.Paquetes.Add(temp);

                    //Se aplica el cambio en la Base de Datos como un nuevo paquete
                    await _context.SaveChangesAsync();

                    //Se crea el mensaje de respuesta efectiva 
                    await _auditoria.Registro(
                        ObtenerUsuario(),
                        msj = $"Creación del paquete con el identificador: {temp.paquete_id} y nombre:{temp.nombre} se almaceno de manera efectiva"
                    );
                }
                else
                {
                    msj = "¡Error en el sistema! No se pueden agregar datos que se encuentren vacios";
                }

            }
            catch (Exception ex)
            {
                msj = $"¡Error en el sistema!{ex.InnerException.ToString}";
            }

            return msj;
        }
        //Cierre del Método "CreatePaquete"

        //El método para poder llegar a editar los paquetes que ya se encuentran ingresados en la Base de Datos
        [HttpPut]
        [Route("EditPaquete")]
        [Authorize(Roles = "Admin")] 
        public async Task<string> EditPaquete(Paquete temp)
        {
            string msj = "Error";

            if (temp != null)
            {
                Paquete pPaquete = _context.Paquetes.FirstOrDefault(p => p.paquete_id == temp.paquete_id);
                if (pPaquete != null)
                {
                    //Se presenta una actualización en los datos, el PaqueteID NO se puede modificar
                    pPaquete.nombre = temp.nombre;
                    pPaquete.costo_por_persona_noche = temp.costo_por_persona_noche;
                    pPaquete.prima_porcentaje = temp.prima_porcentaje;
                    pPaquete.mensualidades = temp.mensualidades;

                    //Damos la actualización dentro de la Base de Datos 
                    _context.Paquetes.Update(pPaquete);

                    //Se aplican los cambios nuevos en la Base de Datos
                    await _context.SaveChangesAsync();

                    await _auditoria.Registro(
                        ObtenerUsuario(),
                        msj = $"Se actualisa del paquete con el identificador: {temp.paquete_id} con el nombre de {temp.nombre}"
                    );
                }
                else
                {
                    msj = $"El paquete {temp.nombre} no se encuentra dentro de la base de datos. Volver a intentar";
                }
            }
            else
            {
                msj = "¡Error en el sistema! No se pueden modificar datos con espacios vacios";
            }
            return msj;
        }
        //Cierre del Método "EditPaquete"

        //El método para eliminar del sistema los paquetes que ya se encuentran en la Base de Datos.    //Por medio del PaqueteID
        [HttpDelete]
        [Route("DeletePaquete")]
        [Authorize(Roles = "Admin")]
        public async Task<string> DeletePaquete(int pPaqueteID)
        {
            string msj = "Error";

            try
            {
                Paquete temp = _context.Paquetes.FirstOrDefault(pi => pi.paquete_id == pPaqueteID);
                if (temp != null)
                {
                    //Se elimina el paquete con sus datos
                    _context.Paquetes.Remove(temp);

                    //Se aplican los cambios nuevos en la Base de Datos
                    await _context.SaveChangesAsync();

                    await _auditoria.Registro(
                        ObtenerUsuario(),
                        $"Se elimina si identificador: {temp.paquete_id} que presentaba el nombre: {temp.nombre}"
                    );
                    msj = $"Se elimina si identificador: {temp.paquete_id} que presentaba el nombre: {temp.nombre}";

                }
                else
                {
                    msj = $"¡Error en el sistema! Con el {pPaqueteID} No se a encontrado en el sistema el paquete a eliminar";
                }
            }
            catch (Exception ex)
            {
                msj = $"¡Error en el sistema!{ex.InnerException.ToString}";
            }
            return msj;
        }
        //Cierre del Método "DeletePaquete"

        //El método para búscar del sistema los paquetes que ya se encuentran en la Base de Datos   //Por medio del PaqueteID        
        [HttpGet]
        [Route("SearchPaquete")]
        [Authorize(Roles = "Admin,Empleado,Cliente")]
        public List<Paquete> SearchPaquete(int pPaqueteID)
        {
            //Presenta la lista
            List<Paquete> temp = new List<Paquete>();

            //Se busca el paquete específico en base a el PaqueteID
            temp = _context.Paquetes.Where(s => s.paquete_id == pPaqueteID).ToList();

            //Damos entrega de la lista referente al ID encontrados en el sistema
            return temp;
        }
        //Cierre del Método "SearchPaquete"

        //El método para búscar del sistema los paquetes que ya se encuentran en la Base de Datos   //Por medio del DescriptPaquete
        [HttpGet]
        [Route("SearchDescripPaquete")]
        [Authorize(Roles = "Admin,Empleado,Cliente")]
        public async Task<List<Paquete>> SearchDescripPaquete(string nombre)
        {
            //Presenta la lista
            List<Paquete> temp = new List<Paquete>();

            //Se busca el paquete específico en base a el PaqueteID
            temp = _context.Paquetes.Where(sd => sd.nombre.StartsWith(nombre)).ToList();

            //Damos entrega de la lista referente al ID encontrados en el sistema
            return temp;
        }
        //Cierre del Método "SearchPaquete"     
    }
}