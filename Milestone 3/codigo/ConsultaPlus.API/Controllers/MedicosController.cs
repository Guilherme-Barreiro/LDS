using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using ConsultaPlus.Core.Interfaces;
using ConsultaPlus.Core.Models;
using ConsultaPlus.API.DTOs.Medicos;
using Microsoft.AspNetCore.Authorization;

namespace ConsultaPlus.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MedicosController : ControllerBase
    {
        private readonly IMedicoService _svc;
        private readonly IDisponibilidadeService _agenda;

        public MedicosController(IMedicoService svc, IDisponibilidadeService agenda)
        {
            _svc = svc;
            _agenda = agenda;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var medicos = await _svc.GetAllAsync();
            var res = medicos.Select(ToResponse);
            return Ok(res);
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var medico = await _svc.GetByIdAsync(id);
            if (medico is null) return NotFound();
            return Ok(ToResponse(medico));
        }

        [HttpGet("search")]
        public async Task<IActionResult> Search([FromQuery] string nome)
        {
            if (string.IsNullOrWhiteSpace(nome))
                return BadRequest(new { message = "Parâmetro 'nome' é obrigatório." });

            var medicos = await _svc.SearchByNomeAsync(nome);
            var res = medicos.Select(ToResponse);
            return Ok(res);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create([FromBody] CreateMedicoDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.NomeCompleto))
                return BadRequest("NomeCompleto é obrigatório.");
            if (string.IsNullOrWhiteSpace(dto.NUtente))
                return BadRequest("NUtente é obrigatório.");
            if (string.IsNullOrWhiteSpace(dto.Email))
                return BadRequest("Email é obrigatório.");
            if (string.IsNullOrWhiteSpace(dto.Telemovel))
                return BadRequest("Telemovel é obrigatório.");

            var medico = new Medico
            {
                NomeCompleto = dto.NomeCompleto.Trim(),
                Telemovel = dto.Telemovel.Trim(),
                Email = dto.Email.Trim(),
                NUtente = dto.NUtente.Trim(),
                PasswordHash = dto.Password,
                DataNascimento = dto.DataNascimento
            };

            var createdMedico = await _svc.CreateAsync(medico);

            var res = ToResponse(createdMedico);
            return CreatedAtAction(nameof(GetById), new { id = createdMedico.Id }, res);
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateMedicoDto dto)
        {
            var medico = await _svc.GetByIdAsync(id);
            if (medico is null) return NotFound();

            medico.NomeCompleto = dto.NomeCompleto?.Trim() ?? medico.NomeCompleto;
            medico.Telemovel = dto.Telemovel?.Trim() ?? medico.Telemovel;
            medico.Email = dto.Email?.Trim() ?? medico.Email;
            medico.DataNascimento = dto.DataNascimento;

            await _svc.UpdateAsync(medico);
            return NoContent();
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            await _svc.DeleteAsync(id);
            return NoContent();
        }

        [HttpGet("{medicoId:int}/disponibilidade")]
        public async Task<IActionResult> GetDisponibilidade(
            int medicoId,
            [FromQuery] DateTime? from = null,
            [FromQuery] DateTime? to = null,
            CancellationToken ct = default)
        {
            var start = from ?? DateTime.UtcNow;
            var end = to ?? start.AddDays(14);
            if (end <= start)
                return BadRequest(new { message = "'to' deve ser maior que 'from'." });

            var slots = await _agenda.GetSlotsLivresAsync(medicoId, start, end, ct);

            var res = slots.Select(s => new DisponibilidadeSlotDto
            {
                Start = s,
                End = s.AddMinutes(30)
            });

            return Ok(res);
        }

        [HttpGet("{medicoId:int}/proximos-slots")]
        public async Task<IActionResult> GetProximosSlots(
            int medicoId,
            [FromQuery] int count = 10,
            CancellationToken ct = default)
        {
            if (count <= 0) count = 10;

            var slots = await _agenda.GetProximosSlotsAsync(medicoId, count, ct);

            var res = slots.Select(s => new DisponibilidadeSlotDto
            {
                Start = s,
                End = s.AddMinutes(30)
            });

            return Ok(res);
        }

        private static MedicoResponseDto ToResponse(Medico m) => new()
        {
            Id = m.Id,
            NomeCompleto = m.NomeCompleto,
            Telemovel = m.Telemovel,
            Email = m.Email,
            NUtente = m.NUtente,
            DataNascimento = m.DataNascimento,
            DataCriacao = m.DataCriacao
        };
    }
}
