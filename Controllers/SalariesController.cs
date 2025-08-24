using aplicacionNomina.Core.Data;
using aplicacionNomina.Core.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace aplicacionNomina.Core.Controllers
{
    [Authorize(Policy = "RRHHOrAdmin")]
    public class SalariesController : Controller
    {
        private readonly NominaDbContext _db;

        public SalariesController(NominaDbContext db)
        {
            _db = db;
        }

        // GET /Salaries?empNo=111
        public async Task<IActionResult> Index(int empNo)
        {
            var emp = await _db.Employees.AsNoTracking()
                         .FirstOrDefaultAsync(e => e.EmpNo == empNo);
            if (emp == null) return NotFound();

            var list = await _db.Salaries.AsNoTracking()
                         .Where(s => s.EmpNo == empNo)
                         .OrderByDescending(s => s.FromDate)
                         .ToListAsync();

            ViewBag.Emp = emp;
            return View(list);
        }

        // GET /Salaries/Create?empNo=111
        [HttpGet]
        public async Task<IActionResult> Create(int empNo)
        {
            var emp = await _db.Employees.FindAsync(empNo);
            if (emp == null) return NotFound();

            ViewBag.Emp = emp;
            return View(new Salary { EmpNo = empNo, FromDate = DateTime.Today });
        }

        // POST /Salaries/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("EmpNo,Amount,FromDate")] Salary m)
        {
            if (m.Amount <= 0) ModelState.AddModelError(nameof(m.Amount), "El salario debe ser mayor a cero.");
            var emp = await _db.Employees.FindAsync(m.EmpNo);
            if (emp == null) ModelState.AddModelError(string.Empty, "Empleado no encontrado.");
            if (!ModelState.IsValid) { ViewBag.Emp = emp; return View(m); }

            var f = m.FromDate.Date;
            var usuario = User?.Identity?.Name ?? "sistema";

            using var tx = await _db.Database.BeginTransactionAsync();

            // buscar salario vigente
            var vigente = await _db.Salaries.SingleOrDefaultAsync(s => s.EmpNo == m.EmpNo && s.ToDate == null);

            if (vigente != null && vigente.FromDate == f)
            {
                var anterior = vigente.Amount;
                vigente.Amount = m.Amount;
                _db.Salaries.Update(vigente);
                await _db.SaveChangesAsync();

                _db.LogAuditoriaSalarios.Add(new LogAuditoriaSalarios
                {
                    Usuario = usuario,
                    FechaActualizacion = DateTime.Now,
                    EmpNo = m.EmpNo,
                    Salario = m.Amount,
                    DetalleCambio = $"Actualización de salario (misma fecha {f:yyyy-MM-dd}) de {anterior} a {m.Amount}"
                });
                await _db.SaveChangesAsync();

                await tx.CommitAsync();
                return RedirectToAction(nameof(Index), new { empNo = m.EmpNo });
            }

            if (vigente != null && vigente.FromDate > f)
            {
                ModelState.AddModelError(string.Empty, $"Existe un salario vigente que inicia en {vigente.FromDate:yyyy-MM-dd}.");
                ViewBag.Emp = emp;
                return View(m);
            }

            if (vigente != null)
            {
                vigente.ToDate = f.AddDays(-1);
                _db.Salaries.Update(vigente);
                await _db.SaveChangesAsync();
            }

            var nuevo = new Salary { EmpNo = m.EmpNo, Amount = m.Amount, FromDate = f, ToDate = null };
            _db.Salaries.Add(nuevo);
            await _db.SaveChangesAsync();

            _db.LogAuditoriaSalarios.Add(new LogAuditoriaSalarios
            {
                Usuario = usuario,
                FechaActualizacion = DateTime.Now,
                EmpNo = m.EmpNo,
                Salario = m.Amount,
                DetalleCambio = (vigente == null)
                    ? $"Registro inicial de salario {m.Amount} desde {f:yyyy-MM-dd}"
                    : $"Actualización de salario de {vigente.Amount} a {m.Amount} desde {f:yyyy-MM-dd}"
            });
            await _db.SaveChangesAsync();

            await tx.CommitAsync();
            return RedirectToAction(nameof(Index), new { empNo = m.EmpNo });
        }
    }
}
