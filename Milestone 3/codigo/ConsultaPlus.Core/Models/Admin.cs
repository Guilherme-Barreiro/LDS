using System;

namespace ConsultaPlus.Core.Models
{
    public class Admin
    {
        public int Id { get; set; }
        public string Username { get; set; } = default!;
        public string PasswordHash { get; set; } = default!;
        public string? Email { get; set; }
        public string NomeCompleto { get; set; } = "Administrador";
        public DateTime DataCriacao { get; set; } = DateTime.UtcNow;
    }
}
