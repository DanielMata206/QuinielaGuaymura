namespace PresentacionQuinielaGuaymura.Models
{
    public class PrediccionViewModel
{
        public int IdPartido { get; set; }
        public string Grupo { get; set; }
        public DateTime FechaPartido { get; set; }
        public string PaisLocal { get; set; }
        public string PaisVisitante { get; set; }
        public int? GolesLocalPrediccion { get; set; }
        public int? GolesVisitantePrediccion { get; set; }
        public int? PuntosGanados { get; set; }
    }
}
