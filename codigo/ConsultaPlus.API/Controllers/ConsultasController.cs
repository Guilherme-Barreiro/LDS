using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using ConsultaPlus.Core.Interfaces;
using ConsultaPlus.Core.Models;
using ConsultaPlus.API.DTOs.Consultas;
using System;

namespace ConsultaPlus.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ConsultasController : ControllerBase
    {
        private readonly IConsultaRepository _repo;
        private readonly IMedicoRepository _medicos;
        private readonly IPacienteRepository _pacientes;
        private readonly ISalaRepository _salas;
        private readonly IEspecialidadeRepository _especialidades;

        public ConsultasController(
            IConsultaRepository repo,
            IMedicoRepository medicos,
            IPacienteRepository pacientes,
            ISalaRepository salas,
            IEspecialidadeRepository especialidades)
        {
            _repo = repo;
            _medicos = medicos;
            _pacientes = pacientes;
            _salas = salas;
            _especialidades = especialidades;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var list = await _repo.GetAllAsync();
            var res = list.Select(ToResponse);
            return Ok(res);
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var c = await _repo.GetByIdAsync(id);
            if (c is null) return NotFound(new { message = $"Consulta {id} não encontrada." });
            return Ok(ToResponse(c));
        }

        [HttpGet("medico/{medicoId:int}")]
        public async Task<IActionResult> GetByMedico(int medicoId, [FromQuery] DateTime? de, [FromQuery] DateTime? ate)
        {
            var list = await _repo.GetAllAsync();
            var q = list.Where(c => c.MedicoId == medicoId);

            if (de.HasValue) q = q.Where(c => c.DataConsulta >= de.Value);
            if (ate.HasValue) q = q.Where(c => c.DataConsulta <= ate.Value);

            var res = q.OrderBy(c => c.DataConsulta).Select(ToResponse);
            return Ok(res);
        }

        [HttpGet("paciente/{pacienteId:int}")]
        public async Task<IActionResult> GetByPaciente(int pacienteId, [FromQuery] DateTime? de, [FromQuery] DateTime? ate)
        {
            var list = await _repo.GetAllAsync();
            var q = list.Where(c => c.PacienteId == pacienteId);

            if (de.HasValue) q = q.Where(c => c.DataConsulta >= de.Value);
            if (ate.HasValue) q = q.Where(c => c.DataConsulta <= ate.Value);

            var res = q.OrderBy(c => c.DataConsulta).Select(ToResponse);
            return Ok(res);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateConsultaDto dto)
        {
            var errors = new List<string>();

            if (await _pacientes.GetByIdAsync(dto.PacienteId) is null)
                errors.Add($"PacienteId {dto.PacienteId} não existe.");
            if (await _medicos.GetByIdAsync(dto.MedicoId) is null)
                errors.Add($"MedicoId {dto.MedicoId} não existe.");
            if (await _salas.GetByIdAsync(dto.SalaId) is null)
                errors.Add($"SalaId {dto.SalaId} não existe.");
            if (await _especialidades.GetByIdAsync(dto.EspecialidadeId) is null)
                errors.Add($"EspecialidadeId {dto.EspecialidadeId} não existe.");

            if (errors.Count > 0)
                return BadRequest(new { message = "IDs inválidos.", errors });

            var c = new Consulta
            {
                PacienteId = dto.PacienteId,
                MedicoId = dto.MedicoId,
                SalaId = dto.SalaId,
                EspecialidadeId = dto.EspecialidadeId,
                DataConsulta = dto.DataConsulta,
                Duracao = dto.Duracao,
                Estado = dto.Estado
            };

            await _repo.AddAsync(c);
            return StatusCode(201, ToResponse(c));
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var existing = await _repo.GetByIdAsync(id);
            if (existing is null) return NotFound(new { message = $"Consulta {id} não encontrada." });

            await _repo.DeleteAsync(id);
            return NoContent();
        }
        private static ConsultaResponseDto ToResponse(Consulta c) => new()
        {
            Id = c.Id,
            PacienteId = c.PacienteId,
            MedicoId = c.MedicoId,
            SalaId = c.SalaId,
            EspecialidadeId = c.EspecialidadeId,
            DataConsulta = c.DataConsulta,
            Duracao = c.Duracao,
            Estado = c.Estado
        };
    }
}
