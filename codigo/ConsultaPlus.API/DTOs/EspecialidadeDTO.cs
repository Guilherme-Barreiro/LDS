using System.ComponentModel;

namespace ConsultaPlus.API.DTOs
{
    public class EspecialidadeDTO
    {
        [ReadOnly(true)] 
        public int Id { get; set; }
        public string Nome { get; set; }
    }
}