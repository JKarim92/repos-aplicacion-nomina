using aplicacionNomina.Core.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace aplicacionNomina.Core.Controllers;

public class AccountController : Controller
{
    private readonly NominaDbContext _db;
    public AccountController(NominaDbContext db) => _db = db;

    [AllowAnonymous]
    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        return View();
    }

    [AllowAnonymous]
    [HttpPost]
    public async Task<IActionResult> Login(string usuario, string clave, string? returnUrl = null)
    {
        // Busca por usuario en tu tabla actual
        var user = await _db.Users.AsNoTracking()
            .FirstOrDefaultAsync(u => u.Usuario == usuario);

        if (user == null)
        {
            ModelState.AddModelError(string.Empty, "Credenciales inválidas.");
            return View();
        }

        var stored = user.ClaveHash?.Trim() ?? string.Empty;
        bool ok = false;

        // Soporte a contraseñas en BCrypt ($2a/$2b/$2y) o en texto plano
        if (stored.StartsWith("$2a$") || stored.StartsWith("$2b$") || stored.StartsWith("$2y$"))
            ok = BCrypt.Net.BCrypt.Verify(clave, stored);
        else
            ok = string.Equals(stored, clave, StringComparison.Ordinal);

        if (!ok)
        {
            ModelState.AddModelError(string.Empty, "Credenciales inválidas.");
            return View();
        }

        var role = user.Rol ?? "RRHH"; // como tu tabla no tiene rol, usamos RRHH por defecto

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, user.Usuario),
            new Claim(ClaimTypes.Role, role),
            new Claim("EmpNo", user.EmpNo.ToString())
        };
        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
            return Redirect(returnUrl);
        return RedirectToAction("Index", "Home");
    }

    [Authorize]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction("Login");
    }

    [AllowAnonymous]
    public IActionResult AccessDenied() => Content("Acceso denegado.");
}


