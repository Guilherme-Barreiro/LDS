namespace ConsultaPlus.API.DTOs
{
    public class TaxaNaoComparecimentoRequestDTO
    {
        public DateTime? DataInicio { get; set; }
        public DateTime? DataFim { get; set; }
        public int? MedicoId { get; set; }
        public int? EspecialidadeId { get; set; }
    }
}
