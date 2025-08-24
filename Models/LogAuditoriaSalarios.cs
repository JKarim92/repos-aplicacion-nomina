using System;

namespace aplicacionNomina.Core.Models
{
    public class LogAuditoriaSalarios
    {
        public int Id { get; set; }
        public string Usuario { get; set; } = "";
        public DateTime FechaActualizacion { get; set; }
        public string DetalleCambio { get; set; } = "";
        public long Salario { get; set; }     // bigint en BD → long en C#
        public int EmpNo { get; set; }        // referencia al empleado
    }
}




