# 📊 Sistema de Nómina – Reutilización del Software

Proyecto académico desarrollado en **ASP.NET Core MVC** con **Entity Framework Core** y **SQL Server**, que implementa un sistema de gestión de nómina con auditoría de salarios, dashboard y reportes, siguiendo la especificación del documento *Especificación Sistema Nómina Reutilización del Software*.

---

## 👥 Integrantes
- Jonathan Bernal
- Carlos Quinlli
- Jonathan Coavoy
- Leonel Cepeda

---

## 🛠 Tecnologías utilizadas
- **Backend**: ASP.NET Core 8.0 MVC  
- **ORM**: Entity Framework Core  
- **Base de Datos**: SQL Server 2022  
- **Frontend**: Bootstrap 5, Chart.js  
- **Control de versiones**: GitHub  

---

## 📂 Estructura del proyecto
aplicacionNomina.Core/
├── Controllers/ # Controladores MVC
├── Data/ # DbContext y configuración EF Core
├── Models/ # Entidades del dominio
├── Services/ # Lógica de negocio (ej. SalaryService)
├── ViewModels/ # Clases auxiliares para vistas y dashboard
├── Views/ # Vistas Razor (.cshtml)
├── wwwroot/ # Archivos estáticos (css, js, bootstrap)
└── Program.cs # Configuración principal

