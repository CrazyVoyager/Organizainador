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
        public string Title { get; set; } = string.Empty;
        public DateTime Start { get; set; }
        public DateTime? End { get; set; }
        public string Color { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty; // Añadido para la barra lateral
        public string EventType { get; set; } = string.Empty;   // 'Clase' o 'Actividad'
    }

    public class CalendarioModel : PageModel
    {
        // Aseg�rate de que 'AppDbContext' coincida con el nombre real de tu contexto
        private readonly AppDbContext _dbContext;

        public CalendarioModel(AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        // Propiedad para cargar la lista de eventos del d�a en la barra lateral (Vista Razor)
        public List<AppEvent> DailyEvents { get; set; } = new List<AppEvent>();

        // --- 1. OnGet: Carga Inicial de la P�gina ---
        public async Task OnGetAsync()
        {
            // Al entrar a la p�gina, cargamos los eventos de HOY para la barra lateral
            await LoadDailyEvents(DateTime.Today);
        }

        // --- 2. M�todo Auxiliar: Cargar eventos de un d�a espec�fico en DailyEvents ---
        private async Task LoadDailyEvents(DateTime date)
        {
            int userId = GetCurrentUserId();

            DailyEvents = (await GetEventsForUser(userId))
                .Where(e => e.Start.Date == date.Date) // Filtra solo los eventos de esa fecha
                .OrderBy(e => e.Start)                 // Ordena por hora de inicio
                .ToList();
        }

        // --- 3. M�todo Auxiliar: Obtener ID del usuario actual ---
        private int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim != null && int.TryParse(userIdClaim.Value, out int userId))
            {
                return userId;
            }
            return 0;
        }

        // --- 4. L�gica Principal: Obtener TODAS las actividades y clases (incluyendo recurrencia) ---
        private async Task<List<AppEvent>> GetEventsForUser(int userId)
        {
            if (userId == 0) return new List<AppEvent>();

            var events = new List<AppEvent>();

            // A. Obtener Actividades
            var actividades = await _dbContext.Actividades
                .Where(a => a.UsuarioId == userId)
                .ToListAsync();

            foreach (var act in actividades)
            {
                events.Add(new AppEvent
                {
                    Id = act.Id,
                    Title = act.Nombre,
                    Start = act.CreatedAt ?? DateTime.MinValue,
                    End = (act.CreatedAt ?? DateTime.MinValue).AddHours(1),
                    Color = "#dc3545", // Rojo
                    Description = act.Descripcion,
                    EventType = "Actividad"
                });
            }

            // B. Obtener Horarios de Clases
            var horarios = await _dbContext.Horarios
                .Include(h => h.Clase)
                .Where(h => h.Clase != null && h.Clase.UsuarioId == userId)
                .ToListAsync();

            var dayMap = new Dictionary<string, DayOfWeek>
            {
                { "Lunes", DayOfWeek.Monday },
                { "Martes", DayOfWeek.Tuesday },
                { "Mi�rcoles", DayOfWeek.Wednesday },
                { "Jueves", DayOfWeek.Thursday },
                { "Viernes", DayOfWeek.Friday },
                { "S�bado", DayOfWeek.Saturday },
                { "Domingo", DayOfWeek.Sunday }
            };

            // Proyectamos horarios recurrentes (30 d�as atr�s, 1 a�o adelante)
            for (int i = -30; i <= 365; i++)
            {
                DateTime day = DateTime.Today.AddDays(i);

                foreach (var h in horarios)
                {
                    if (dayMap.TryGetValue(h.DiaSemana, out DayOfWeek targetDayOfWeek) && day.DayOfWeek == targetDayOfWeek)
                    {
                        DateTime start = day.Date.Add(h.HoraInicio);
                        DateTime end = day.Date.Add(h.HoraFin);

                        if (end <= start) end = end.AddDays(1);

                        events.Add(new AppEvent
                        {
                            Id = h.Id,
                            Title = (h.Clase?.Nombre ?? "Clase") + " (" + h.DiaSemana + ")",
                            Start = start,
                            End = end,
                            Color = "#0d6efd", // Azul
                            Description = h.Clase?.Descripcion ?? string.Empty,
                            EventType = "Clase"
                        });
                    }
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

        // --- 6. Handler AJAX: OnGetDailyEvents (Para actualizar la barra lateral al hacer click) ---
        public async Task<JsonResult> OnGetDailyEvents(string date)
        {
            if (!DateTime.TryParse(date, out DateTime selectedDate))
            {
                return new JsonResult(new { success = false, message = "Formato de fecha inv�lido." });
            }

            int userId = GetCurrentUserId();
            if (userId == 0) return new JsonResult(new { success = false, message = "Usuario no autenticado." });

            // Obtenemos todos y filtramos en memoria (se podr�a optimizar, pero reutiliza tu l�gica actual)
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
