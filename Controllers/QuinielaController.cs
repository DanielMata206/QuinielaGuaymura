using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient; // Necesario para conectarse a SQL Server
using QuinielaGuaymura.Models;
using System.Data;

namespace QuinielaGuaymura.Controllers
{
    public class QuinielaController : Controller
    {
        private readonly IConfiguration _configuration;

        public QuinielaController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        // ── Ranking (ya existía) ──────────────────────────────────────
        public IActionResult Index()
        {
            List<RankingViewModel> listaRanking = new List<RankingViewModel>();
            string connectionString = _configuration.GetConnectionString("ConexionQuiniela");
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
                                NombreUsuario = reader["Usuario"].ToString().Trim(),
                                PartidosAcertados = Convert.ToInt32(reader["PartidosAcertados"]),
                                PuntajeTotal = Convert.ToInt32(reader["puntos_totales"])
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ViewBag.ErrorBD = "Ocurrió un inconveniente al cargar el ranking.";
            }
            return View("Ranking", listaRanking);
        }

        // ── Realizar Quiniela GET — muestra los partidos ──────────────
        public IActionResult RealizarQuiniela()
        {
            // Si no hay sesión, redirige al inicio
            string usuarioActivo = HttpContext.Session.GetString("UsuarioActivo");
            if (string.IsNullOrEmpty(usuarioActivo))
            {
                TempData["Mensaje"] = "⚠️ Debes iniciar sesión para realizar tu quiniela.";
                TempData["TipoMensaje"] = "warning";
                return RedirectToAction("Index", "Home");
            }

            string connectionString = _configuration.GetConnectionString("ConexionQuiniela");

            // Traemos todos los partidos con nombres de países y grupo
            string query = @"
                SELECT 
                    p.ID_Partido,
                    p.Grupo,
                    p.Fecha_Partido,
                    pa1.Pais AS PaisLocal,
                    pa2.Pais AS PaisVisitante
                FROM partidos p
                JOIN paises pa1 ON p.ID_Pais_Local    = pa1.ID_Pais
                JOIN paises pa2 ON p.ID_Pais_Visitante = pa2.ID_Pais
                ORDER BY p.Grupo, p.Fecha_Partido";

            // Diccionario: clave = grupo, valor = lista de partidos de ese grupo
            var partidosPorGrupo = new Dictionary<string, List<PartidoViewModel>>();

            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    connection.Open();
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            string grupo = reader["Grupo"].ToString().Trim();

                            if (!partidosPorGrupo.ContainsKey(grupo))
                                partidosPorGrupo[grupo] = new List<PartidoViewModel>();

                            partidosPorGrupo[grupo].Add(new PartidoViewModel
                            {
                                IdPartido = Convert.ToInt32(reader["ID_Partido"]),
                                Grupo = grupo,
                                FechaPartido = Convert.ToDateTime(reader["Fecha_Partido"]),
                                PaisLocal = reader["PaisLocal"].ToString().Trim(),
                                PaisVisitante = reader["PaisVisitante"].ToString().Trim()
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                TempData["Mensaje"] = "❌ Error al cargar los partidos.";
                TempData["TipoMensaje"] = "danger";
                return RedirectToAction("Index", "Home");
            }

            return View(partidosPorGrupo);
        }

        // ── Realizar Quiniela POST — guarda las predicciones ──────────
        [HttpPost]
        public IActionResult GuardarQuiniela(IFormCollection form)
        {
            string usuarioActivo = HttpContext.Session.GetString("UsuarioActivo");
            if (string.IsNullOrEmpty(usuarioActivo))
                return RedirectToAction("Index", "Home");

            string connectionString = _configuration.GetConnectionString("ConexionQuiniela");

            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    // Obtenemos el ID del usuario activo
                    int? idUsuario = HttpContext.Session.GetInt32("IDUsuario");
                    if (idUsuario == null)
                        return RedirectToAction("Index", "Home");

                    // Recorremos cada campo del form que empiece con "local_"
                    foreach (string key in form.Keys)
                    {
                        if (!key.StartsWith("local_")) continue;

                        string idPartidoStr = key.Replace("local_", "");
                        int idPartido = int.Parse(idPartidoStr);

                        int golesLocal = int.TryParse(form["local_" + idPartidoStr], out int gl) ? gl : 0;
                        int golesVisitante = int.TryParse(form["visitante_" + idPartidoStr], out int gv) ? gv : 0;

                        // Verificamos si ya existe una predicción para este partido y usuario
                        int existe = 0;
                        using (SqlCommand cmdCheck = new SqlCommand(
                            @"SELECT COUNT(*) FROM PREDICCIONES 
                              WHERE ID_Usuario = @u AND ID_Partido = @p", connection))
                        {
                            cmdCheck.Parameters.AddWithValue("@u", idUsuario);
                            cmdCheck.Parameters.AddWithValue("@p", idPartido);
                            existe = (int)cmdCheck.ExecuteScalar();
                        }

                        if (existe == 0)
                        {
                            // INSERT si no existe
                            using (SqlCommand cmdInsert = new SqlCommand(
                                @"INSERT INTO PREDICCIONES 
                                  (ID_Usuario, ID_Partido, Goles_Local_Prediccion, Goles_Visitante_Prediccion, Puntos_Ganados, FechaRegistro)
                                  VALUES (@u, @p, @gl, @gv, 0, @fecha)", connection))
                            {
                                cmdInsert.Parameters.AddWithValue("@u", idUsuario);
                                cmdInsert.Parameters.AddWithValue("@p", idPartido);
                                cmdInsert.Parameters.AddWithValue("@gl", golesLocal);
                                cmdInsert.Parameters.AddWithValue("@gv", golesVisitante);
                                cmdInsert.Parameters.AddWithValue("@fecha", DateTime.Now.ToString("yyyyMMdd"));
                                cmdInsert.ExecuteNonQuery();
                            }
                        }
                        else
                        {
                            // UPDATE si ya había predicción
                            using (SqlCommand cmdUpdate = new SqlCommand(
                                @"UPDATE PREDICCIONES 
                                  SET Goles_Local_Prediccion = @gl, Goles_Visitante_Prediccion = @gv
                                  WHERE ID_Usuario = @u AND ID_Partido = @p", connection))
                            {
                                cmdUpdate.Parameters.AddWithValue("@gl", golesLocal);
                                cmdUpdate.Parameters.AddWithValue("@gv", golesVisitante);
                                cmdUpdate.Parameters.AddWithValue("@u", idUsuario);
                                cmdUpdate.Parameters.AddWithValue("@p", idPartido);
                                cmdUpdate.ExecuteNonQuery();
                            }
                        }
                    }
                }

                TempData["Mensaje"] = "✅ ¡Quiniela guardada exitosamente!";
                TempData["TipoMensaje"] = "success";
            }
            catch (Exception ex)
            {
                TempData["Mensaje"] = "❌ Error al guardar la quiniela.";
                TempData["TipoMensaje"] = "danger";
            }

            return RedirectToAction("Index", "Home");
        }
    }
}