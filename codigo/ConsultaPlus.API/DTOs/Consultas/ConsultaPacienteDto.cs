namespace ConsultaPlus.API.DTOs.Consultas
{
    public record ConsultaPacienteDto(
        int Id,
        DateTime Inicio,
        DateTime Fim,
        string Estado,
        int MedicoId,
        int EspecialidadeId,
        int? SalaId
    );
}
