using ConsultaPlus.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace ConsultaPlus.Infrastructure.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

        // Tabelas
        public virtual DbSet<Paciente> Pacientes { get; set; }
        public virtual DbSet<Medico> Medicos { get; set; }
        public virtual DbSet<Consulta> Consultas { get; set; }
        public virtual DbSet<Especialidade> Especialidades { get; set; }
        public virtual DbSet<Sala> Salas { get; set; }
        public virtual DbSet<HorarioTrabalhoMedico> HorariosTrabalhoMedicos { get; set; }
        public virtual DbSet<HorarioExcecaoMedico> HorariosExcecaoMedicos { get; set; }
        public virtual DbSet<EspecialidadeMedico> EspecialidadesMedico { get; set; }
        public virtual DbSet<Notificacao> Notificacoes { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<EspecialidadeMedico>()
                .HasKey(em => new { em.MedicoId, em.EspecialidadeId });

            modelBuilder.Entity<EspecialidadeMedico>()
                .HasOne(em => em.Medico)
                .WithMany(m => m.EspecialidadesMedico)
                .HasForeignKey(em => em.MedicoId);

            modelBuilder.Entity<EspecialidadeMedico>()
                .HasOne(em => em.Especialidade)
                .WithMany(e => e.EspecialidadesMedico)
                .HasForeignKey(em => em.EspecialidadeId);

            modelBuilder.Entity<Paciente>()
                .HasIndex(p => p.NUtente)
                .IsUnique();

            modelBuilder.Entity<Medico>()
                .HasIndex(m => m.NUtente)
                .IsUnique();

            modelBuilder.Entity<Notificacao>(b =>
            {
                b.Property(n => n.DataCriacao).HasDefaultValueSql("GETUTCDATE()");
                b.Property(n => n.Lida).HasDefaultValue(false);

                b.HasOne(n => n.Medico)
                    .WithMany()
                    .HasForeignKey(n => n.MedicoId)
                    .OnDelete(DeleteBehavior.SetNull);

                b.HasOne(n => n.Paciente)
                    .WithMany()
                    .HasForeignKey(n => n.PacienteId)
                    .OnDelete(DeleteBehavior.SetNull);

                b.HasIndex(n => new { n.MedicoId, n.Lida });
                b.HasIndex(n => new { n.PacienteId, n.Lida });
            });

        }
    }
}
