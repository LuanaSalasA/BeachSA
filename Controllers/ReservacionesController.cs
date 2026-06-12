using APIRESTful.Models;
using APIRESTful.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Security.Claims;
using System.Text.Json;

namespace APIRESTful.Controllers
{
    [ApiController]
    [Route("[controller]")] //Manejo de la ruta completa de Reservaciones
    public class ReservacionesController : ControllerBase
    {
        //La variable de DBContextBeachSA
        private readonly DbContextBeachSA _context = null;

        private readonly AuditoriaService _auditoria = null;
        public ReservacionesController(DbContextBeachSA pContextReservaciones, AuditoriaService pAuditoria)
        {
            _context = pContextReservaciones;
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
        //==================== MÉTODO PRIVADO: OBTENER TIPO DE CAMBIO ====================

        //Obtiene el tipo de cambio de compra desde la API de GOMETA
        private async Task<decimal> ObtenerTipoCambioCompra()
        {
            decimal tipoCambio = 0;

            try
            {
                APISgometa api = new APISgometa();
                var client = api.Iniciar();

                // Endpoint: https://apis.gometa.org/tdc/tdc.json
                var response = await client.GetAsync("/tdc/tdc.json");

                if (!response.IsSuccessStatusCode)
                {
                    return 0;
                }

                var json = await response.Content.ReadAsStringAsync();

                using (JsonDocument doc = JsonDocument.Parse(json))
                {
                    JsonElement root = doc.RootElement;

                    if (root.TryGetProperty("compra", out JsonElement compraElement))
                    {
                        string compraStr = compraElement.GetString();

                        //Se intenta convertir el valor a decimal
                        if (!decimal.TryParse(compraStr, NumberStyles.Any, CultureInfo.InvariantCulture, out tipoCambio))
                        {
                            tipoCambio = 0;
                        }
                    }
                }
            }
            catch
            {
                tipoCambio = 0;
            }

            return tipoCambio;
        }

        //==================== LISTAR RESERVACIONES ====================

        //Método encargado de mostrar el listado de las reservaciones
        [HttpGet]
        [Route("ListReservacion")] //Nombre de la ruta para ver la lista
        [Authorize(Roles = "Admin,Empleado")]
        public async Task<IActionResult> ListReservacion()
        {
            var data = await _context.Reservaciones
                .Include(r => r.cliente)
                .Include(r => r.paquete)
                .Include(r => r.tipo_pago)
                .ToListAsync();
            return Ok(data);
        }

        //Cierre del Método "ListReservacion"

        //==================== CREAR RESERVACIÓN ====================

        //Método encargado de crear una reservación
        [HttpPost]
        [Route("CreateReservacion")]
        [Authorize(Roles = "Admin,Empleado,Cliente")]
        public async Task<string> CreateReservacion(Reservacion temp)
        {
            string msj = "Error";

            try
            {
                if (temp == null)
                {
                    return "¡Error en el sistema! No se pueden agregar datos vacíos";
                }

                // Ignorar entidades de navegación que vengan en el JSON
                temp.cliente = null;
                temp.paquete = null;
                temp.tipo_pago = null;

                // Validar noches y personas
                if (temp.noches <= 0 || temp.personas <= 0)
                {
                    return "No se aceptan noches o personas en cero o en negativos";
                }

                // Validar que exista el cliente
                Cliente cliente = _context.Clientes.FirstOrDefault(c => c.cedula == temp.cliente_cedula);
                if (cliente == null)
                {
                    return $"No existe un cliente registrado con la cédula {temp.cliente_cedula}. Debe registrar el cliente primero.";
                }

                // Validar que exista el paquete
                Paquete paquete = _context.Paquetes.FirstOrDefault(p => p.paquete_id == temp.paquete_id);
                if (paquete == null)
                {
                    return $"El paquete seleccionado con el identificador {temp.paquete_id} no existe en el sistema.";
                }

                // Validar que exista el tipo de pago
                TipoPago tipoPago = _context.TiposPago.FirstOrDefault(tp => tp.tipo_pago_id == temp.metodo_pago_id);
                if (tipoPago == null)
                {
                    return $"El método de pago seleccionado con el identificador {temp.metodo_pago_id} no existe en el sistema.";
                }

                // ===================== 1) TIPO DE CAMBIO DESDE GOMETA =====================

                decimal tipoCambio = await ObtenerTipoCambioCompra();

                // Si por alguna razón viene 0 o negativo, dejamos 1 como respaldo
                if (tipoCambio <= 0)
                {
                    tipoCambio = 1;
                }

                temp.tipo_cambio_usd = tipoCambio;

                // ===================== 2) CHEQUE: VALIDAR DATOS =====================

                if (tipoPago.nombre == "Cheque")
                {
                    if (string.IsNullOrWhiteSpace(temp.numero_cheque) || string.IsNullOrWhiteSpace(temp.banco_cheque))
                    {
                        return "Para el método de pago Cheque es obligatorio indicar el número de cheque y el banco.";
                    }
                }
                else
                {
                    // Si no es cheque, limpiamos esos campos
                    temp.numero_cheque = null;
                    temp.banco_cheque = null;
                }

                // ===================== 3) DESCUENTO POR NOCHES (SOLO EFECTIVO) =====================

                temp.descuento_pct = 0;

                if (tipoPago.nombre == "Efectivo")
                {
                    if (temp.noches >= 3 && temp.noches <= 6)
                    {
                        temp.descuento_pct = 10;
                    }
                    else if (temp.noches >= 7 && temp.noches <= 9)
                    {
                        temp.descuento_pct = 15;
                    }
                    else if (temp.noches >= 10 && temp.noches <= 12)
                    {
                        temp.descuento_pct = 20;
                    }
                    else if (temp.noches >= 13)
                    {
                        temp.descuento_pct = 25;
                    }
                }

                // ===================== 4) CÁLCULOS PRINCIPALES (MISMA LÓGICA QUE SIMULAR) =====================

                decimal costo_noche = paquete.costo_por_persona_noche;
                decimal subtotal = costo_noche * temp.personas * temp.noches;

                decimal descuento_colones = Math.Round(subtotal * (temp.descuento_pct / 100m), 2);

                decimal iva_pct = 13m;
                decimal base_imponible = subtotal - descuento_colones;
                decimal iva_colones = Math.Round(base_imponible * (iva_pct / 100m), 2);
                decimal total_colones = base_imponible + iva_colones;

                decimal total_usd = Math.Round(total_colones / tipoCambio, 2);

                temp.subtotal_colones = subtotal;
                temp.descuento_colones = descuento_colones;
                temp.iva_pct = iva_pct;
                temp.iva_colones = iva_colones;
                temp.total_colones = total_colones;
                temp.total_usd = total_usd;

                // ===================== 5) PRIMA Y MENSUALIDADES SEGÚN PAQUETE =====================

                temp.prima_porcentaje = paquete.prima_porcentaje;
                temp.prima_monto = Math.Round(total_colones * (temp.prima_porcentaje / 100m), 2);
                temp.mensualidades = paquete.mensualidades;

                // ===================== 6) FECHA DE RESERVACIÓN =====================

                temp.fecha_reservacion = DateTime.Now;

                // ===================== 7) GUARDAR EN BASE =====================

                _context.Reservaciones.Add(temp);
                await _context.SaveChangesAsync();

                await _auditoria.Registro(
                    ObtenerUsuario(),
                    msj = $"Se presenta una nueva reserva con su identificador: {temp.reservacion_id} el cual consta como cliente: {temp.cliente_cedula} y la adquisición del paquete: {paquete.nombre}. Por medio del pago en: {tipoPago.nombre}."
                );
            }
            catch (Exception ex)
            {
                msj = $"¡Error en el sistema! {ex.InnerException?.ToString() ?? ex.Message}";
            }

            return msj;
        }

        //Cierre del Método "CreateReservacion"

        //==================== EDITAR RESERVACIÓN ====================

        //Método encargado de editar las reservaciones
        [HttpPut]
        [Route("UpdateReservacion")]
        [Authorize(Roles = "Admin,Empleado")]
        public async Task<string> UpdateReservacion(Reservacion temp)
        {
            string msj = "Error";

            if (temp == null)
            {
                return "¡Error en el sistema! No se pueden modificar datos con espacios vacíos";
            }

            //Buscar la reservación existente
            Reservacion pReservacion = _context.Reservaciones
                .FirstOrDefault(r => r.reservacion_id == temp.reservacion_id);

            if (pReservacion == null)
            {
                return "No se encontró la reserva con ese identificador";
            }

            try
            {
                // Ignorar entidades de navegación que puedan venir en el JSON
                temp.cliente = null;
                temp.paquete = null;
                temp.tipo_pago = null;

                //Validar noches y personas
                if (temp.noches <= 0 || temp.personas <= 0)
                {
                    return "No se aceptan noches o personas en cero o en negativos";
                }

                //Validar cliente
                Cliente cliente = _context.Clientes.FirstOrDefault(c => c.cedula == temp.cliente_cedula);
                if (cliente == null)
                {
                    return $"No existe un cliente registrado con la cédula {temp.cliente_cedula}.";
                }

                //Validar paquete
                Paquete paquete = _context.Paquetes.FirstOrDefault(p => p.paquete_id == temp.paquete_id);
                if (paquete == null)
                {
                    return $"El paquete seleccionado con el identificador {temp.paquete_id} no existe en el sistema.";
                }

                //Validar tipo de pago
                TipoPago tipoPago = _context.TiposPago.FirstOrDefault(tp => tp.tipo_pago_id == temp.metodo_pago_id);
                if (tipoPago == null)
                {
                    return $"El método de pago seleccionado con el identificador {temp.metodo_pago_id} no existe en el sistema.";
                }

                //Obtener tipo de cambio actualizado desde GOMETA
                decimal tipoCambio = await ObtenerTipoCambioCompra();
                if (tipoCambio <= 0)
                {
                    tipoCambio = 1; // respaldo
                }

                //Asignar datos básicos
                pReservacion.cliente_cedula = temp.cliente_cedula;
                pReservacion.paquete_id = temp.paquete_id;
                pReservacion.noches = temp.noches;
                pReservacion.personas = temp.personas;
                pReservacion.metodo_pago_id = temp.metodo_pago_id;
                pReservacion.tipo_cambio_usd = tipoCambio;
                pReservacion.fecha_reservacion = DateTime.Now;

                //Cheque: validar número y banco
                if (tipoPago.nombre == "Cheque")
                {
                    if (string.IsNullOrWhiteSpace(temp.numero_cheque) || string.IsNullOrWhiteSpace(temp.banco_cheque))
                    {
                        return "Para el método de pago Cheque es obligatorio indicar el número de cheque y el banco.";
                    }

                    pReservacion.numero_cheque = temp.numero_cheque;
                    pReservacion.banco_cheque = temp.banco_cheque;
                }
                else
                {
                    pReservacion.numero_cheque = null;
                    pReservacion.banco_cheque = null;
                }

                //==================== CÁLCULO DE DESCUENTO ====================

                if (tipoPago.nombre == "Efectivo")
                {
                    if (pReservacion.noches >= 3 && pReservacion.noches <= 6)
                    {
                        pReservacion.descuento_pct = 10;
                    }
                    else if (pReservacion.noches >= 7 && pReservacion.noches <= 9)
                    {
                        pReservacion.descuento_pct = 15;
                    }
                    else if (pReservacion.noches >= 10 && pReservacion.noches <= 12)
                    {
                        pReservacion.descuento_pct = 20;
                    }
                    else if (pReservacion.noches >= 13)
                    {
                        pReservacion.descuento_pct = 25;
                    }
                    else
                    {
                        pReservacion.descuento_pct = 0;
                    }
                }
                else
                {
                    pReservacion.descuento_pct = 0;
                }

                //==================== CÁLCULOS PRINCIPALES ====================

                decimal costo_noche = paquete.costo_por_persona_noche;
                decimal subtotal = costo_noche * pReservacion.personas * pReservacion.noches;

                decimal descuento_colones = Math.Round(subtotal * (pReservacion.descuento_pct / 100m), 2);

                decimal iva_pct = 13m; // IVA estándar
                decimal base_imponible = subtotal - descuento_colones;
                decimal iva_colones = Math.Round(base_imponible * (iva_pct / 100m), 2);
                decimal total_colones = base_imponible + iva_colones;

                decimal total_usd = Math.Round(total_colones / tipoCambio, 2);

                //Prima y mensualidades según paquete
                pReservacion.prima_porcentaje = paquete.prima_porcentaje;
                pReservacion.prima_monto = Math.Round(total_colones * (pReservacion.prima_porcentaje / 100m), 2);
                pReservacion.mensualidades = paquete.mensualidades;

                //Asignar cálculos
                pReservacion.subtotal_colones = subtotal;
                pReservacion.descuento_colones = descuento_colones;
                pReservacion.iva_pct = iva_pct;
                pReservacion.iva_colones = iva_colones;
                pReservacion.total_colones = total_colones;
                pReservacion.total_usd = total_usd;

                //Actualizar en Base de Datos
                _context.Reservaciones.Update(pReservacion);
                await _context.SaveChangesAsync();

                await _auditoria.Registro(
                    ObtenerUsuario(),
                    msj = $"Se presenta una actualización en la reserva identificada como: {pReservacion.reservacion_id} el cual consta como cliente: {pReservacion.cliente_cedula} y la adquisición del paquete: {paquete.nombre}. Por medio del pago en: {tipoPago.nombre}."
                );
            }
            catch (Exception ex)
            {
                msj = $"¡Error en el sistema! {ex.InnerException?.ToString() ?? ex.Message}";
            }

            return msj;
        }

        //Cierre del Método "UpdateReservacion"

        //==================== ELIMINAR RESERVACIÓN ====================

        [HttpDelete]
        [Route("DeleteReservacion")]
        [Authorize(Roles = "Admin")]
        public async Task<string> DeleteReservacion(int pReservacionId)
        {
            string msj = "Error";

            try
            {
                Reservacion temp = _context.Reservaciones.FirstOrDefault(ri => ri.reservacion_id == pReservacionId);
                if (temp != null)
                {
                    _context.Reservaciones.Remove(temp);
                    await _context.SaveChangesAsync();

                    await _auditoria.Registro(
                        ObtenerUsuario(),
                        msj = $"Se presenta la eliminación del identificador: {temp.reservacion_id} perteneciente a la reserva del cliente: {temp.cliente_cedula}."
                     );
                }
                else
                {
                    msj = $"¡Error en el sistema! Con {pReservacionId} no se ha encontrado en el sistema la reserva a eliminar";
                }
            }
            catch (Exception ex)
            {
                msj = $"¡Error en el sistema! {ex.InnerException?.ToString() ?? ex.Message}";
            }
            return msj;
        }
        //Cierre del Método "DeleteReservacion"

        //==================== BUSCAR POR ID ====================

        [HttpGet]
        [Route("SearchReservacion")]
        [Authorize(Roles = "Admin,Empleado")]
        public async Task<IActionResult> SearchReservacion(int pReservacion_id)
        {
            //Se busca la reserva por su ID incluyendo las relaciones
            var reserva = await _context.Reservaciones
               .Include(r => r.cliente)
               .Include(r => r.paquete)
               .Include(r => r.tipo_pago)
               .FirstOrDefaultAsync(s => s.reservacion_id == pReservacion_id);

            if (reserva == null)
            {
                return NotFound($"No se encontró una reservación con el identificador {pReservacion_id}");
            }

            //Se presenta la reservación encontrada
            return Ok(reserva);
        }

        //Cierre del Método "SearchReservacion"

        //==================== NUEVO: SIMULADOR DE RESERVACIÓN (SIN GUARDAR) ====================

        //Método para calcular los datos de la reservación sin guardar en la Base de Datos
        [HttpGet]
        [Route("SimularReservacion")]
        [Authorize(Roles = "Admin,Empleado,Cliente")]
        public async Task<IActionResult> SimularReservacion(int paquete_id, int metodo_pago_id, int noches, int personas)
        {
            if (noches <= 0 || personas <= 0)
            {
                return BadRequest("No se aceptan noches o personas en cero o en negativos");
            }

            //Validar paquete
            Paquete paquete = _context.Paquetes.FirstOrDefault(p => p.paquete_id == paquete_id);
            if (paquete == null)
            {
                return BadRequest($"El paquete con identificador {paquete_id} no existe en el sistema.");
            }

            //Validar tipo de pago
            TipoPago tipoPago = _context.TiposPago.FirstOrDefault(tp => tp.tipo_pago_id == metodo_pago_id);
            if (tipoPago == null)
            {
                return BadRequest($"El método de pago con identificador {metodo_pago_id} no existe en el sistema.");
            }

            //Obtener tipo de cambio
            decimal tipoCambio = await ObtenerTipoCambioCompra();
            if (tipoCambio <= 0)
            {
                return BadRequest("No se pudo obtener el tipo de cambio desde GOMETA. Intente de nuevo más tarde.");
            }

            //Calcular descuento
            decimal descuento_pct = 0;

            if (tipoPago.nombre == "Efectivo")
            {
                if (noches >= 3 && noches <= 6)
                {
                    descuento_pct = 10;
                }
                else if (noches >= 7 && noches <= 9)
                {
                    descuento_pct = 15;
                }
                else if (noches >= 10 && noches <= 12)
                {
                    descuento_pct = 20;
                }
                else if (noches >= 13)
                {
                    descuento_pct = 25;
                }
            }

            decimal costo_noche = paquete.costo_por_persona_noche;
            decimal subtotal = costo_noche * personas * noches;
            decimal descuento_colones = Math.Round(subtotal * (descuento_pct / 100m), 2);

            decimal iva_pct = 13m;
            decimal base_imponible = subtotal - descuento_colones;
            decimal iva_colones = Math.Round(base_imponible * (iva_pct / 100m), 2);
            decimal total_colones = base_imponible + iva_colones;
            decimal total_usd = Math.Round(total_colones / tipoCambio, 2);

            decimal prima_porcentaje = paquete.prima_porcentaje;
            decimal prima_monto = Math.Round(total_colones * (prima_porcentaje / 100m), 2);
            int mensualidades = paquete.mensualidades;

            var resultado = new
            {
                Paquete = paquete.nombre,
                MetodoPago = tipoPago.nombre,
                Noches = noches,
                Personas = personas,
                TipoCambio = tipoCambio,
                SubtotalColones = subtotal,
                DescuentoPorcentaje = descuento_pct,
                DescuentoColones = descuento_colones,
                IvaPorcentaje = iva_pct,
                IvaColones = iva_colones,
                TotalColones = total_colones,
                TotalUSD = total_usd,
                PrimaPorcentaje = prima_porcentaje,
                PrimaMonto = prima_monto,
                Mensualidades = mensualidades
            };

            return Ok(resultado);
        }
        //Cierre del Método "SimularReservacion"
    }
}