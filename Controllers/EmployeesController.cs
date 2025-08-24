using aplicacionNomina.Core.Data;
using aplicacionNomina.Core.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace aplicacionNomina.Core.Controllers;

[Authorize(Policy = "RRHHOrAdmin")]
public class EmployeesController : Controller
{
    private readonly NominaDbContext _db;
    public EmployeesController(NominaDbContext db) => _db = db;

    public async Task<IActionResult> Index(string? q, int page = 1, int pageSize = 20)
    {
        var query = _db.Employees.AsQueryable();
        if (!string.IsNullOrWhiteSpace(q))
        {
            query = query.Where(e => e.FirstName.Contains(q) || e.LastName.Contains(q) || e.Ci.Contains(q) || e.Correo.Contains(q));
        }

        var total = await query.CountAsync();
        var items = await query
            .OrderBy(e => e.EmpNo)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        ViewBag.Total = total;
        ViewBag.Page = page;
        ViewBag.PageSize = pageSize;
        ViewBag.Query = q;

        return View(items);
    }

    public IActionResult Create() => View(new Employee { HireDate = DateTime.Today, BirthDate = new DateTime(1990,1,1) });

    [HttpPost]
    public async Task<IActionResult> Create(Employee model)
    {
        if (!ModelState.IsValid) return View(model);
        _db.Employees.Add(model);
        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var e = await _db.Employees.FindAsync(id);
        if (e == null) return NotFound();
        return View(e);
    }

    [HttpPost]
    public async Task<IActionResult> Edit(int id, Employee model)
    {
        if (id != model.EmpNo) return BadRequest();
        if (!ModelState.IsValid) return View(model);
        _db.Update(model);
        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Details(int id)
    {
        var e = await _db.Employees.FindAsync(id);
        if (e == null) return NotFound();
        return View(e);
    }

    [HttpPost]
    public async Task<IActionResult> Desactivar(int id)
    {
        var e = await _db.Employees.FindAsync(id);
        if (e == null) return NotFound();
        e.Activo = false;
        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }
}
