using System.ComponentModel.DataAnnotations;

namespace aplicacionNomina.Core.Models;

public class UserAccount
{
    // PK compuesta (EmpNo, Usuario) -> la definimos en DbContext
    public int EmpNo { get; set; }                            // users.emp_no

    [Required, StringLength(100)]
    public string Usuario { get; set; } = string.Empty;       // users.usuario

    [Required, StringLength(100)]
    public string ClaveHash { get; set; } = string.Empty;     // users.clave (hash o texto)

    // Campos lógicos no mapeados en BD (se ignorarán en DbContext si no existen)
    public string Rol { get; set; } = "RRHH";
    public bool Activo { get; set; } = true;
}



