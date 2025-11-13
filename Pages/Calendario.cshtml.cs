using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;
using System.Threading.Tasks;
using Organizainador.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System;
using Organizainador.Data;

namespace Organizainador.Pages
{
    // Modelo extendido para un evento del calendario
    public class AppEvent
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public DateTime Start { get; set; }
        public DateTime? End { get; set; }
        public string Color { get; set; }
        public string Description { get; set; } // Añadido para la barra lateral
        public string EventType { get; set; } // 'Clase' o 'Actividad'
    }

    public class CalendarioModel : PageModel
    {
        // 1. Reemplaza 'ApplicationDbContext' con el nombre de tu contexto de BD real
        private readonly AppDbContext _dbContext;

        public CalendarioModel(AppDbContext dbContext) // Inyección de dependencia
        {
            _dbContext = dbContext;
        }

        // Propiedad para cargar la lista de eventos del día actual en la barra lateral
        public List<AppEvent> DailyEvents { get; set; } = new List<AppEvent>();

        // --- OnGet (Carga Inicial) ---
        public async Task OnGetAsync()
        {
            // Cargar los eventos para la fecha actual en la barra lateral al cargar la página
            await LoadDailyEvents(DateTime.Today);
        }

        // Función auxiliar para obtener el ID del usuario logeado
        private int GetCurrentUserId()
        {
            // ASUME que el ID del usuario se guarda como ClaimTypes.NameIdentifier
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim != null && int.TryParse(userIdClaim.Value, out int userId))
            {
                return userId;
            }
            // Retorna 0 si no está logeado o si el ID no es válido (manejo de error)
            return 0;
        }

        // Función principal que obtiene TODAS las actividades y clases recurrentes del usuario
        private async Task<List<AppEvent>> GetEventsForUser(int userId)
        {
            if (userId == 0) return new List<AppEvent>();

            var events = new List<AppEvent>();

            // A. Obtener Actividades (ActividadModel)
            var actividades = await _dbContext.Actividades.Where(a => a.UsuarioId == userId).ToListAsync();

            foreach (var act in actividades)
            {
                // Asumimos que created_at es la fecha del evento y dura 1 hora.
                events.Add(new AppEvent
                {
                    Id = act.Id,
                    Title = act.Nombre,
                    Start = act.CreatedAt ?? DateTime.MinValue,
                    End = (act.CreatedAt ?? DateTime.MinValue).AddHours(1),
                    Color = "#dc3545", // Rojo para Actividades
                    Description = act.Descripcion,
                    EventType = "Actividad"
                });
            }

            // B. Obtener Horarios de Clases (ClaseModel + HorarioModel)
            var horarios = await _dbContext.Horarios
                .Include(h => h.Clase) // Necesitas cargar la clase asociada
                .Where(h => h.Clase.UsuarioId == userId)
                .ToListAsync();

            // Mapeo de días de la semana (ajustar si usas otro formato en tu BD)
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

            // Proyectamos los horarios recurrentes en un rango de fechas (ej: un año)
            // Esto es necesario porque FullCalendar pide fechas concretas.
            for (int i = -30; i <= 365; i++) // 30 días pasados y 1 año futuro
            {
                DateTime day = DateTime.Today.AddDays(i);

                foreach (var h in horarios)
                {
                    if (dayMap.TryGetValue(h.DiaSemana, out DayOfWeek targetDayOfWeek) && day.DayOfWeek == targetDayOfWeek)
                    {
                        DateTime start = day.Date.Add(h.HoraInicio);
                        DateTime end = day.Date.Add(h.HoraFin);

                        // Si la hora de fin es anterior a la de inicio, asume que termina el día siguiente
                        if (end <= start) end = end.AddDays(1);

                        events.Add(new AppEvent
                        {
                            Id = h.Id,
                            Title = h.Clase.Nombre + " (" + h.DiaSemana + ")",
                            Start = start,
                            End = end,
                            Color = "#0d6efd", // Azul para Clases
                            Description = h.Clase.Descripcion,
                            EventType = "Clase"
                        });
                    }
                }
            }

            return events;
        }

        // Lógica de carga para la barra lateral (usada en OnGet y LoadDailyEvents)
        private async Task LoadDailyEvents(DateTime date)
        {
            int userId = GetCurrentUserId();

            DailyEvents = (await GetEventsForUser(userId))
                .Where(e => e.Start.Date == date.Date) // Filtra solo los eventos del día
                .OrderBy(e => e.Start)
                .ToList();
        }

        // --- OnGetEvents (Handler AJAX para FullCalendar) ---
        public async Task<JsonResult> OnGetEvents()
        {
            int userId = GetCurrentUserId();
            if (userId == 0) return new JsonResult(new List<object>());

            var allEvents = await GetEventsForUser(userId);

            var calendarEvents = allEvents.Select(e => new
            {
                id = e.Id,
                title = e.Title,
                start = e.Start.ToString("yyyy-MM-ddTHH:mm:ss"),
                end = e.End?.ToString("yyyy-MM-ddTHH:mm:ss"),
                backgroundColor = e.Color,
                borderColor = e.Color,
                // Propiedades extendidas para usarse en JavaScript (p.ej. al hacer clic)
                extendedProps = new
                {
                    description = e.Description,
                    eventType = e.EventType
                }
            });

            return new JsonResult(calendarEvents);
        }

        // --- OnGetDailyEvents (NUEVO Handler AJAX para la barra lateral) ---
        // Se llama desde JavaScript al hacer clic en una fecha
        public async Task<JsonResult> OnGetDailyEvents(string date)
        {
            if (!DateTime.TryParse(date, out DateTime selectedDate))
            {
                return new JsonResult(new { success = false, message = "Formato de fecha inválido." });
            }

            int userId = GetCurrentUserId();
            if (userId == 0) return new JsonResult(new { success = false, message = "Usuario no autenticado." });

            var allEvents = await GetEventsForUser(userId);

            var dailyEvents = allEvents
                .Where(e => e.Start.Date == selectedDate.Date)
                .OrderBy(e => e.Start)
                .Select(e => new
                {
                    title = e.Title,
                    description = e.Description,
                    time = e.Start.ToString("HH:mm") + (e.End.HasValue ? " - " + e.End.Value.ToString("HH:mm") : ""),
                    color = e.Color,
                    type = e.EventType
                })
                .ToList();

            return new JsonResult(new { success = true, events = dailyEvents });
        }
    }
}