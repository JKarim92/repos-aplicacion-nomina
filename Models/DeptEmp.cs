using System.ComponentModel.DataAnnotations;

namespace aplicacionNomina.Core.Models;

public class DeptEmp
{
    [Required]
    public int EmpNo { get; set; }                           // dept_emp.emp_no
    public Employee? Employee { get; set; }

    [Required]
    public int DeptNo { get; set; }                          // dept_emp.dept_no
    public Department? Department { get; set; }

    [Required]
    public DateTime FromDate { get; set; }                   // dept_emp.from_date (date)

    public DateTime? ToDate { get; set; }                    // dept_emp.to_date (date NULL)
}


