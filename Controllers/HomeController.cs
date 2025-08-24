using System.Text.Json;
using aplicacionNomina.Core.Data;
using aplicacionNomina.Core.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace aplicacionNomina.Core.Controllers
{
    [Authorize] // quita esta línea si quieres probar sin login
    public class HomeController : Controller
    {
        private readonly NominaDbContext _db;
        public HomeController(NominaDbContext db) => _db = db;

        public async Task<IActionResult> Index()
        {
            var hoy = DateTime.Today;

            // ===== Deptos vigentes hoy: obtener FromDate máximo y volver a unir (patrón Max+JOIN) =====
            var deMax =
                from de in _db.DeptEmps.AsNoTracking()
                where de.FromDate <= hoy && (de.ToDate == null || de.ToDate >= hoy)
                group de by de.EmpNo into g
                select new { EmpNo = g.Key, FromDate = g.Max(x => x.FromDate) };

            var deLatest =
                from de in _db.DeptEmps.AsNoTracking()
                join mx in deMax on new { de.EmpNo, de.FromDate } equals new { mx.EmpNo, mx.FromDate }
                select new { de.EmpNo, de.DeptNo, de.FromDate };

            // ===== Salarios vigentes hoy: mismo patrón Max+JOIN =====
            var salMax =
                from s in _db.Salaries.AsNoTracking()
                where s.FromDate <= hoy && (s.ToDate == null || s.ToDate >= hoy)
                group s by s.EmpNo into g
                select new { EmpNo = g.Key, FromDate = g.Max(x => x.FromDate) };

            var salLatest =
                from s in _db.Salaries.AsNoTracking()
                join mx in salMax on new { s.EmpNo, s.FromDate } equals new { mx.EmpNo, mx.FromDate }
                select new { s.EmpNo, s.Amount, s.FromDate };

            // ===== KPIs =====
            var empleadosActivos = await _db.Employees.CountAsync(e => e.Activo);
            var departamentosActivos = await _db.Departments.CountAsync(d => d.Activo);
            var managersActivos = await _db.DeptManagers.CountAsync(dm => dm.FromDate <= hoy && (dm.ToDate == null || dm.ToDate >= hoy));

            var nominaVigente = await (
                from s in salLatest
                join e in _db.Employees.AsNoTracking().Where(e => e.Activo)
                    on s.EmpNo equals e.EmpNo
                select (long?)s.Amount
            ).SumAsync() ?? 0;

            var salarioProm = await (
                from s in salLatest
                join e in _db.Employees.AsNoTracking().Where(e => e.Activo)
                     on s.EmpNo equals e.EmpNo
                select (long?)s.Amount
            ).AverageAsync() ?? 0;

            var nuevos30 = await _db.Employees.CountAsync(e => e.Activo && e.HireDate >= hoy.AddDays(-30));
            var cambios30 = await _db.LogAuditoriaSalarios.CountAsync(l => l.FechaActualizacion >= hoy.AddDays(-30));

            // ===== Tabla: últimos cambios salariales (JOIN explícito con Employees) =====
            var ultimosCambios = await (
                from l in _db.LogAuditoriaSalarios.AsNoTracking()
                join e in _db.Employees.AsNoTracking() on l.EmpNo equals e.EmpNo
                orderby l.FechaActualizacion descending
                select new UltimoCambioSalario
                {
                    EmpNo = l.EmpNo,
                    Salario = l.Salario,
                    Fecha = l.FechaActualizacion,
                    Usuario = l.Usuario,
                    Nombre = e.FirstName + " " + e.LastName
                }
            )
            .Take(8)
            .ToListAsync();

            // ===== Tabla: nuevos empleados (últimos 30 días) =====
            var nuevos = await _db.Employees.AsNoTracking()
                .Where(e => e.Activo && e.HireDate >= hoy.AddDays(-30))
                .OrderByDescending(e => e.HireDate)
                .Take(8)
                .Select(e => new NuevoEmpleado
                {
                    EmpNo = e.EmpNo,
                    Nombre = e.FirstName + " " + e.LastName,
                    Alta = e.HireDate,
                    Correo = e.Correo
                })
                .ToListAsync();

            // ==== CHART 1: Nómina vigente por departamento (JOIN internos y GROUP BY simple) ====
            // 1) Armamos una vista intermedia EmpNo-DeptNo-Amount (vigentes hoy)
            var empDeptSal =
                from de in deLatest
                join s in salLatest on de.EmpNo equals s.EmpNo
                select new { de.EmpNo, de.DeptNo, s.Amount };

            // 2) Agrupamos por departamento y sumamos
            var nominaDept = await (
                from d in _db.Departments.AsNoTracking().Where(x => x.Activo)
                join eds in empDeptSal on d.DeptNo equals eds.DeptNo
                group eds by new { d.DeptNo, d.DeptName } into g
                select new
                {
                    g.Key.DeptName,
                    Total = g.Sum(x => (long?)x.Amount) ?? 0
                }
            )
            .OrderByDescending(x => x.Total)
            .Take(8)
            .ToListAsync();

            // ===== Chart 2: Cambios de salario por día (últimos 14 días) =====
            var inicio = hoy.AddDays(-13);
            var cambiosPorDia = await _db.LogAuditoriaSalarios.AsNoTracking()
                .Where(l => l.FechaActualizacion >= inicio)
                .GroupBy(l => l.FechaActualizacion.Date)
                .Select(g => new { Dia = g.Key, Cnt = g.Count() })
                .ToListAsync();

            var vm = new DashboardVm
            {
                EmpleadosActivos = empleadosActivos,
                DepartamentosActivos = departamentosActivos,
                ManagersActivos = managersActivos,
                NominaVigenteTotal = nominaVigente,
                SalarioPromedio = (decimal)salarioProm,
                AltasUltimos30d = nuevos30,
                CambiosSalario30d = cambios30,
                UltimosCambios = ultimosCambios,
                NuevosEmpleados = nuevos,
                Depts = nominaDept.Select(x => x.DeptName).ToList(),
                NominaPorDepto = nominaDept.Select(x => x.Total).ToList(),
                Dias = Enumerable.Range(0, 14).Select(i => inicio.AddDays(i).ToString("dd/MM")).ToList(),
                CambiosPorDia = Enumerable.Range(0, 14)
                    .Select(i => cambiosPorDia.FirstOrDefault(c => c.Dia == inicio.AddDays(i))?.Cnt ?? 0)
                    .ToList()
            };

            // datos para Chart.js
            ViewBag.ChartDepts = JsonSerializer.Serialize(vm.Depts);
            ViewBag.ChartNomina = JsonSerializer.Serialize(vm.NominaPorDepto);
            ViewBag.ChartDias = JsonSerializer.Serialize(vm.Dias);
            ViewBag.ChartCambios = JsonSerializer.Serialize(vm.CambiosPorDia);

            return View(vm);
        }
    }
}

