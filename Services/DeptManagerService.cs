using aplicacionNomina.Core.Data;
using aplicacionNomina.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace aplicacionNomina.Core.Services;

public class DeptManagerService
{
    private readonly NominaDbContext _db;
    public DeptManagerService(NominaDbContext db) => _db = db;

    /// <summary>
    /// Asigna un gerente a un departamento desde una fecha (inclusive).
    /// - Cierra la jefatura previa si estaba vigente en esa fecha.
    /// - Evita solapamientos (sólo un manager vigente por depto en la misma fecha).
    /// </summary>
    public async Task AssignAsync(int deptNo, int empNo, DateTime fromDate, DateTime? toDate = null)
    {
        if (toDate.HasValue && toDate.Value < fromDate)
            throw new InvalidOperationException("La fecha 'to_date' no puede ser anterior a 'from_date'.");

        // ¿Existe solapamiento con alguna jefatura de ese departamento?
        var overlap = await _db.DeptManagers.AnyAsync(m =>
            m.DeptNo == deptNo &&
            // rango (m.FromDate..m.ToDate) se cruza con (fromDate..toDate)
            !(m.ToDate.HasValue && m.ToDate.Value < fromDate) &&
            !(toDate.HasValue && toDate.Value < m.FromDate)
        );

        if (overlap)
        {
            // Antes de abortar, intentamos cerrar la vigente si empieza antes de fromDate
            var vigente = await _db.DeptManagers
                .Where(m => m.DeptNo == deptNo && (m.ToDate == null || m.ToDate >= fromDate))
                .OrderByDescending(m => m.FromDate)
                .FirstOrDefaultAsync();

            if (vigente != null && vigente.FromDate <= fromDate)
            {
                // cerrar la vigente el día anterior
                vigente.ToDate = fromDate.AddDays(-1);
            }
            else
            {
                throw new InvalidOperationException("Ya existe un gerente vigente que se solapa en fechas para este departamento.");
            }
        }

        _db.DeptManagers.Add(new DeptManager
        {
            DeptNo = deptNo,
            EmpNo = empNo,
            FromDate = fromDate,
            ToDate = toDate
        });

        await _db.SaveChangesAsync();
    }

    /// <summary>
    /// Termina (pone ToDate) la jefatura vigente de un departamento.
    /// </summary>
    public async Task EndCurrentAsync(int deptNo, DateTime endDate)
    {
        var vigente = await _db.DeptManagers
            .Where(m => m.DeptNo == deptNo && (m.ToDate == null || m.ToDate >= endDate))
            .OrderByDescending(m => m.FromDate)
            .FirstOrDefaultAsync();

        if (vigente == null)
            throw new InvalidOperationException("No existe una jefatura vigente para terminar.");

        if (endDate < vigente.FromDate)
            throw new InvalidOperationException("La fecha de fin no puede ser anterior a la fecha de inicio actual.");

        vigente.ToDate = endDate;
        await _db.SaveChangesAsync();
    }

    /// <summary>
    /// Devuelve el gerente vigente (si lo hay) para un departamento y fecha.
    /// </summary>
    public Task<DeptManager?> GetCurrentAsync(int deptNo, DateTime onDate)
    {
        return _db.DeptManagers
            .Where(m => m.DeptNo == deptNo && m.FromDate <= onDate && (m.ToDate == null || m.ToDate >= onDate))
            .OrderByDescending(m => m.FromDate)
            .FirstOrDefaultAsync();
    }

    /// <summary>
    /// Historial completo de gerentes de un departamento (más reciente primero).
    /// </summary>
    public Task<List<DeptManager>> GetHistoryAsync(int deptNo)
    {
        return _db.DeptManagers
            .Where(m => m.DeptNo == deptNo)
            .OrderByDescending(m => m.FromDate)
            .ToListAsync();
    }
}
