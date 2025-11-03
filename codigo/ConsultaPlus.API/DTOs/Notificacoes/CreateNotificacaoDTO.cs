namespace ConsultaPlus.API.DTOs.Notificacoes
{
    public class CreateNotificacaoDto
    {
        public string Categoria { get; set; } = default!;
        public string Descricao { get; set; } = default!;
        public int? MedicoId { get; set; }
        public int? PacienteId { get; set; }
    }
}
