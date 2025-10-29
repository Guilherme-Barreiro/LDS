using System;
using System.Threading;
using System.Threading.Tasks;
using ConsultaPlus.API.Controllers;
using ConsultaPlus.API.DTOs;
using ConsultaPlus.API.Helpers;
using ConsultaPlus.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

public class AdminHorariosControllerUnitTests
{
    private static AdminHorariosController BuildController(
        Mock<IHorarioTrabalhoMedico>? horarioMock = null,
        Mock<IHorarioExcecaoMedico>? excecaoMock = null)
    {
        horarioMock ??= new Mock<IHorarioTrabalhoMedico>(MockBehavior.Strict);
        excecaoMock ??= new Mock<IHorarioExcecaoMedico>(MockBehavior.Strict);

        return new AdminHorariosController(horarioMock.Object, excecaoMock.Object, db: null!);
    }

    [Fact]
    public async Task DefinirHorario_Sucesso_Devolve_NoContent()
    {
        var horario = new Mock<IHorarioTrabalhoMedico>(MockBehavior.Strict);
        horario.Setup(s => s.DefinirHorarioAsync(
                1, "Seg", TimeSpan.FromHours(9), TimeSpan.FromHours(12), It.IsAny<CancellationToken>()))
            .ReturnsAsync(123);

        var sut = BuildController(horario);

        var req = new DefinirHorarioRequest { DiaSemana = "seg", HoraInicio = TimeSpan.FromHours(9), HoraFim = TimeSpan.FromHours(12) };
        var result = await sut.DefinirHorario(1, req, CancellationToken.None);

        Assert.IsType<NoContentResult>(result);
        horario.VerifyAll();
    }

    [Fact]
    public async Task DefinirHorario_MedicoNaoExiste_Devolve_404()
    {
        var horario = new Mock<IHorarioTrabalhoMedico>(MockBehavior.Strict);
        horario.Setup(s => s.DefinirHorarioAsync(1, "Seg", It.IsAny<TimeSpan>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
               .ThrowsAsync(new KeyNotFoundException("Médico não encontrado."));

        var sut = BuildController(horario);

        var req = new DefinirHorarioRequest { DiaSemana = "seg", HoraInicio = TimeSpan.FromHours(9), HoraFim = TimeSpan.FromHours(10) };
        var result = await sut.DefinirHorario(1, req, CancellationToken.None);

        var notFound = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Contains("não encontrado", notFound.Value!.ToString(), StringComparison.OrdinalIgnoreCase);
        horario.VerifyAll();
    }

    [Fact]
    public async Task DefinirHorario_Sobreposicao_Devolve_409()
    {
        var horario = new Mock<IHorarioTrabalhoMedico>(MockBehavior.Strict);
        horario.Setup(s => s.DefinirHorarioAsync(1, "Seg", It.IsAny<TimeSpan>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
               .ThrowsAsync(new InvalidOperationException("sobreposto"));

        var sut = BuildController(horario);

        var req = new DefinirHorarioRequest { DiaSemana = "seg", HoraInicio = TimeSpan.FromHours(9), HoraFim = TimeSpan.FromHours(10) };
        var result = await sut.DefinirHorario(1, req, CancellationToken.None);

        var conflict = Assert.IsType<ConflictObjectResult>(result);
        Assert.Contains("sobre", conflict.Value!.ToString(), StringComparison.OrdinalIgnoreCase);
        horario.VerifyAll();
    }

    [Fact]
    public async Task RegistarExcecao_Sucesso_Devolve_204()
    {
        var excecao = new Mock<IHorarioExcecaoMedico>(MockBehavior.Strict);
        excecao.Setup(s => s.RegistarExcecaoAsync(1, new DateOnly(2025, 10, 27),
                    TimeSpan.FromHours(10), TimeSpan.FromHours(12), true, "Formação", It.IsAny<CancellationToken>()))
               .Returns(Task.CompletedTask);

        var sut = BuildController(excecaoMock: excecao);

        var req = new RegistarExcecaoRequest
        {
            Data = new DateOnly(2025, 10, 27),
            HoraInicio = TimeSpan.FromHours(10),
            HoraFim = TimeSpan.FromHours(12),
            IsReducao = true,
            Motivo = "Formação"
        };
        var result = await sut.RegistarExcecao(1, req, CancellationToken.None);

        Assert.IsType<NoContentResult>(result);
        excecao.VerifyAll();
    }
}
