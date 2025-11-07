using ConsultaPlus.API.DTOs.Sns;
using ConsultaPlus.Core.Models;
using ConsultaPlus.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ConsultaPlus.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SnsPacientesController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    public SnsPacientesController(ApplicationDbContext db) => _db = db;

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateSnsPacienteDto dto, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(dto.NUtente) ||
            string.IsNullOrWhiteSpace(dto.NomeCompleto) ||
            string.IsNullOrWhiteSpace(dto.Nif) ||
            string.IsNullOrWhiteSpace(dto.Telemovel) ||
            string.IsNullOrWhiteSpace(dto.Morada) ||
            string.IsNullOrWhiteSpace(dto.Email))
            return BadRequest(new { message = "Todos os campos são obrigatórios." });

        var exists = await _db.SnsPacientes.AnyAsync(x => x.NUtente == dto.NUtente.Trim(), ct);
        if (exists) return Conflict(new { message = "Já existe um registo SNS com esse NUtente." });

        var e = new SnsPaciente
        {
            NUtente = dto.NUtente.Trim(),
            NomeCompleto = dto.NomeCompleto.Trim(),
            Nif = dto.Nif.Trim(),
            Telemovel = dto.Telemovel.Trim(),
            Morada = dto.Morada.Trim(),
            Email = dto.Email.Trim(),
            DataNascimento = dto.DataNascimento
        };

        _db.SnsPacientes.Add(e);
        await _db.SaveChangesAsync(ct);

        return CreatedAtAction(nameof(GetById), new { id = e.Id }, ToResponse(e));
    }


    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct)
        => Ok(await _db.SnsPacientes
            .AsNoTracking()
            .Select(e => ToResponse(e))
            .ToListAsync(ct));

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var e = await _db.SnsPacientes.FindAsync(new object[] { id }, ct);
        return e is null ? NotFound() : Ok(ToResponse(e));
    }

    [HttpGet("{nUtente}")]
    public async Task<IActionResult> GetByNUtente(string nUtente, CancellationToken ct)
    {
        var e = await _db.SnsPacientes.AsNoTracking()
            .FirstOrDefaultAsync(x => x.NUtente == nUtente, ct);

        return e is null ? NotFound() : Ok(ToResponse(e));
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateSnsPacienteDto dto, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(dto.NomeCompleto) ||
            string.IsNullOrWhiteSpace(dto.Nif) ||
            string.IsNullOrWhiteSpace(dto.Telemovel) ||
            string.IsNullOrWhiteSpace(dto.Morada) ||
            string.IsNullOrWhiteSpace(dto.Email))
            return BadRequest(new { message = "Todos os campos são obrigatórios." });

        var e = await _db.SnsPacientes.FindAsync(new object[] { id }, ct);
        if (e is null) return NotFound();

        e.NomeCompleto = dto.NomeCompleto.Trim();
        e.Nif = dto.Nif.Trim();
        e.Telemovel = dto.Telemovel.Trim();
        e.Morada = dto.Morada.Trim();
        e.Email = dto.Email.Trim();
        e.DataNascimento = dto.DataNascimento;

        await _db.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var e = await _db.SnsPacientes.FindAsync(new object[] { id }, ct);
        if (e is null) return NotFound();
        _db.SnsPacientes.Remove(e);
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }

    private static SnsPacienteResponseDto ToResponse(SnsPaciente e) => new()
    {
        Id = e.Id,
        NUtente = e.NUtente,
        NomeCompleto = e.NomeCompleto,
        Nif = e.Nif,
        Telemovel = e.Telemovel,
        Morada = e.Morada,
        Email = e.Email,
        DataNascimento = e.DataNascimento,
        DataCriacao = e.DataCriacao
    };

    [HttpPost("importar/{nUtente}")]
    [Authorize(Roles = "Paciente")]
    public async Task<IActionResult> ImportarParaPaciente(string nUtente, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(nUtente))
            return BadRequest(new { message = "NUtente é obrigatório." });

        var n = nUtente.Trim();

        var sns = await _db.SnsPacientes.AsNoTracking()
            .FirstOrDefaultAsync(x => x.NUtente == n, ct);

        if (sns is null)
            return NotFound(new { message = $"Registo SNS com NUtente '{n}' não encontrado." });

        var paciente = await _db.Pacientes
            .FirstOrDefaultAsync(p => p.NUtente == n, ct);

        if (paciente is null)
            return NotFound(new { message = $"Paciente com NUtente '{n}' não encontrado." });

        paciente.NomeCompleto = sns.NomeCompleto;
        paciente.Nif = sns.Nif;
        paciente.Telemovel = sns.Telemovel;
        paciente.Morada = sns.Morada;
        paciente.Email = sns.Email;
        paciente.DataNascimento = sns.DataNascimento;

        await _db.SaveChangesAsync(ct);

        return Ok(new
        {
            message = "Dados importados com sucesso.",
            paciente = new
            {
                paciente.Id,
                paciente.NUtente,
                paciente.NomeCompleto,
                paciente.Nif,
                paciente.Telemovel,
                paciente.Morada,
                paciente.Email,
                paciente.DataNascimento,
                paciente.DataCriacao
            }
        });
    }
}
