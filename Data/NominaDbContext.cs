using System;
using System.Globalization;
using aplicacionNomina.Core.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion; // <-- correcto: ValueConversion

namespace aplicacionNomina.Core.Data
{
    public class NominaDbContext : DbContext
    {
        public NominaDbContext(DbContextOptions<NominaDbContext> options) : base(options) { }

        public DbSet<Employee> Employees => Set<Employee>();
        public DbSet<Department> Departments => Set<Department>();
        public DbSet<DeptEmp> DeptEmps => Set<DeptEmp>();
        public DbSet<DeptManager> DeptManagers => Set<DeptManager>();
        public DbSet<Title> Titles => Set<Title>();
        public DbSet<Salary> Salaries => Set<Salary>();
        public DbSet<UserAccount> Users => Set<UserAccount>();
        public DbSet<LogAuditoriaSalarios> LogAuditoriaSalarios { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder mb)
        {
            base.OnModelCreating(mb);

            // Log_AuditoriaSalarios
            mb.Entity<LogAuditoriaSalarios>(l =>
            {
                l.ToTable("Log_AuditoriaSalarios");
                l.HasKey(x => x.Id);

                l.Property(x => x.Id).HasColumnName("id");
                l.Property(x => x.Usuario).HasColumnName("usuario").HasMaxLength(50);
                l.Property(x => x.EmpNo).HasColumnName("emp_no");
                l.Property(x => x.Salario).HasColumnName("salario");
                l.Property(x => x.FechaActualizacion).HasColumnName("fechaActualizacion");
                l.Property(x => x.DetalleCambio).HasColumnName("DetalleCambio").HasMaxLength(250);
            });

            // Conversores para columnas VARCHAR(50) con fechas (titles/salaries)
            var strToDate = new ValueConverter<DateTime, string>(
                v => v.ToString("yyyy-MM-dd"),
                v => string.IsNullOrWhiteSpace(v)
                        ? new DateTime(1900, 1, 1)
                        : DateTime.Parse(v, CultureInfo.InvariantCulture)
            );

            var strToNullableDate = new ValueConverter<DateTime?, string>(
                v => v.HasValue ? v.Value.ToString("yyyy-MM-dd") : "",
                v => string.IsNullOrWhiteSpace(v)
                        ? (DateTime?)null
                        : DateTime.Parse(v, CultureInfo.InvariantCulture)
            );

            // employees
            mb.Entity<Employee>(e =>
            {
                e.ToTable("employees");
                e.HasKey(x => x.EmpNo);

                e.Property(x => x.EmpNo).HasColumnName("emp_no");
                e.Property(x => x.Ci).HasColumnName("ci").HasMaxLength(50).IsRequired();
                e.Property(x => x.BirthDate).HasColumnName("birth_date").IsRequired();
                e.Property(x => x.FirstName).HasColumnName("first_name").HasMaxLength(50).IsRequired();
                e.Property(x => x.LastName).HasColumnName("last_name").HasMaxLength(50).IsRequired();
                e.Property(x => x.Gender).HasColumnName("gender").HasMaxLength(1).IsRequired();
                e.Property(x => x.HireDate).HasColumnName("hire_date").IsRequired();
                e.Property(x => x.Correo).HasColumnName("correo").HasMaxLength(100).IsRequired(false);
                e.Property(x => x.Activo).HasColumnName("activo");
            });

            // departments
            mb.Entity<Department>(d =>
            {
                d.ToTable("departments");
                d.HasKey(x => x.DeptNo);

                d.Property(x => x.DeptNo).HasColumnName("dept_no");
                d.Property(x => x.DeptName).HasColumnName("dept_name").HasMaxLength(50).IsRequired();
                d.Property(x => x.Activo).HasColumnName("activo");
            });

            // dept_emp
            mb.Entity<DeptEmp>(de =>
            {
                de.ToTable("dept_emp");
                de.HasKey(x => new { x.EmpNo, x.DeptNo, x.FromDate });

                de.Property(x => x.EmpNo).HasColumnName("emp_no");
                de.Property(x => x.DeptNo).HasColumnName("dept_no");
                de.Property(x => x.FromDate).HasColumnName("from_date");
                de.Property(x => x.ToDate).HasColumnName("to_date");

                // Relaciones explícitas (evita columnas sombra DepartmentDeptNo / EmployeeEmpNo)
                de.HasOne(x => x.Employee)
                  .WithMany()
                  .HasForeignKey(x => x.EmpNo)
                  .HasPrincipalKey(e => e.EmpNo);     // <- sin genérico

                de.HasOne(x => x.Department)
                  .WithMany()
                  .HasForeignKey(x => x.DeptNo)
                  .HasPrincipalKey(d => d.DeptNo);     // <- sin genérico
            });

            // dept_manager
            mb.Entity<DeptManager>(dm =>
            {
                dm.ToTable("dept_manager");
                dm.HasKey(x => new { x.DeptNo, x.FromDate });

                dm.Property(x => x.EmpNo).HasColumnName("emp_no");
                dm.Property(x => x.DeptNo).HasColumnName("dept_no");
                dm.Property(x => x.FromDate).HasColumnName("from_date");
                dm.Property(x => x.ToDate).HasColumnName("to_date");

                dm.HasOne(x => x.Employee)
                  .WithMany()
                  .HasForeignKey(x => x.EmpNo)
                  .HasPrincipalKey(e => e.EmpNo);     // <- sin genérico

                dm.HasOne(x => x.Department)
                  .WithMany()
                  .HasForeignKey(x => x.DeptNo)
                  .HasPrincipalKey(d => d.DeptNo);     // <- sin genérico
            });

            // titles  (fechas VARCHAR en DB)
            mb.Entity<Title>(t =>
            {
                t.ToTable("titles");
                t.HasKey(x => new { x.EmpNo, x.TitleName, x.FromDate });

                t.Property(x => x.EmpNo).HasColumnName("emp_no");
                t.Property(x => x.TitleName).HasColumnName("title").HasMaxLength(50).IsRequired();

                t.Property(x => x.FromDate)
                    .HasColumnName("from_date")
                    .HasColumnType("varchar(50)")
                    .HasConversion(strToDate);

                t.Property(x => x.ToDate)
                    .HasColumnName("to_date")
                    .HasColumnType("varchar(50)")
                    .HasConversion(strToNullableDate);
            });

            // salaries (salary=bigint; fechas VARCHAR en DB)
            mb.Entity<Salary>(s =>
            {
                s.ToTable("salaries");
                s.HasKey(x => new { x.EmpNo, x.FromDate });

                s.Property(x => x.EmpNo).HasColumnName("emp_no");
                s.Property(x => x.Amount).HasColumnName("salary"); // bigint -> long

                s.Property(x => x.FromDate)
                    .HasColumnName("from_date")
                    .HasColumnType("varchar(50)")
                    .HasConversion(strToDate);

                s.Property(x => x.ToDate)
                    .HasColumnName("to_date")
                    .HasColumnType("varchar(50)")
                    .HasConversion(strToNullableDate);
            });

            // users
            mb.Entity<UserAccount>(u =>
            {
                u.ToTable("users");
                u.HasKey(x => new { x.EmpNo, x.Usuario });

                u.Property(x => x.EmpNo).HasColumnName("emp_no");
                u.Property(x => x.Usuario).HasColumnName("usuario").HasMaxLength(100);
                u.Property(x => x.ClaveHash).HasColumnName("clave").HasMaxLength(100);

                u.Ignore(x => x.Rol);
                u.Ignore(x => x.Activo);
            });

            // Log_AuditoriaSalarios
            mb.Entity<LogAuditoriaSalarios>(l =>
            {
                l.ToTable("Log_AuditoriaSalarios");
                l.HasKey(x => x.Id);

                l.Property(x => x.Id).HasColumnName("id");
                l.Property(x => x.Usuario).HasColumnName("usuario").HasMaxLength(50);
                l.Property(x => x.EmpNo).HasColumnName("emp_no");
                l.Property(x => x.Salario).HasColumnName("salario"); // bigint -> long
                l.Property(x => x.FechaActualizacion).HasColumnName("fechaActualizacion");
                l.Property(x => x.DetalleCambio).HasColumnName("DetalleCambio").HasMaxLength(250);
            });
        }
    }
}

