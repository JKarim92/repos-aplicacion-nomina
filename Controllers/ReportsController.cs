using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using aplicacionNomina.Core.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

// Si vas a exportar a Excel:
using ClosedXML.Excel;

namespace aplicacionNomina.Core.Controllers
{
    [Authorize(Policy = "RRHHOrAdmin")]
    public class ReportsController : Controller
    {
        private readonly NominaDbContext _db;

        public ReportsController(NominaDbContext db)
        {
            _db = db;
        }

        // ------------------ NÓMINA VIGENTE (por si ya lo usabas) ------------------
        public async Task<IActionResult> NominaVigente(int? deptNo, DateTime? fecha)
        {
            var f = (fecha ?? DateTime.Today).Date;

            ViewBag.Departments = await _db.Departments
                .AsNoTracking()
                .Where(d => d.Activo)
                .OrderBy(d => d.DeptName)
                .ToListAsync();

            ViewBag.Fecha = f;
            ViewBag.DeptNo = deptNo;

            var deFiltered = _db.DeptEmps.AsNoTracking()
                .Where(d => d.FromDate <= f && (d.ToDate == null || d.ToDate >= f));

            var deKeys = deFiltered
                .GroupBy(d => d.EmpNo)
                .Select(g => new { EmpNo = g.Key, FromDate = g.Max(x => x.FromDate) });

            var deLatest = from d in deFiltered
                           join k in deKeys
                               on new { d.EmpNo, d.FromDate } equals new { k.EmpNo, k.FromDate }
                           select new { d.EmpNo, d.DeptNo };

            if (deptNo.HasValue)
                deLatest = deLatest.Where(x => x.DeptNo == deptNo.Value);

            var salFiltered = _db.Salaries.AsNoTracking()
                .Where(s => s.FromDate <= f && (s.ToDate == null || s.ToDate >= f));

            var salKeys = salFiltered
                .GroupBy(s => s.EmpNo)
                .Select(g => new { EmpNo = g.Key, FromDate = g.Max(x => x.FromDate) });

            var salLatest = from s in salFiltered
                            join k in salKeys
                                on new { s.EmpNo, s.FromDate } equals new { k.EmpNo, k.FromDate }
                            select new { s.EmpNo, s.Amount };

            var query =
                from e in _db.Employees.AsNoTracking().Where(x => x.Activo)
                join de in deLatest on e.EmpNo equals de.EmpNo
                join d in _db.Departments.AsNoTracking().Where(x => x.Activo) on de.DeptNo equals d.DeptNo
                join s in salLatest on e.EmpNo equals s.EmpNo into sj
                from s in sj.DefaultIfEmpty()
                select new
                {
                    e.EmpNo,
                    e.FirstName,
                    e.LastName,
                    DeptNo = d.DeptNo,
                    DeptName = d.DeptName,
                    Amount = (long?)s.Amount ?? 0L
                };

            var lista = await query
                .OrderBy(r => r.DeptName)
                .ThenBy(r => r.LastName)
                .ThenBy(r => r.FirstName)
                .ToListAsync();

            return View(lista);
        }

        // ------------------ AUDITORÍA DE SALARIOS (LISTA) ------------------
        public async Task<IActionResult> AuditoriaSalarios(int? empNo, DateTime? desde, DateTime? hasta)
        {
            var f1 = (desde ?? DateTime.Today.AddMonths(-1)).Date;
            var f2 = (hasta ?? DateTime.Today).Date;

            // Combo empleados
            ViewBag.Emps = await _db.Employees.AsNoTracking()
                .Where(e => e.Activo)
                .OrderBy(e => e.LastName).ThenBy(e => e.FirstName)
                .Select(e => new { e.EmpNo, Nombre = e.FirstName + " " + e.LastName })
                .ToListAsync();

            ViewBag.EmpNo = empNo;
            ViewBag.Desde = f1;
            ViewBag.Hasta = f2;

            var q = _db.LogAuditoriaSalarios.AsNoTracking()
                .Where(l => l.FechaActualizacion >= f1 && l.FechaActualizacion <= f2);

            if (empNo.HasValue) q = q.Where(l => l.EmpNo == empNo.Value);

            var lista = await q
                .OrderByDescending(l => l.FechaActualizacion)
                .ThenBy(l => l.EmpNo)
                .ToListAsync();

            return View(lista);
        }

        // ------------------ AUDITORÍA DE SALARIOS (EXCEL) ------------------
        public async Task<IActionResult> AuditoriaSalariosExcel(int? empNo, DateTime? desde, DateTime? hasta)
        {
            var f1 = (desde ?? DateTime.Today.AddMonths(-1)).Date;
            var f2 = (hasta ?? DateTime.Today).Date;

            var q = _db.LogAuditoriaSalarios.AsNoTracking()
                .Where(l => l.FechaActualizacion >= f1 && l.FechaActualizacion <= f2);

            if (empNo.HasValue) q = q.Where(l => l.EmpNo == empNo.Value);

            var data = await q
                .OrderByDescending(l => l.FechaActualizacion)
                .ThenBy(l => l.EmpNo)
                .Select(l => new
                {
                    l.EmpNo,
                    l.Usuario,
                    Salario = l.Salario,
                    Fecha = l.FechaActualizacion,
                    l.DetalleCambio
                })
                .ToListAsync();

            using var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add("Auditoria");
            ws.Cell(1, 1).InsertTable(data);
            ws.Columns().AdjustToContents();

            using var ms = new MemoryStream();
            wb.SaveAs(ms);
            ms.Position = 0;
            var fileName = $"AuditoriaSalarios_{f1:yyyyMMdd}_{f2:yyyyMMdd}.xlsx";

            return File(ms.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                fileName);
        }
    }
}
