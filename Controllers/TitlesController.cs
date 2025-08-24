using aplicacionNomina.Core.Data;
using aplicacionNomina.Core.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace aplicacionNomina.Core.Controllers
{
    [Authorize(Policy = "RRHHOrAdmin")]
    public class TitlesController : Controller
    {
        private readonly NominaDbContext _db;
        public TitlesController(NominaDbContext db) => _db = db;

        public async Task<IActionResult> Index(int empNo)
        {
            var emp = await _db.Employees.FindAsync(empNo);
            if (emp == null) return NotFound();

            var list = await _db.Titles
                .Where(t => t.EmpNo == empNo)
                .OrderByDescending(t => t.FromDate)
                .ToListAsync();

            ViewBag.Emp = emp;
            return View(list);
        }

        [HttpGet]
        public IActionResult Create(int empNo)
        {
            return View(new Title
            {
                EmpNo = empNo,
                FromDate = DateTime.Today
            });
        }

        [HttpPost]
        public async Task<IActionResult> Create(Title model)
        {
            if (string.IsNullOrWhiteSpace(model.TitleName))
                ModelState.AddModelError(nameof(model.TitleName), "El título es obligatorio.");
            if (model.ToDate.HasValue && model.ToDate.Value < model.FromDate)
                ModelState.AddModelError(nameof(model.ToDate), "La fecha fin no puede ser menor que la fecha inicio.");

            // Validar solapamientos
            var overlap = await _db.Titles.AnyAsync(t =>
                t.EmpNo == model.EmpNo &&
                !(t.ToDate.HasValue && t.ToDate.Value < model.FromDate) &&
                !(model.ToDate.HasValue && model.ToDate.Value < t.FromDate)
            );
            if (overlap)
                ModelState.AddModelError(string.Empty, "Existe un título que se solapa en fechas.");

            if (!ModelState.IsValid) return View(model);

            // Cerrar el vigente si corresponde
            var vigente = await _db.Titles
                .Where(t => t.EmpNo == model.EmpNo && (t.ToDate == null || t.ToDate >= model.FromDate))
                .OrderByDescending(t => t.FromDate)
                .FirstOrDefaultAsync();
            if (vigente != null) vigente.ToDate = model.FromDate.AddDays(-1);

            _db.Titles.Add(model);
            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Index), new { empNo = model.EmpNo });
        }
    }
}
