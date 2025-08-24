using System.ComponentModel.DataAnnotations;

namespace aplicacionNomina.Core.Models;

public class Department
{
    [Key]
    public int DeptNo { get; set; }                 // departments.dept_no (int)

    [Required, StringLength(50)]
    public string DeptName { get; set; } = string.Empty; // departments.dept_name

    public bool Activo { get; set; } = true;        // departments.activo (bit)
}


