# BeachSA
Proyecto universitario en ASP.NET Core (.NET 8) y SQL Server para Beach SA. Divide responsabilidades en dos esquemas: API_Security (sec) para usuarios, roles y JWT; y APIRESTful (app) para reservas y facturación. Integra la API de GOMETA para conversión de divisas y aplicación de descuentos dinámicos.

# Beach SA - Sistema de Gestión Turística e Inmobiliaria

>  **Proyecto Académico:** Desarrollado para el Laboratorio de asp.net. Todos los derechos reservados © 2026.

**Beach SA** es una solución de software empresarial basada en una API RESTful robusta, desarrollada con **C# y ASP.NET Core**. El sistema implementa una arquitectura por capas acoplada a **Microsoft SQL Server** para la automatización, persistencia y control operativo de complejos turísticos, junto con la integración de servicios de metadatos externos.

---

## Stack Tecnológico

* **Ecosistema Principal:** .NET Core (ASP.NET Core Web API)
* **Persistencia de Datos:** Microsoft SQL Server & Entity Framework Core
* **Integraciones Externas:** Consumo de API REST / Cliente HTTP (**GoMeta API**)
* **Seguridad y Acceso:** JSON Web Tokens (JWT) & Hashing de contraseñas
* **Documentación Interactiva:** Swagger / OpenAPI Especificación v3

---

## Estructura del Proyecto

El repositorio sigue los principios de separación de responsabilidades para garantizar la escalabilidad del sistema:

* **Controllers/:** Capa de presentación. Expone los endpoints de la API, gestiona el enrutamiento HTTP y recibe las peticiones.
* **Models/:** Capa de datos y contratos. Define las entidades de la base de datos y los Data Transfer Objects (DTOs).
* **Services/:** Capa de negocio. Contiene el núcleo transaccional, las validaciones, las reglas del sistema y el cliente de consumo para servicios de terceros.
* **Data/:** Capa de infraestructura. Administra el contexto de conexión a la base de datos (DbContext) y mapeos relacionales.

---

## Guía de Pruebas e Interacción con la API (Swagger)

Para interactuar con los servicios expuestos en vivo y verificar el comportamiento del sistema, siga el flujo de autenticación detallado a continuación:

### 1. Acceso al Entorno de Pruebas


### 2. Flujo de Autenticación y Registro (JWT) [https://www.api-security-beachsa.somee.com/swagger/index.html]
Debido a que los módulos de operaciones están protegidos, es obligatorio generar un token de acceso:
1. Localice el módulo de **Seguridad / Autenticación**.
2. Despliegue el endpoint `POST /api/auth/register`, introduzca los datos requeridos en el JSON y ejecute la petición.
3. Diríjase al endpoint `POST /api/auth/login`, introduzca las credenciales registradas y ejecute.
4. El servidor retornará una respuesta exitosa con una cadena de texto codificada correspondiente al **Token**. Cópielo sin incluir las comillas.

### 3. Autorización en la Interfaz [https://www.beachsa.somee.com/swagger/index.html]
1. Diríjase a la parte superior de la página de Swagger y haga clic en el botón **Authorize** (identificado con un candado).
2. En el campo de texto de valor, introduzca exactamente la palabra **Bearer** seguida de un espacio y pegue el token copiado. Ejemplo: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
3. Haga clic en **Authorize** y luego cierre la ventana modal (Close). Los endpoints protegidos quedarán desbloqueados para su consumo.

---

## Módulos Destacados y Lógica de Negocio

### Integración con la API Externa GoMeta
El sistema no trabaja de forma aislada; incorpora un cliente HTTP optimizado para comunicarse de manera asíncrona con el servicio externo de **gometa**.
* **Flujo Operacional:** A través de servicios dedicados, la aplicación consume los endpoints de GoMeta para enriquecer el ecosistema de Beach SA con metadatos dinámicos y validaciones externas. Esta lógica maneja de forma segura las credenciales de la API de terceros y deserializa las respuestas JSON complejas directamente en objetos fuertemente tipados de C#, demostrando habilidades avanzadas en consumo y parsing de APIs REST.

### Sistema de Auditoría (Trazabilidad Total)
El sistema cuenta con un motor de auditoría automatizado encargado de registrar de forma persistente e inmutable los ciclos de vida de la información. 
* **Flujo Operacional:** Al consumir los endpoints del sistema (como la creación o modificación de un recurso), el middleware intercepta la transacción. El sistema almacena en la tabla de auditoría el **momento exacto** de la operación, el tipo de evento y la identidad del usuario responsable, asegurando una bitácora forense de datos transparente. Puede comprobar esto consumiendo el módulo de auditoría inmediatamente después de registrar su usuario.

###  Capa de Seguridad Perimetral
La lógica de seguridad valida los privilegios del portador del token JWT en cada petición HTTP mediante políticas de autorización. El almacenamiento de credenciales críticas en la base de datos cuenta con algoritmos de hashing unidireccionales para evitar la fuga de información sensible.
