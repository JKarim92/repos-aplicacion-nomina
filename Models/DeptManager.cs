using System.ComponentModel.DataAnnotations;

namespace aplicacionNomina.Core.Models;

public class DeptManager
{
    [Required]
    public int EmpNo { get; set; }                           // dept_manager.emp_no
    public Employee? Employee { get; set; }

    [Required]
    public int DeptNo { get; set; }                          // dept_manager.dept_no
    public Department? Department { get; set; }

    [Required]
    public DateTime FromDate { get; set; }                   // dept_manager.from_date (date)

    public DateTime? ToDate { get; set; }                    // dept_manager.to_date (date NULL)
}

