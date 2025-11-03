using System;

namespace ConsultaPlus.Core.Models
{
    public class Notificacao
    {
        public int Id { get; set; }
        public string Categoria { get; set; } = default!;
        public string Descricao { get; set; } = default!;
        public DateTime DataCriacao { get; set; } = DateTime.UtcNow;

        public bool Lida { get; set; } = false;

        public int? MedicoId { get; set; }
        public int? PacienteId { get; set; }

        public Medico? Medico { get; set; }
        public Paciente? Paciente { get; set; }
    }
}
