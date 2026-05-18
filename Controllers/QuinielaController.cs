using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient; // Necesario para conectarse a SQL Server
using QuinielaGuaymura.Models;
using System.Data;

namespace QuinielaGuaymura.Controllers
{
    public class QuinielaController : Controller
    {
        private readonly IConfiguration _configuration;

        // Inyectamos la configuración para leer el appsettings.json
        public QuinielaController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public IActionResult Ranking()
        {
            List<RankingViewModel> listaRanking = new List<RankingViewModel>();

            // 1. Obtenemos la cadena de conexión de Somee
            string connectionString = _configuration.GetConnectionString("ConexionQuiniela");

            // 2. Consulta SQL Corregida (Usando tus tablas reales USUARIOS y PREDICCIONES)
            string query = @"
                SELECT 
                    u.Usuario, 
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
                                    // Trim() limpia los espacios fantasmas del CHAR(100)
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
                // Manejo de error sutil para la vista si la base de datos de Somee falla
                ViewBag.ErrorBD = "Ocurrió un inconveniente al cargar el ranking.";
            }

            // 3. Le pasamos la lista real de la base de datos a la Vista
            return View(listaRanking);
        }
    }
}