using aplicacionNomina.Core.Data;
using aplicacionNomina.Core.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace aplicacionNomina.Core.Controllers
{
    [Authorize(Policy = "RRHHOrAdmin")]
    public class DeptEmpController : Controller
    {
        private readonly NominaDbContext _db;
        public DeptEmpController(NominaDbContext db) => _db = db;

        // GET: /DeptEmp?empNo=111
        public async Task<IActionResult> Index(int empNo, int page = 1, int pageSize = 50)
        {
            var emp = await _db.Employees.AsNoTracking().FirstOrDefaultAsync(e => e.EmpNo == empNo);
            if (emp == null) return NotFound();

            var list = await _db.DeptEmps.AsNoTracking()
                .Where(x => x.EmpNo == empNo)
                .Include(x => x.Department)                 // gracias al mapeo explícito en el DbContext
                .OrderByDescending(x => x.FromDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.Emp = emp;
            return View(list); // Views/DeptEmp/Index.cshtml
        }

        // GET: /DeptEmp/Create?empNo=111
        [HttpGet]
        public async Task<IActionResult> Create(int empNo)
        {
            var emp = await _db.Employees.FindAsync(empNo);
            if (emp == null) return NotFound();

            var depts = await _db.Departments.AsNoTracking()
                .Where(d => d.Activo)
                .OrderBy(d => d.DeptName)
                .ToListAsync();

            ViewBag.Emp = emp;
            ViewBag.Depts = depts;

            return View(new DeptEmp
            {
                EmpNo = empNo,
                FromDate = DateTime.Today
            });
        }

        // POST: /DeptEmp/Create
        [HttpPost]
        public async Task<IActionResult> Create(int empNo, int deptNo, DateTime fromDate)
        {
            var emp = await _db.Employees.FindAsync(empNo);
            if (emp == null) return NotFound();

            var f = fromDate.Date;

            // validar solapamiento
            var vigente = await _db.DeptEmps
                .Where(x => x.EmpNo == empNo && x.ToDate == null)
                .OrderByDescending(x => x.FromDate)
                .FirstOrDefaultAsync();

            if (vigente != null && vigente.FromDate >= f)
            {
                ModelState.AddModelError(string.Empty, "Existe una asignación vigente que se solapa en fechas.");
            }

            if (!ModelState.IsValid)
            {
                ViewBag.Emp = emp;
                ViewBag.Depts = await _db.Departments.AsNoTracking()
                    .Where(d => d.Activo)
                    .OrderBy(d => d.DeptName)
                    .ToListAsync();

                return View(new DeptEmp { EmpNo = empNo, DeptNo = deptNo, FromDate = f });
            }

            // cerrar vigente
            if (vigente != null) vigente.ToDate = f.AddDays(-1);

            // nueva asignación (to_date NULL = vigente)
            _db.DeptEmps.Add(new DeptEmp
            {
                EmpNo = empNo,
                DeptNo = deptNo,
                FromDate = f,
                ToDate = null // asegúrate de que la columna permite NULL
            });

            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Index), new { empNo });
        }
    }
}

