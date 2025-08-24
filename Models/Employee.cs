using System.ComponentModel.DataAnnotations;

namespace aplicacionNomina.Core.Models;

public class Employee
{
    [Key]
    public int EmpNo { get; set; }                          // employees.emp_no

    [Required, StringLength(50)]
    public string Ci { get; set; } = string.Empty;          // employees.ci

    [Required]
    public DateTime BirthDate { get; set; }                 // employees.birth_date (date)

    [Required, StringLength(50)]
    public string FirstName { get; set; } = string.Empty;   // employees.first_name

    [Required, StringLength(50)]
    public string LastName { get; set; } = string.Empty;    // employees.last_name

    [Required, StringLength(1)]
    public string Gender { get; set; } = "M";               // employees.gender (char(1))

    [Required]
    public DateTime HireDate { get; set; }                  // employees.hire_date (date)

    [StringLength(100)]
    public string? Correo { get; set; }                     // employees.correo (NULL)

    public bool Activo { get; set; } = true;                // employees.activo (bit)

    public string NombreCompleto => $"{FirstName} {LastName}";
}


