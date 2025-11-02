using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

using ConsultaPlus.API.Controllers;
using ConsultaPlus.API.DTOs.Notificacoes;
using ConsultaPlus.Core.Interfaces;
using ConsultaPlus.Core.Models;

using static ConsultaPlus.Tests.TextAssert; // se tiveres o helper de ignorar acentos

namespace ConsultaPlus.Tests.Notificacoes
{
    public class NotificacoesControllerTests
    {
        private readonly Mock<INotificacaoRepository> _repo;
        private readonly NotificacoesController _controller;

        public NotificacoesControllerTests()
        {
            _repo = new Mock<INotificacaoRepository>(MockBehavior.Strict);
            _controller = new NotificacoesController(_repo.Object);
        }

        // ---------------------------
        // GET /api/Notificacoes
        // ---------------------------
        [Fact]
        public async Task Get_SemFiltros_DeveChamarGetAll_EMapear()
        {
            var dados = new List<Notificacao>
            {
                new Notificacao { Id = 1, Categoria = "C1", Descricao = "D1", Lida = false, MedicoId = 10, PacienteId = 100, DataCriacao = new DateTime(2024,1,1) },
                new Notificacao { Id = 2, Categoria = "C2", Descricao = "D2", Lida = true,  MedicoId = 20, PacienteId = 200, DataCriacao = new DateTime(2024,2,2) },
            };
            _repo.Setup(r => r.GetAllAsync()).ReturnsAsync(dados);

            var result = await _controller.Get(null, null, unreadOnly: false);

            var ok = Assert.IsType<OkObjectResult>(result);
            var list = Assert.IsAssignableFrom<IEnumerable<NotificacaoResponseDto>>(ok.Value);
            var arr = list.ToArray();
            Assert.Equal(2, arr.Length);
            Assert.Collection(arr,
                n => { Assert.Equal(1, n.Id); Assert.Equal("C1", n.Categoria); Assert.Equal("D1", n.Descricao); Assert.False(n.Lida); Assert.Equal(10, n.MedicoId); Assert.Equal(100, n.PacienteId); Assert.Equal(new DateTime(2024, 1, 1), n.DataCriacao); },
                n => { Assert.Equal(2, n.Id); Assert.Equal("C2", n.Categoria); Assert.Equal("D2", n.Descricao); Assert.True(n.Lida); Assert.Equal(20, n.MedicoId); Assert.Equal(200, n.PacienteId); Assert.Equal(new DateTime(2024, 2, 2), n.DataCriacao); }
            );
            _repo.Verify(r => r.GetAllAsync(), Times.Once);
            _repo.Verify(r => r.GetByMedicoAsync(It.IsAny<int>(), It.IsAny<bool>()), Times.Never);
            _repo.Verify(r => r.GetByPacienteAsync(It.IsAny<int>(), It.IsAny<bool>()), Times.Never);
        }

        [Fact]
        public async Task Get_ComMedico_UnreadFalse_DeveChamarGetByMedico()
        {
            _repo.Setup(r => r.GetByMedicoAsync(7, false))
                 .ReturnsAsync(new[] { new Notificacao { Id = 3, Categoria = "C", Descricao = "D", MedicoId = 7 } });

            var result = await _controller.Get(medicoId: 7, pacienteId: null, unreadOnly: false);

            var ok = Assert.IsType<OkObjectResult>(result);
            var list = Assert.IsAssignableFrom<IEnumerable<NotificacaoResponseDto>>(ok.Value);
            Assert.Single(list);
            _repo.Verify(r => r.GetByMedicoAsync(7, false), Times.Once);
            _repo.Verify(r => r.GetAllAsync(), Times.Never);
            _repo.Verify(r => r.GetByPacienteAsync(It.IsAny<int>(), It.IsAny<bool>()), Times.Never);
        }

        [Fact]
        public async Task Get_ComMedico_UnreadTrue_DeveChamarGetByMedico_ComTrue()
        {
            _repo.Setup(r => r.GetByMedicoAsync(9, true))
                 .ReturnsAsync(Enumerable.Empty<Notificacao>());

            var result = await _controller.Get(medicoId: 9, pacienteId: null, unreadOnly: true);

            Assert.IsType<OkObjectResult>(result);
            _repo.Verify(r => r.GetByMedicoAsync(9, true), Times.Once);
        }

        [Fact]
        public async Task Get_ComPaciente_UnreadFalse_DeveChamarGetByPaciente()
        {
            _repo.Setup(r => r.GetByPacienteAsync(55, false))
                 .ReturnsAsync(new[] { new Notificacao { Id = 1, PacienteId = 55, Categoria = "X", Descricao = "Y" } });

            var result = await _controller.Get(medicoId: null, pacienteId: 55, unreadOnly: false);

            var ok = Assert.IsType<OkObjectResult>(result);
            var list = Assert.IsAssignableFrom<IEnumerable<NotificacaoResponseDto>>(ok.Value);
            Assert.Single(list);
            _repo.Verify(r => r.GetByPacienteAsync(55, false), Times.Once);
            _repo.Verify(r => r.GetAllAsync(), Times.Never);
            _repo.Verify(r => r.GetByMedicoAsync(It.IsAny<int>(), It.IsAny<bool>()), Times.Never);
        }

        // ---------------------------
        // GET /api/Notificacoes/{id}
        // ---------------------------
        [Fact]
        public async Task GetById_Existente_DeveRetornarOk_ComDto()
        {
            var n = new Notificacao { Id = 10, Categoria = "C", Descricao = "D", Lida = true, MedicoId = 1, PacienteId = 2, DataCriacao = new DateTime(2024, 3, 3) };
            _repo.Setup(r => r.GetByIdAsync(10)).ReturnsAsync(n);

            var result = await _controller.GetById(10);

            var ok = Assert.IsType<OkObjectResult>(result);
            var dto = Assert.IsType<NotificacaoResponseDto>(ok.Value);
            Assert.Equal(10, dto.Id);
            Assert.Equal("C", dto.Categoria);
            Assert.Equal("D", dto.Descricao);
            Assert.True(dto.Lida);
            Assert.Equal(1, dto.MedicoId);
            Assert.Equal(2, dto.PacienteId);
            Assert.Equal(new DateTime(2024, 3, 3), dto.DataCriacao);
            _repo.Verify(r => r.GetByIdAsync(10), Times.Once);
        }

        [Fact]
        public async Task GetById_Inexistente_DeveRetornarNotFound()
        {
            _repo.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Notificacao?)null);

            var result = await _controller.GetById(999);

            Assert.IsType<NotFoundResult>(result);
            _repo.Verify(r => r.GetByIdAsync(999), Times.Once);
        }

        // ---------------------------
        // POST /api/Notificacoes
        // ---------------------------
        [Theory]
        [InlineData(null, "desc", "Categoria e Descricao sao obrigatorias")]
        [InlineData("   ", "desc", "Categoria e Descricao sao obrigatorias")]
        [InlineData("cat", null, "Categoria e Descricao sao obrigatorias")]
        [InlineData("cat", "   ", "Categoria e Descricao sao obrigatorias")]
        public async Task Create_Invalido_DeveRetornarBadRequest(string categoria, string descricao, string expectedMsgPart)
        {
            var dto = new CreateNotificacaoDto { Categoria = categoria, Descricao = descricao, MedicoId = 1, PacienteId = 2 };

            var result = await _controller.Create(dto);

            var bad = Assert.IsType<BadRequestObjectResult>(result);
            var prop = bad.Value!.GetType().GetProperty("message");
            Assert.NotNull(prop);
            // comparação ignorando acentos
            ContainsIgnoringDiacritics(expectedMsgPart, prop!.GetValue(bad.Value)?.ToString() ?? "");
            _repo.Verify(r => r.AddAsync(It.IsAny<Notificacao>()), Times.Never);
        }

        [Fact]
        public async Task Create_Valido_DeveAdicionar_Trimmar_E_RetornarCreatedAtAction()
        {
            var dto = new CreateNotificacaoDto
            {
                Categoria = "  Urgente ",
                Descricao = "  Marcação alterada ",
                MedicoId = 7,
                PacienteId = 77
            };

            _repo.Setup(r => r.AddAsync(It.IsAny<Notificacao>()))
                 .Callback<Notificacao>(n => n.Id = 123)
                 .Returns(Task.CompletedTask);

            var result = await _controller.Create(dto);

            var created = Assert.IsType<CreatedAtActionResult>(result);
            Assert.Equal(nameof(NotificacoesController.GetById), created.ActionName);
            Assert.Equal(123, created.RouteValues!["id"]);

            var body = Assert.IsType<NotificacaoResponseDto>(created.Value);
            Assert.Equal(123, body.Id);
            Assert.Equal("Urgente", body.Categoria);
            Assert.Equal("Marcação alterada", body.Descricao);
            Assert.Equal(7, body.MedicoId);
            Assert.Equal(77, body.PacienteId);

            _repo.Verify(r => r.AddAsync(It.Is<Notificacao>(n =>
                n.Categoria == "Urgente" &&
                n.Descricao == "Marcação alterada" &&
                n.MedicoId == 7 &&
                n.PacienteId == 77
            )), Times.Once);
        }

        // ---------------------------
        // PUT /api/Notificacoes/{id}
        // ---------------------------
        [Fact]
        public async Task Update_Inexistente_DeveRetornarNotFound()
        {
            _repo.Setup(r => r.GetByIdAsync(5)).ReturnsAsync((Notificacao?)null);

            var dto = new UpdateNotificacaoDto { Categoria = "Nova" };

            var result = await _controller.Update(5, dto);

            Assert.IsType<NotFoundResult>(result);
            _repo.Verify(r => r.GetByIdAsync(5), Times.Once);
            _repo.Verify(r => r.UpdateAsync(It.IsAny<Notificacao>()), Times.Never);
        }

        [Fact]
        public async Task Update_Valido_DeveAtualizarCampos_Trimmar_E_RetornarOkComDto()
        {
            var original = new Notificacao
            {
                Id = 9,
                Categoria = "Antiga",
                Descricao = "Desc antiga",
                Lida = false,
                MedicoId = 1,
                PacienteId = 2
            };

            _repo.Setup(r => r.GetByIdAsync(9)).ReturnsAsync(original);
            _repo.Setup(r => r.UpdateAsync(It.IsAny<Notificacao>())).Returns(Task.CompletedTask);

            var dto = new UpdateNotificacaoDto
            {
                Categoria = "  Nova Cat ",
                Descricao = "  Nova Desc ",
                Lida = true
            };

            var result = await _controller.Update(9, dto);

            var ok = Assert.IsType<OkObjectResult>(result);
            var body = Assert.IsType<NotificacaoResponseDto>(ok.Value);
            Assert.Equal(9, body.Id);
            Assert.Equal("Nova Cat", body.Categoria);
            Assert.Equal("Nova Desc", body.Descricao);
            Assert.True(body.Lida);
            Assert.Equal(1, body.MedicoId);
            Assert.Equal(2, body.PacienteId);

            _repo.Verify(r => r.UpdateAsync(It.Is<Notificacao>(n =>
                n.Id == 9 &&
                n.Categoria == "Nova Cat" &&
                n.Descricao == "Nova Desc" &&
                n.Lida == true &&
                n.MedicoId == 1 &&
                n.PacienteId == 2
            )), Times.Once);
        }

        [Fact]
        public async Task Update_ComBrancosOuNulls_NaoAlteraCampos_E_RetornaOk()
        {
            var original = new Notificacao
            {
                Id = 11,
                Categoria = "KeepCat",
                Descricao = "KeepDesc",
                Lida = false
            };

            _repo.Setup(r => r.GetByIdAsync(11)).ReturnsAsync(original);
            _repo.Setup(r => r.UpdateAsync(It.IsAny<Notificacao>())).Returns(Task.CompletedTask);

            var dto = new UpdateNotificacaoDto
            {
                Categoria = "   ",   // whitespace -> não altera
                Descricao = null,    // null -> não altera
                Lida = null          // não altera
            };

            var result = await _controller.Update(11, dto);

            var ok = Assert.IsType<OkObjectResult>(result);
            var body = Assert.IsType<NotificacaoResponseDto>(ok.Value);
            Assert.Equal(11, body.Id);
            Assert.Equal("KeepCat", body.Categoria);
            Assert.Equal("KeepDesc", body.Descricao);
            Assert.False(body.Lida);

            _repo.Verify(r => r.UpdateAsync(It.Is<Notificacao>(n =>
                n.Id == 11 &&
                n.Categoria == "KeepCat" &&
                n.Descricao == "KeepDesc" &&
                n.Lida == false
            )), Times.Once);
        }

        // ---------------------------
        // PATCH /api/Notificacoes/{id}/ler
        // ---------------------------
        [Fact]
        public async Task MarcarComoLida_DefaultTrue_Sucesso_DeveRetornarNoContent()
        {
            _repo.Setup(r => r.MarcarComoLidaAsync(4, true)).ReturnsAsync(true);

            var result = await _controller.MarcarComoLida(4);

            Assert.IsType<NoContentResult>(result);
            _repo.Verify(r => r.MarcarComoLidaAsync(4, true), Times.Once);
        }

        [Fact]
        public async Task MarcarComoLida_False_NotFound_QuandoRepoDevolveFalse()
        {
            _repo.Setup(r => r.MarcarComoLidaAsync(6, false)).ReturnsAsync(false);

            var result = await _controller.MarcarComoLida(6, Lida: false);

            Assert.IsType<NotFoundResult>(result);
            _repo.Verify(r => r.MarcarComoLidaAsync(6, false), Times.Once);
        }

        // ---------------------------
        // DELETE /api/Notificacoes/{id}
        // ---------------------------
        [Fact]
        public async Task Delete_DeveChamarRepositorio_ERetornarNoContent()
        {
            _repo.Setup(r => r.DeleteAsync(22)).Returns(Task.CompletedTask);

            var result = await _controller.Delete(22);

            Assert.IsType<NoContentResult>(result);
            _repo.Verify(r => r.DeleteAsync(22), Times.Once);
        }
    }
}
