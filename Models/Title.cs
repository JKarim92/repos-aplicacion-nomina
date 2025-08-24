using System.ComponentModel.DataAnnotations;

namespace aplicacionNomina.Core.Models;

public class Title
{
    [Required]
    public int EmpNo { get; set; }

    [Required, StringLength(50)]
    public string TitleName { get; set; } = string.Empty;

    [Required]
    public DateTime FromDate { get; set; }    // mapeado a varchar(50) en DB

    public DateTime? ToDate { get; set; }     // mapeado a varchar(50) en DB
}
