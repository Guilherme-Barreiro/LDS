namespace ConsultaPlus.API.DTOs.Notificacoes
{
    public class NotificacaoResponseDto
    {
        public int Id { get; set; }
        public string Categoria { get; set; } = default!;
        public string Descricao { get; set; } = default!;
        public DateTime DataCriacao { get; set; }
        public bool Lida { get; set; }
        public int? MedicoId { get; set; }
        public int? PacienteId { get; set; }
    }
}
