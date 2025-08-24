using aplicacionNomina.Core.Data;
using aplicacionNomina.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace aplicacionNomina.Core.Services
{
    public class SalaryService
    {
        private readonly NominaDbContext _db;
        private readonly IHttpContextAccessor _http;

        public SalaryService(NominaDbContext db, IHttpContextAccessor http)
        {
            _db = db;
            _http = http;
        }

        public async Task AddAsync(int empNo, long amount, DateTime fromDate)
        {
            using var tx = await _db.Database.BeginTransactionAsync();

            var f = fromDate.Date;

            // 1) Cerrar salario vigente si existe
            var vigente = await _db.Salaries
                .SingleOrDefaultAsync(s => s.EmpNo == empNo && s.ToDate == null);

            if (vigente != null)
            {
                if (vigente.FromDate == f)
                {
                    // Misma fecha: actualiza el monto
                    vigente.Amount = amount;
                    await _db.SaveChangesAsync();

                    // Auditar actualización
                    await RegistrarAuditoria(empNo, amount, vigente.Amount, "Actualización de salario (misma fecha)");
                    await tx.CommitAsync();
                    return;
                }

                if (vigente.FromDate > f)
                    throw new InvalidOperationException($"Existe un salario vigente que inicia en {vigente.FromDate:yyyy-MM-dd}.");

                vigente.ToDate = f.AddDays(-1);
                _db.Salaries.Update(vigente);
                await _db.SaveChangesAsync();
            }

            // 2) Crear nuevo salario
            var nuevo = new Salary
            {
                EmpNo = empNo,
                Amount = amount,
                FromDate = f,
                ToDate = null
            };
            _db.Salaries.Add(nuevo);
            await _db.SaveChangesAsync();

            // 3) Registrar auditoría
            await RegistrarAuditoria(empNo, amount, vigente?.Amount, vigente == null ? "Registro inicial de salario" : "Actualización de salario");

            await tx.CommitAsync();
        }

        private async Task RegistrarAuditoria(int empNo, long nuevo, long? anterior, string motivo)
        {
            var usuario = _http.HttpContext?.User?.Identity?.Name ?? "sistema";
            var detalle = anterior.HasValue
                ? $"{motivo}: de {anterior.Value} a {nuevo}"
                : $"{motivo}: {nuevo}";

            _db.LogAuditoriaSalarios.Add(new LogAuditoriaSalarios
            {
                Usuario = usuario,
                FechaActualizacion = DateTime.Now,
                DetalleCambio = detalle,
                Salario = nuevo,
                EmpNo = empNo
            });

            await _db.SaveChangesAsync();
        }
    }
}
