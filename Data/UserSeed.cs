using aplicacionNomina.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace aplicacionNomina.Core.Data;

public class UserSeed
{
    private readonly NominaDbContext _db;

    public UserSeed(NominaDbContext db)
    {
        _db = db;
    }

    public async Task SeedAsync()
    {
        // Seed empleado base
        if (!await _db.Employees.AnyAsync())
        {
            _db.Employees.Add(new Employee
            {
                EmpNo = 1,
                Ci = "0102030405",
                BirthDate = new DateTime(1990, 1, 1),
                FirstName = "Admin",
                LastName = "Sistema",
                Gender = "O",
                HireDate = DateTime.Today,
                Correo = "admin@empresa.com",
                Activo = true
            });
            await _db.SaveChangesAsync();
        }

        // Seed usuario admin con BCrypt
        if (!await _db.Users.AnyAsync())
        {
            var admin = new UserAccount
            {
                EmpNo = 1,
                Usuario = "admin",
                Rol = "Admin",
                Activo = true
            };
            admin.ClaveHash = BCrypt.Net.BCrypt.HashPassword("Admin123$");
            _db.Users.Add(admin);
            await _db.SaveChangesAsync();
        }
    }
}
