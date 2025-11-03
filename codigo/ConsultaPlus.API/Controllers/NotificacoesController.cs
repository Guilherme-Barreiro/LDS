
using ConsultaPlus.API.DTOs.Notificacoes;
using ConsultaPlus.Core.Interfaces;
using ConsultaPlus.Core.Models;
using Microsoft.AspNetCore.Mvc;

namespace ConsultaPlus.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class NotificacoesController : ControllerBase
    {
        private readonly INotificacaoRepository _repo;

        public NotificacoesController(INotificacaoRepository repo) => _repo = repo;

        // GET /api/Notificacoes?medicoId=&pacienteId=&unreadOnly=
        [HttpGet]
        public async Task<IActionResult> Get([FromQuery] int? medicoId, [FromQuery] int? pacienteId, [FromQuery] bool unreadOnly = false)
        {
            IEnumerable<Notificacao> list;

            if (medicoId.HasValue)
                list = await _repo.GetByMedicoAsync(medicoId.Value, unreadOnly);
            else if (pacienteId.HasValue)
                list = await _repo.GetByPacienteAsync(pacienteId.Value, unreadOnly);
            else
                list = await _repo.GetAllAsync();

            var res = list.Select(ToDto);
            return Ok(res);
        }

        // GET /api/Notificacoes/{id}
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var n = await _repo.GetByIdAsync(id);
            return n is null ? NotFound() : Ok(ToDto(n));
        }

        // POST /api/Notificacoes
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateNotificacaoDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Categoria) || string.IsNullOrWhiteSpace(dto.Descricao))
                return BadRequest(new { message = "Categoria e Descricao são obrigatórias." });

            var n = new Notificacao
            {
                Categoria = dto.Categoria.Trim(),
                Descricao = dto.Descricao.Trim(),
                MedicoId = dto.MedicoId,
                PacienteId = dto.PacienteId
            };

            await _repo.AddAsync(n);
            return CreatedAtAction(nameof(GetById), new { id = n.Id }, ToDto(n));
        }

        // PUT /api/Notificacoes/{id}
        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateNotificacaoDto dto)
        {
            var n = await _repo.GetByIdAsync(id);
            if (n is null) return NotFound();

            if (!string.IsNullOrWhiteSpace(dto.Categoria)) n.Categoria = dto.Categoria.Trim();
            if (!string.IsNullOrWhiteSpace(dto.Descricao)) n.Descricao = dto.Descricao.Trim();
            if (dto.Lida.HasValue) n.Lida = dto.Lida.Value;

            await _repo.UpdateAsync(n);
            return Ok(ToDto(n));
        }

        // PATCH /api/Notificacoes/{id}/ler?Lida=true|false
        [HttpPatch("{id:int}/ler")]
        public async Task<IActionResult> MarcarComoLida(int id, [FromQuery] bool Lida = true)
        {
            var ok = await _repo.MarcarComoLidaAsync(id, Lida);
            return ok ? NoContent() : NotFound();
        }

        // DELETE /api/Notificacoes/{id}
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            await _repo.DeleteAsync(id);
            return NoContent();
        }

        private static NotificacaoResponseDto ToDto(Notificacao n) => new()
        {
            Id = n.Id,
            Categoria = n.Categoria,
            Descricao = n.Descricao,
            DataCriacao = n.DataCriacao,
            Lida = n.Lida,
            MedicoId = n.MedicoId,
            PacienteId = n.PacienteId
        };
    }
}
