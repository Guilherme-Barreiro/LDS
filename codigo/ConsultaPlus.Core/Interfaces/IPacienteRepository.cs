using ConsultaPlus.Core.Models;
using System.Threading.Tasks;

namespace ConsultaPlus.Core.Interfaces;

public interface IPacienteRepository    {
    Task<Paciente?> GetByNUtenteAsync(string nUtente);
    Task AddAsync(Paciente paciente);
}