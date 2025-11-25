using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Organizainador.Data;
using Organizainador.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Organizainador.Pages
{
    public class CalendarioModel : PageModel
    {
        private readonly AppDbContext _dbContext;

        public CalendarioModel(AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public void OnGet()
        {
            // Página inicial - solo renderizar la vista
        }

        // Handler para obtener eventos
        public async Task<JsonResult> OnGetGetEvents()
        {
            int userId = GetCurrentUserId();
            if (userId == 0)
            {
                return new JsonResult(new List<object>());
            }

            var events = new List<object>();

            try
            {
                // 1. Obtener Actividades
                var actividades = await _dbContext.Actividades
                    .Where(a => a.UsuarioId == userId)
                    .ToListAsync();

                foreach (var actividad in actividades)
                {
                    if (actividad.CreatedAt.HasValue)
                    {
                        events.Add(new
                        {
                            id = actividad.Id,
                            title = actividad.Nombre,
                            start = actividad.CreatedAt.Value.ToString("yyyy-MM-ddTHH:mm:ss"),
                            backgroundColor = "#dc3545",
                            borderColor = "#dc3545",
                            description = actividad.Descripcion,
                            eventType = "Actividad"
                        });
                    }
                }

                // 2. Obtener Horarios de Clases (recurrentes)
                var horarios = await _dbContext.Horarios
                    .Include(h => h.Clase)
                    .Where(h => h.Clase != null && h.Clase.UsuarioId == userId && h.EsRecurrente)
                    .ToListAsync();

                var dayMap = new Dictionary<string, DayOfWeek>
                {
                    { "Lunes", DayOfWeek.Monday },
                    { "Martes", DayOfWeek.Tuesday },
                    { "Miércoles", DayOfWeek.Wednesday },
                    { "Jueves", DayOfWeek.Thursday },
                    { "Viernes", DayOfWeek.Friday },
                    { "Sábado", DayOfWeek.Saturday },
                    { "Domingo", DayOfWeek.Sunday }
                };

                // Generar eventos recurrentes para los próximos 3 meses y pasados 1 mes
                DateTime startDate = DateTime.Today.AddDays(-30);
                DateTime endDate = DateTime.Today.AddDays(90);

                foreach (var horario in horarios)
                {
                    if (!string.IsNullOrEmpty(horario.DiaSemana) && 
                        dayMap.TryGetValue(horario.DiaSemana, out DayOfWeek targetDay))
                    {
                        for (DateTime date = startDate; date <= endDate; date = date.AddDays(1))
                        {
                            if (date.DayOfWeek == targetDay)
                            {
                                DateTime eventStart = date.Date.Add(horario.HoraInicio);
                                DateTime eventEnd = date.Date.Add(horario.HoraFin);

                                events.Add(new
                                {
                                    id = horario.Id,
                                    title = horario.Clase?.Nombre ?? "Clase",
                                    start = eventStart.ToString("yyyy-MM-ddTHH:mm:ss"),
                                    end = eventEnd.ToString("yyyy-MM-ddTHH:mm:ss"),
                                    backgroundColor = "#0d6efd",
                                    borderColor = "#0d6efd",
                                    description = horario.Clase?.Descripcion,
                                    eventType = "Clase"
                                });
                            }
                        }
                    }
                }

                // 3. Obtener Horarios únicos (no recurrentes)
                var horariosUnicos = await _dbContext.Horarios
                    .Include(h => h.Clase)
                    .Where(h => h.Clase != null && h.Clase.UsuarioId == userId && !h.EsRecurrente)
                    .ToListAsync();

                foreach (var horario in horariosUnicos)
                {
                    if (horario.FechaEspecifica.HasValue)
                    {
                        DateTime eventStart = horario.FechaEspecifica.Value.Date.Add(horario.HoraInicio);
                        DateTime eventEnd = horario.FechaEspecifica.Value.Date.Add(horario.HoraFin);

                        events.Add(new
                        {
                            id = horario.Id,
                            title = horario.Clase?.Nombre ?? "Clase",
                            start = eventStart.ToString("yyyy-MM-ddTHH:mm:ss"),
                            end = eventEnd.ToString("yyyy-MM-ddTHH:mm:ss"),
                            backgroundColor = "#0d6efd",
                            borderColor = "#0d6efd",
                            description = horario.Clase?.Descripcion,
                            eventType = "Clase"
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al cargar eventos: {ex.Message}");
                return new JsonResult(new List<object>());
            }

            return new JsonResult(events);
        }

        private int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim != null && int.TryParse(userIdClaim.Value, out int userId))
            {
                return userId;
            }
            return 0;
        }
    }
}
