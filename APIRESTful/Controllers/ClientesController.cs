using APIRESTful.Models;
using APIRESTful.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace APIRESTful.Controllers
{
    [ApiController]
    [Route("[controller]")] //Manejo de la ruta completa de Clientes
    public class ClientesController : ControllerBase
    {
        //La varialbre de DBContextBeachSA
        private readonly DbContextBeachSA _context = null;
        private readonly AuditoriaService _auditoria = null;
        public ClientesController(DbContextBeachSA pContextBeachSA, AuditoriaService pAuditoria)
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

        //Método encargado de mostrar el listado de clientes
        [HttpGet] //Se extrae
        [Route("ListCliente")] //Nombre de la ruta para ver la lista de clientes
        [Authorize(Roles = "Admin,Empleado")]
        public List<Cliente> ListCliente()
        {
            //Se devuelve la lista de clientes ya almacenados en nuestra base de datos
            return _context.Clientes.ToList();
        }
        //Cierre del Método "ListCliente"

        // ===== NUEVO: Obtener el nombre desde GOMETA según la cédula =====
        [HttpGet]
        [Route("GetNombreGometa")]
        [Authorize(Roles = "Admin,Empleado")]
        public async Task<string> GetNombreGometa(string cedula)
        {
            string msj = "Error";

            try
            {
                if (string.IsNullOrWhiteSpace(cedula))//por si detecta un nulo o un nulo falso, osea que esta en blanco pero no nulo
                {
                    return "La cédula no puede venir vacía.";
                }

                APISgometa api = new APISgometa();
                var client = api.Iniciar();

                // Llamamos a: https://apis.gometa.org/cedulas/{cedula}
                var response = await client.GetAsync($"/cedulas/{cedula}");

                if (!response.IsSuccessStatusCode)
                {
                    return $"No se encontró información en GOMETA para la cédula {cedula}.";
                }

                var json = await response.Content.ReadAsStringAsync();

                // Leemos el campo "nombre" del JSON
                using (JsonDocument doc = JsonDocument.Parse(json))
                {
                    JsonElement root = doc.RootElement;

                    if (root.TryGetProperty("nombre", out JsonElement nombreElement))
                    {
                        string nombre = nombreElement.GetString();
                        msj = nombre;
                    }
                    else
                    {
                        msj = "La respuesta de GOMETA no contiene el campo 'nombre'.";
                    }
                }
            }
            catch (Exception ex)
            {
                msj = $"¡Error en el sistema al consultar GOMETA! {ex.Message}";
            }

            return msj;
        }
        //Cierre del Método "GetNombreGometa"

        //El método para registrar un nuevo cliente usando GOMETA para el nombre
        [HttpPost]
        [Route("CreateCliente")]
        [Authorize(Roles = "Admin,Empleado")]
        public async Task<string> CreateCliente(Cliente temp)
        {
            string msj = "Error";

            try
            {
                if (temp != null)
                {
                    if (string.IsNullOrWhiteSpace(temp.cedula))
                    {
                        return "La cédula es obligatoria para registrar un cliente.";
                    }

                    //Validar que no exista ya un cliente con la misma cédula
                    Cliente existe = _context.Clientes.FirstOrDefault(c => c.cedula == temp.cedula);
                    if (existe != null)
                    {
                        msj = $"Ya existe un cliente registrado con la cédula {temp.cedula}.";
                    }
                    else
                    {
                        // ===== Llamar a GOMETA para obtener el nombre =====
                        APISgometa api = new APISgometa();
                        var client = api.Iniciar();

                        var response = await client.GetAsync($"/cedulas/{temp.cedula}");

                        if (!response.IsSuccessStatusCode)
                        {
                            return $"No se encontró información en GOMETA para la cédula {temp.cedula}. No se puede registrar el cliente.";
                        }

                        var json = await response.Content.ReadAsStringAsync();

                        string nombreGometa = "";

                        using (JsonDocument doc = JsonDocument.Parse(json))
                        {
                            JsonElement root = doc.RootElement;

                            if (root.TryGetProperty("nombre", out JsonElement nombreElement))
                            {
                                nombreGometa = nombreElement.GetString();
                            }
                        }

                        if (string.IsNullOrWhiteSpace(nombreGometa))
                        {
                            return "La respuesta de GOMETA no contiene un nombre válido. No se puede registrar el cliente.";
                        }

                        // Aquí ignoramos cualquier fullname que venga del front
                        temp.fullname = nombreGometa;

                        //Espacio para poder almacenar el cliente en la tabla correspondiente en la Base de Datos
                        _context.Clientes.Add(temp);

                        //Se aplica el cambio en la Base de Datos como un nuevo cliente
                        await _context.SaveChangesAsync();

                        //Se crea el mensaje de respuesta efectiva 
                        await _auditoria.Registro(
                            ObtenerUsuario(),
                            msj = $"Creación del cliente con el identificador: {temp.cedula} con el nombre: {temp.fullname}"
                        );
                    }
                }
                else
                {
                    msj = "¡Error en el sistema! No se pueden agregar datos que se encuentren vacíos.";
                }
            }
            catch (Exception ex)
            {
                msj = $"¡Error en el sistema! {ex.InnerException?.ToString() ?? ex.Message}";
            }

            return msj;
        }
        //Cierre del Método "CreateCliente"

        //El método para editar los clientes que ya se encuentran ingresados en la Base de Datos
        [HttpPut]
        [Route("EditCliente")]
        [Authorize(Roles = "Admin,Empleado")]
        public async Task<string> EditCliente(Cliente temp)
        {
            string msj = "Error";

            if (temp != null)
            {
                //Se busca el cliente por su cédula (la cédula no se modifica)
                Cliente pCliente = _context.Clientes.FirstOrDefault(c => c.cedula == temp.cedula);
                if (pCliente != null)
                {
                    //OJO: aquí podrías decidir si vuelves a consultar GOMETA o no.
                    //Por ahora, mantenemos el nombre que ya estaba guardado.
                    //Si quisieras actualizar el nombre según GOMETA, se repetiría el mismo proceso que en CreateCliente.

                    pCliente.tipo_cedula = temp.tipo_cedula;
                    pCliente.telefono = temp.telefono;
                    pCliente.direccion = temp.direccion;
                    pCliente.email = temp.email;

                    //Damos la actualización dentro de la Base de Datos 
                    _context.Clientes.Update(pCliente);

                    //Se aplican los cambios nuevos en la Base de Datos
                    await _context.SaveChangesAsync();

                    await _auditoria.Registro(
                        ObtenerUsuario(),
                        msj = $"Se presenta una actualización del cliente con el identificador: {temp.cedula} y nombre: {pCliente.fullname}"
                    );
                }
                else
                {
                    msj = $"El cliente con la cédula {temp.cedula} no se encuentra dentro de la base de datos. Volver a intentar.";
                }
            }
            else
            {
                msj = "¡Error en el sistema! No se pueden modificar datos con espacios vacíos.";
            }
            return msj;
        }
        //Cierre del Método "EditCliente"

        //El método para eliminar del sistema los clientes que ya se encuentran en la Base de Datos
        [HttpDelete]
        [Route("DeleteCliente")]
        [Authorize(Roles = "Admin")]
        public async Task<string> DeleteCliente(string pCedula)
        {
            string msj = "Error";

            try
            {
                Cliente temp = _context.Clientes.FirstOrDefault(c => c.cedula == pCedula);
                if (temp != null)
                {
                    //Se elimina el cliente con sus datos
                    _context.Clientes.Remove(temp);

                    //Se aplican los cambios nuevos en la Base de Datos
                    await _context.SaveChangesAsync();

                    await _auditoria.Registro(
                        ObtenerUsuario(),
                        msj = $"Se presenta la eliminacón del cliente con el identificador: {temp.cedula} con el nombre {temp.fullname}"
                    );
                }
                else
                {
                    msj = $"¡Error en el sistema! Con la cédula {pCedula} no se ha encontrado en el sistema el cliente a eliminar.";
                }
            }
            catch (Exception ex)
            {
                msj = $"¡Error en el sistema! {ex.InnerException?.ToString() ?? ex.Message}";
            }
            return msj;
        }
        //Cierre del Método "DeleteCliente"

        //El método para búscar del sistema los clientes por medio de la cédula
        [HttpGet]
        [Route("SearchCliente")]
        [Authorize(Roles = "Admin,Empleado")]
        public List<Cliente> SearchCliente(string pCedula)
        {
            //Presenta la lista
            List<Cliente> temp = new List<Cliente>();

            //Se busca el cliente específico en base a la cédula
            temp = _context.Clientes.Where(c => c.cedula == pCedula).ToList();

            //Damos entrega de la lista referente a la cédula encontrada en el sistema
            return temp;
        }
        //Cierre del Método "SearchCliente"

        //El método para búscar del sistema los clientes por medio del nombre
        [HttpGet]
        [Route("SearchDescripCliente")]
        [Authorize(Roles = "Admin,Empleado")]
        public async Task<List<Cliente>> SearchDescripCliente(string nombre)
        {
            //Presenta la lista
            List<Cliente> temp = new List<Cliente>();

            //Se busca el cliente específico en base al inicio del nombre completo
            temp = _context.Clientes
                           .Where(c => c.fullname.ToLower().Contains(nombre.Trim().ToLower()))
                           .ToList();

            //Damos entrega de la lista referente al nombre encontrados en el sistema
            return temp;
        }
        //Cierre del Método "SearchDescripCliente"
    }
}
