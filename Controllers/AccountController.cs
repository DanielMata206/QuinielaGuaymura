using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;


namespace PresentacionQuinielaGuaymura.Controllers
{
    public class AccountController : Controller
{
        private readonly IConfiguration _configuration;

        public AccountController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        // POST: Login con Correo y Contraseña
        [HttpPost]
        public IActionResult Login(string Correo, string Contrasena)
        {
            string connectionString = _configuration.GetConnectionString("ConexionQuiniela");

            string query = @"SELECT ID_Usuario, Nombre, Apellido 
                             FROM USUARIOS 
                            WHERE Correo = @correo AND Contrasena = @contrasena";
            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@correo", Correo.Trim());
                    command.Parameters.AddWithValue("@contrasena", Contrasena);
                    connection.Open();

                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            string nombreCompleto = reader["Nombre"].ToString().Trim()
                                                  + " " + reader["Apellido"].ToString().Trim();
                            int idUsuario = Convert.ToInt32(reader["ID_Usuario"]);

                            HttpContext.Session.SetString("UsuarioActivo", nombreCompleto);
                            HttpContext.Session.SetInt32("IDUsuario", idUsuario);

                            TempData["Mensaje"] = "✅ ¡Bienvenido, " + nombreCompleto + "!";
                            TempData["TipoMensaje"] = "success";
                        }
                        else
                        {
                            TempData["Mensaje"] = "❌ Correo o contraseña incorrectos.";
                            TempData["TipoMensaje"] = "danger";
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                TempData["Mensaje"] = "❌ Error al conectar con la base de datos.";
                TempData["TipoMensaje"] = "danger";
            }

            return RedirectToAction("Index", "Home");
        }

        // POST: Registro con Nombre, Apellido, Correo y Contraseña
        [HttpPost]
        public IActionResult Register(string Nombre, string Apellido, string Correo, string Contrasena)
        {
            string connectionString = _configuration.GetConnectionString("ConexionQuiniela");

            string queryVerificar = "SELECT COUNT(*) FROM USUARIOS WHERE Correo = @correo";
            string queryInsertar = @"INSERT INTO USUARIOS (Nombre, Apellido, Correo, Contrasena, puntos_totales) 
                                      VALUES (@nombre, @apellido, @correo, @contrasena, 0)";
            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    // Verificar si el correo ya existe
                    using (SqlCommand cmdVerificar = new SqlCommand(queryVerificar, connection))
                    {
                        cmdVerificar.Parameters.AddWithValue("@correo", Correo.Trim());
                        int existe = (int)cmdVerificar.ExecuteScalar();
                        if (existe > 0)
                        {
                            TempData["Mensaje"] = "⚠️ Ese correo ya está registrado.";
                            TempData["TipoMensaje"] = "warning";
                            return RedirectToAction("Index", "Home");
                        }
                    }

                    // Insertar nuevo usuario
                    using (SqlCommand cmdInsertar = new SqlCommand(queryInsertar, connection))
                    {
                        cmdInsertar.Parameters.AddWithValue("@nombre", Nombre.Trim());
                        cmdInsertar.Parameters.AddWithValue("@apellido", Apellido.Trim());
                        cmdInsertar.Parameters.AddWithValue("@correo", Correo.Trim());
                        cmdInsertar.Parameters.AddWithValue("@contrasena", Contrasena);
                        cmdInsertar.ExecuteNonQuery();

                        TempData["Mensaje"] = "✅ ¡Cuenta creada exitosamente! Ya puedes iniciar sesión.";
                        TempData["TipoMensaje"] = "success";
                    }
                }
            }
            catch (Exception ex)
            {
                TempData["Mensaje"] = "❌ Error al registrar. Intenta de nuevo.";
                TempData["TipoMensaje"] = "danger";
            }

            return RedirectToAction("Index", "Home");
        }

        // Cerrar sesión
        public IActionResult Logout()
        {
            HttpContext.Session.Remove("UsuarioActivo");
            HttpContext.Session.Remove("IDUsuario");
            TempData["Mensaje"] = "👋 Sesión cerrada correctamente.";
            TempData["TipoMensaje"] = "success";
            return RedirectToAction("Index", "Home");
        }
    }
}
