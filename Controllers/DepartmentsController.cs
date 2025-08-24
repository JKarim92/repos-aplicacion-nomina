using aplicacionNomina.Core.Data;
using aplicacionNomina.Core.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace aplicacionNomina.Core.Controllers;

[Authorize(Policy = "RRHHOrAdmin")]
public class DepartmentsController : Controller
{
    private readonly NominaDbContext _db;
    public DepartmentsController(NominaDbContext db) => _db = db;

    public async Task<IActionResult> Index(string? q, int page = 1, int pageSize = 20)
    {
        int? qNum = null;
        if (!string.IsNullOrWhiteSpace(q) && int.TryParse(q, out var n)) qNum = n;

        var query = _db.Departments
            .Where(d => d.Activo && (
                string.IsNullOrWhiteSpace(q) ||
                d.DeptName.Contains(q!) ||
                (qNum.HasValue && d.DeptNo == qNum.Value)
            ))
            .OrderBy(d => d.DeptNo);

        var list = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        ViewBag.Q = q;
        return View(list);
    }

    public IActionResult Create() => View(new Department { Activo = true });

    [HttpPost]
    public async Task<IActionResult> Create(Department model)
    {
        if (!ModelState.IsValid) return View(model);
        model.Activo = true;
        _db.Departments.Add(model);
        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var d = await _db.Departments.FindAsync(id);
        if (d == null) return NotFound();
        return View(d);
    }

    [HttpPost]
    public async Task<IActionResult> Edit(Department model)
    {
        if (!ModelState.IsValid) return View(model);
        _db.Update(model);
        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> Disable(int id)
    {
        var d = await _db.Departments.FindAsync(id);
        if (d == null) return NotFound();
        d.Activo = false;
        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }
}
