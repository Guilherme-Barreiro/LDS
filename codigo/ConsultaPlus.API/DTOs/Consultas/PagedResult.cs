namespace ConsultaPlus.API.DTOs.Consultas
{
    public record PagedListDto<T>(int Total, int Page, int PageSize, IReadOnlyList<T> Items);
}
