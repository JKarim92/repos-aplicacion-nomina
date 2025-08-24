using System.ComponentModel.DataAnnotations;

namespace aplicacionNomina.Core.Models;

public class Salary
{
    [Required]
    public int EmpNo { get; set; }

    [Required]
    public long Amount { get; set; }   // bigint

    [Required]
    public DateTime FromDate { get; set; }   // mapeado a varchar(50) en DB

    public DateTime? ToDate { get; set; }    // mapeado a varchar(50) en DB
}
