using System.ComponentModel.DataAnnotations;

namespace ConsultaPlus.API.DTOs
{
    public class ConsultasPorPeriodoRequestDTO
    {
        public required DateTime DataInicio { get; set; }

        public required DateTime DataFim { get; set; }

        public int? MedicoId { get; set; }
    }
}
