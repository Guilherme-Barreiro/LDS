using System;
using Xunit;
using ConsultaPlus.API.Helpers;

namespace ConsultaPlus.Tests.Helpers
{
    public class DiaSemanaHelperTests
    {
        [Theory]

        [InlineData("seg", "Seg")]
        [InlineData("segunda", "Seg")]
        [InlineData("segunda-feira", "Seg")]
        [InlineData("  SeGuNdA  ", "Seg")]

        [InlineData("ter", "Ter")]
        [InlineData("terça", "Ter")]
        [InlineData("terca", "Ter")]
        [InlineData("terça-feira", "Ter")]
        [InlineData("terca-feira", "Ter")]

        [InlineData("qua", "Qua")]
        [InlineData("QuArTa", "Qua")]
        [InlineData("quarta-feira", "Qua")]
        
        [InlineData("qui", "Qui")]
        [InlineData("quinta", "Qui")]
        [InlineData("quinta-feira", "Qui")]
        
        [InlineData("sex", "Sex")]
        [InlineData("sexta", "Sex")]
        [InlineData("sexta-feira", "Sex")]
        
        [InlineData("sab", "Sab")]
        [InlineData("sábado", "Sab")]
        [InlineData("sabado", "Sab")]
        
        [InlineData("dom", "Dom")]
        [InlineData("domingo", "Dom")]
        public void Normalizar_Deve_Mapear_Dias_Conhecidos(string input, string esperado)
        {
            var r = DiaSemanaHelper.Normalizar(input);
            Assert.Equal(esperado, r);
        }

        [Theory]
        [InlineData("  TER  ", "Ter")]
        [InlineData("   segunda-feira   ", "Seg")]
        [InlineData("\tQuInTa\n", "Qui")]
        public void Normalizar_Deve_Ignorar_Espacos_Extras(string input, string esperado)
        {
            var r = DiaSemanaHelper.Normalizar(input);
            Assert.Equal(esperado, r);
        }

        [Theory]
        [InlineData("Feriado", "feriado")]
        [InlineData("QualquerCoisa", "qualquercoisa")]
        public void Normalizar_Deve_Devolver_Minusc_Para_Desconhecido(string input, string esperadoLower)
        {
            var r = DiaSemanaHelper.Normalizar(input);
            Assert.Equal(esperadoLower, r);
        }

        [Fact]
        public void Normalizar_Deve_Devolver_StringVazia_QuandoNull()
        {
            string? input = null;
            var r = DiaSemanaHelper.Normalizar(input);
            Assert.Equal(string.Empty, r);
        }

        [Fact]
        public void Normalizar_Deve_Manter_Vazio_QuandoVazio()
        {
            var r = DiaSemanaHelper.Normalizar(string.Empty);
            Assert.Equal(string.Empty, r);
        }

        [Fact]
        public void Normalizar_Deve_Manter_Espacos_QuandoSoEspacos()
        {
            var input = "   ";
            var r = DiaSemanaHelper.Normalizar(input);
            Assert.Equal(input, r);
        }
    }
}
