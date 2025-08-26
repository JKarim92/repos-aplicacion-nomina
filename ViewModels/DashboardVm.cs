namespace aplicacionNomina.Core.ViewModels
{
    public class DashboardVm
    {
        // KPIs
        public int EmpleadosActivos { get; set; }
        public int DepartamentosActivos { get; set; }
        public int ManagersActivos { get; set; }
        public long NominaVigenteTotal { get; set; }   // suma salarios vigentes hoy
        public decimal SalarioPromedio { get; set; }
        public int AltasUltimos30d { get; set; }       // nuevos empleados (hire_date)
        public int CambiosSalario30d { get; set; }     // en Log_AuditoriaSalarios

        // Tablas inferiores
        public List<UltimoCambioSalario> UltimosCambios { get; set; } = new();
        public List<NuevoEmpleado> NuevosEmpleados { get; set; } = new();

        // Datos para charts
        public List<string> Depts { get; set; } = new();
        public List<long> NominaPorDepto { get; set; } = new();

        public List<string> Dias { get; set; } = new();           // últimos 14 días
        public List<int> CambiosPorDia { get; set; } = new();
    }

    public class UltimoCambioSalario
    {
        public int EmpNo { get; set; }
        public string Nombre { get; set; } = "";
        public long Salario { get; set; }
        public DateTime Fecha { get; set; }
        public string Usuario { get; set; } = "";
    }

    public class NuevoEmpleado
    {
        public int EmpNo { get; set; }
        public string Nombre { get; set; } = "";
        public DateTime Alta { get; set; }
        public string Correo { get; set; } = "";
    }
}
