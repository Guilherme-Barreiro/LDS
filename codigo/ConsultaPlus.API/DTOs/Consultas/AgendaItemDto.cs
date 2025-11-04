namespace ConsultaPlus.API.DTOs.Consultas
{
    public record AgendaItemDto(
        int ConsultaId,
        DateTime Inicio,
        DateTime Fim,
        string Estado,
        int PacienteId,
        int? SalaId
    );
}
