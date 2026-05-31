using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using QuinielaGuaymura.Models;
using System.Data;

namespace QuinielaGuaymura.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IConfiguration _configuration;

        // Inyectamos tanto el Logger original como la Configuración del appsettings.json
        public HomeController(ILogger<HomeController> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
        }

        public IActionResult Index()
        {
            List<RankingViewModel> listaRanking = new List<RankingViewModel>();
            
            // 1. Obtenemos la cadena de conexión de tu servidor de Somee
            string connectionString = _configuration.GetConnectionString("ConexionQuiniela");

            // 2. Consulta SQL con tus tablas reales para calcular posiciones y aciertos
            string query = @"
                SELECT 
                   LTRIM(RTRIM(u.Nombre)) + ' ' + LTRIM(RTRIM(u.Apellido)) AS Usuario, 
                    u.puntos_totales,
                    COUNT(CASE WHEN p.Puntos_Ganados > 0 THEN 1 END) AS PartidosAcertados
                FROM USUARIOS u
                LEFT JOIN PREDICCIONES p ON u.ID_Usuario = p.ID_Usuario
                GROUP BY u.ID_Usuario, u.Usuario, u.puntos_totales
                ORDER BY u.puntos_totales DESC";

            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        connection.Open();
                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            int puesto = 1;
                            while (reader.Read())
                            {
                                listaRanking.Add(new RankingViewModel
                                {
                                    Posicion = puesto++,
                                    // El Trim() limpia los espacios fantasmas que genera el tipo CHAR(100)
                                    NombreUsuario = reader["Usuario"].ToString().Trim(),
                                    PartidosAcertados = Convert.ToInt32(reader["PartidosAcertados"]),
                                    PuntajeTotal = Convert.ToInt32(reader["puntos_totales"])
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Registramos el error en la consola interna por si necesitas revisarlo
                _logger.LogError("Error al conectar con la base de datos de Somee: " + ex.Message);
                ViewBag.ErrorBD = "Ocurrió un inconveniente al cargar el ranking.";
            }

            // 3. Pasamos la lista cargada de la BD a tu vista Index.cshtml
            return View(listaRanking);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}