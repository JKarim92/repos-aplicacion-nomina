using aplicacionNomina.Core.Data;
using aplicacionNomina.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace aplicacionNomina.Core.Services;

public class DeptEmpService
{
    private readonly NominaDbContext _db;
    public DeptEmpService(NominaDbContext db) => _db = db;

    // crea nueva asignación cerrando la anterior si corresponde
    public async Task AddAsync(int empNo, int deptNo, DateTime fromDate, DateTime? toDate = null)
    {
        // solapamiento
        var overlap = await _db.DeptEmps.AnyAsync(d =>
            d.EmpNo == empNo &&
            !(d.ToDate.HasValue && d.ToDate.Value < fromDate) &&
            !(toDate.HasValue && toDate.Value < d.FromDate)
        );
        if (overlap) throw new InvalidOperationException("Asignación solapada.");

        var vigente = await _db.DeptEmps
            .Where(d => d.EmpNo == empNo && (d.ToDate == null || d.ToDate >= fromDate))
            .OrderByDescending(d => d.FromDate)
            .FirstOrDefaultAsync();
        if (vigente != null) vigente.ToDate = fromDate.AddDays(-1);

        _db.DeptEmps.Add(new DeptEmp { EmpNo = empNo, DeptNo = deptNo, FromDate = fromDate, ToDate = toDate });
        await _db.SaveChangesAsync();
    }
}

