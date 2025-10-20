using System;

namespace ConsultaPlus.Core.Models
{
    public class Consulta
    {
        public int Id { get; set; }
        public DateTime DataConsulta { get; set; }
        public int Duracao { get; set; } 
        public string Estado { get; set; } 

        // Chaves Estrangeiras
        public int PacienteId { get; set; }
        public int MedicoId { get; set; }
        public int EspecialidadeId { get; set; }
        public int? SalaId { get; set; } // O '?' a sala pode ser opcional

        // Propriedades de Navegação 
        public Paciente Paciente { get; set; }
        public Medico Medico { get; set; }
        public Especialidade Especialidade { get; set; }
        public Sala? Sala { get; set; }
    }
}