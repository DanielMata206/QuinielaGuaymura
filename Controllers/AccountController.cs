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

        // POST: Recibe el formulario de Login
        [HttpPost]
        public IActionResult Login(string NombreUsuario, string Contrasena)
        {
            string connectionString = _configuration.GetConnectionString("ConexionQuiniela");

            string query = "SELECT COUNT(*) FROM USUARIOS WHERE Usuario = @usuario AND Contraseña = @contrasena";

            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@usuario", NombreUsuario.Trim());
                        command.Parameters.AddWithValue("@contrasena", Contrasena);

                        connection.Open();
                        int count = (int)command.ExecuteScalar();

                        if (count > 0)
                        {
                            // Guardamos el usuario en Session para saber quién está logueado
                            HttpContext.Session.SetString("UsuarioActivo", NombreUsuario.Trim());
                            TempData["Mensaje"] = "¡Bienvenido, " + NombreUsuario.Trim() + "!";
                            TempData["TipoMensaje"] = "success";
                        }
                        else
                        {
                            TempData["Mensaje"] = "❌ Usuario o contraseña incorrectos.";
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

        // POST: Recibe el formulario de Registro
        [HttpPost]
        public IActionResult Register(string NombreUsuario, string Contrasena)
        {
            string connectionString = _configuration.GetConnectionString("ConexionQuiniela");

            // Primero verificamos que el usuario no exista ya
            string queryVerificar = "SELECT COUNT(*) FROM USUARIOS WHERE Usuario = @usuario";
            string queryInsertar = "INSERT INTO USUARIOS (Usuario, Contraseña, puntos_totales) VALUES (@usuario, @contrasena, 0)";

            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    // 1. Verificar si ya existe
                    using (SqlCommand cmdVerificar = new SqlCommand(queryVerificar, connection))
                    {
                        cmdVerificar.Parameters.AddWithValue("@usuario", NombreUsuario.Trim());
                        int existe = (int)cmdVerificar.ExecuteScalar();

                        if (existe > 0)
                        {
                            TempData["Mensaje"] = "⚠️ Ese nombre de usuario ya está en uso.";
                            TempData["TipoMensaje"] = "warning";
                            return RedirectToAction("Index", "Home");
                        }
                    }

                    // 2. Insertar el nuevo usuario
                    using (SqlCommand cmdInsertar = new SqlCommand(queryInsertar, connection))
                    {
                        cmdInsertar.Parameters.AddWithValue("@usuario", NombreUsuario.Trim());
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
            TempData["Mensaje"] = " Sesión cerrada correctamente.";
            TempData["TipoMensaje"] = "success";
            return RedirectToAction("Index", "Home");
        }

    }
}
