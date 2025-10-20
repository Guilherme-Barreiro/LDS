using ConsultaPlus.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace ConsultaPlus.Infrastructure.Data
{
    public class ApplicationDbContext : DbContext
    {
        
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        // Cada DbSet está a representar cada tabela da base de dados
        public DbSet<Paciente> Pacientes { get; set; }
        public DbSet<Medico> Medicos { get; set; }
        public DbSet<Consulta> Consultas { get; set; }
        public DbSet<Especialidade> Especialidades { get; set; }
        public DbSet<Sala> Salas { get; set; }
        public DbSet<HorarioTrabalhoMedico> HorariosTrabalhoMedicos { get; set; }
        public DbSet<HorarioExcecaoMedico> HorariosExcecaoMedicos { get; set; }
        public DbSet<EspecialidadeMedico> EspecialidadesMedico { get; set; }
        public DbSet<Notificacao> Notificacoes { get; set; }


        // O método OnModelCreating é onde configuramos as relações complexas
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configurar a chave primária composta para a tabela  EspecialidadeMedico
            modelBuilder.Entity<EspecialidadeMedico>()
                .HasKey(em => new { em.MedicoId, em.EspecialidadeId });

            // Configurar a relação N-N entre Medico e Especialidade
            modelBuilder.Entity<EspecialidadeMedico>()
                .HasOne(em => em.Medico)
                .WithMany(m => m.EspecialidadesMedico)
                .HasForeignKey(em => em.MedicoId);

            modelBuilder.Entity<EspecialidadeMedico>()
                .HasOne(em => em.Especialidade)
                .WithMany(e => e.EspecialidadesMedico)
                .HasForeignKey(em => em.EspecialidadeId);

            // garantir que o N_Utente é único 
            modelBuilder.Entity<Paciente>()
                .HasIndex(p => p.NUtente)
                .IsUnique();

            modelBuilder.Entity<Medico>()
                .HasIndex(m => m.NUtente)
                .IsUnique();
        }
    }
}