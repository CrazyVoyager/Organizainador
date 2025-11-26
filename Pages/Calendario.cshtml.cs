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
        public required string Title { get; set; }
        public DateTime Start { get; set; }
        public DateTime? End { get; set; }
        public required string Color { get; set; }
        public required string Description { get; set; }
        public required string EventType { get; set; }
    }

    public class CalendarioModel : PageModel
    {
        private readonly AppDbContext _dbContext;

        public CalendarioModel(AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public List<AppEvent> DailyEvents { get; set; } = new List<AppEvent>();

        // --- 1. OnGet: Carga Inicial de la Página ---
        public async Task OnGetAsync()
        {
            await LoadDailyEvents(DateTime.Today);
        }

        // --- 2. Método Auxiliar: Cargar eventos de un día específico ---
        private async Task LoadDailyEvents(DateTime date)
        {
            int userId = GetCurrentUserId();

            DailyEvents = (await GetEventsForUser(userId))
                .Where(e => e.Start.Date == date.Date)
                .OrderBy(e => e.Start)
                .ToList();
        }

        // --- 3. Método Auxiliar: Obtener ID del usuario actual ---
        private int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim != null && int.TryParse(userIdClaim.Value, out int userId))
            {
                return userId;
            }
            return 0;
        }

        // --- 4. Lógica Principal: Obtener TODOS los eventos (Clases y Actividades) ---
        private async Task<List<AppEvent>> GetEventsForUser(int userId)
        {
            if (userId == 0) return new List<AppEvent>();

            var events = new List<AppEvent>();

            // ⭐ CORRECCIÓN: Obtener horarios (tanto de clases como de actividades)
            var horarios = await _dbContext.Horarios
                .Include(h => h.Clase)
                .Include(h => h.Actividad)
                .Where(h => (h.Clase != null && h.Clase.UsuarioId == userId) ||
                           (h.Actividad != null && h.Actividad.UsuarioId == userId))
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

            foreach (var h in horarios)
            {
                // ⭐ EVENTOS RECURRENTES (se repiten cada semana)
                if (h.EsRecurrente && !string.IsNullOrEmpty(h.DiaSemana))
                {
                    // Proyectamos 30 días atrás y 365 días adelante
                    for (int i = -30; i <= 365; i++)
                    {
                        DateTime day = DateTime.Today.AddDays(i);

                        if (dayMap.TryGetValue(h.DiaSemana, out DayOfWeek targetDayOfWeek) && 
                            day.DayOfWeek == targetDayOfWeek)
                        {
                            DateTime start = day.Date.Add(h.HoraInicio);
                            DateTime end = day.Date.Add(h.HoraFin);

                            if (end <= start) end = end.AddDays(1);

                            // Determinar si es Clase o Actividad
                            string titulo = "";
                            string descripcion = "";
                            string color = "";
                            string tipo = "";

                            if (h.Clase != null)
                            {
                                titulo = $"{h.Clase.Nombre} ({h.DiaSemana})";
                                descripcion = h.Clase.Descripcion ?? string.Empty;
                                color = "#2563EB"; // Azul para Clases
                                tipo = "Clase";
                            }
                            else if (h.Actividad != null)
                            {
                                titulo = $"{h.Actividad.Nombre} ({h.DiaSemana})";
                                descripcion = h.Actividad.Descripcion ?? string.Empty;
                                color = "#10B981"; // Verde para Actividades
                                tipo = "Actividad";
                            }

                            events.Add(new AppEvent
                            {
                                Id = h.Id,
                                Title = titulo,
                                Start = start,
                                End = end,
                                Color = color,
                                Description = descripcion,
                                EventType = tipo
                            });
                        }
                    }
                }
                // ⭐ EVENTOS ÚNICOS (ocurren solo una vez en una fecha específica)
                else if (!h.EsRecurrente && h.FechaEspecifica.HasValue)
                {
                    // Usar la fecha específica del horario
                    DateTime fechaEvento = h.FechaEspecifica.Value.Date;
                    DateTime start = fechaEvento.Add(h.HoraInicio);
                    DateTime end = fechaEvento.Add(h.HoraFin);

                    if (end <= start) end = end.AddDays(1);

                    string titulo = "";
                    string descripcion = "";
                    string color = "";
                    string tipo = "";

                    if (h.Clase != null)
                    {
                        titulo = $"{h.Clase.Nombre} ({fechaEvento:dd/MM})";
                        descripcion = h.Clase.Descripcion ?? string.Empty;
                        color = "#2563EB"; // Azul para Clases
                        tipo = "Clase";
                    }
                    else if (h.Actividad != null)
                    {
                        titulo = $"{h.Actividad.Nombre} ({fechaEvento:dd/MM})";
                        descripcion = h.Actividad.Descripcion ?? string.Empty;
                        color = "#10B981"; // Verde para Actividades
                        tipo = "Actividad";
                    }

                    events.Add(new AppEvent
                    {
                        Id = h.Id,
                        Title = titulo,
                        Start = start,
                        End = end,
                        Color = color,
                        Description = descripcion,
                        EventType = tipo
                    });
                }
            }

            return events;
        }

        // --- 5. Handler AJAX: OnGetEvents (Para llenar el calendario principal) ---
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
                extendedProps = new
                {
                    description = e.Description,
                    eventType = e.EventType
                }
            });

            return new JsonResult(calendarEvents);
        }

        // --- 6. Handler AJAX: OnGetDailyEvents (Para actualizar la barra lateral) ---
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
                    eventType = e.EventType
                })
                .ToList();

            return new JsonResult(new { success = true, events = dailyEvents });
        }
    }
}
