using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Collections.Generic;
using System.Linq;
using System;
using System.Threading.Tasks; // Necesario para tareas asíncronas

namespace Organizainador.Pages
{
    // Modelo para un evento
    public class AppEvent
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public DateTime Start { get; set; }
        public DateTime? End { get; set; }
        public string Color { get; set; }
    }

    public class CalendarioModel : PageModel
    {
        // --- CAMBIO IMPORTANTE ---
        // Usamos una lista ESTÁTICA para simular una base de datos.
        // Los datos persistirán mientras la app esté corriendo.
        private static int s_eventCounter = 1; // Un contador simple
        private static readonly List<AppEvent> s_db = new List<AppEvent>
        {
            // Puedes dejar algunos eventos de ejemplo si quieres
            // new AppEvent { Id = s_eventCounter++, Title = "Evento Inicial", Start = DateTime.Now.Date, Color = "#0d6efd" }
        };


        // Este método se ejecuta cuando se carga la página (GET)
        public void OnGet()
        {
        }

        // Este método es llamado por FullCalendar para OBTENER eventos
        public JsonResult OnGetEvents()
        {
            var calendarEvents = s_db.Select(e => new
            {
                id = e.Id,
                title = e.Title,
                start = e.Start.ToString("yyyy-MM-ddTHH:mm:ss"), // Formato ISO 8601
                end = e.End?.ToString("yyyy-MM-ddTHH:mm:ss"),
                backgroundColor = e.Color,
                borderColor = e.Color
            });

            return new JsonResult(calendarEvents);
        }

        // --- ¡NUEVO MÉTODO! ---
        // Este método es llamado por JavaScript (fetch) para CREAR un evento (POST)
        // Necesita [ValidateAntiForgeryToken] por seguridad.
        [ValidateAntiForgeryToken]
        public async Task<JsonResult> OnPostCreateEvent(string title, string start)
        {
            try
            {
                var newEvent = new AppEvent
                {
                    Id = s_eventCounter++,
                    Title = title,
                    Start = DateTime.Parse(start), // Convierte el string de fecha
                    Color = "#5cb85c" // Verde por defecto para eventos nuevos
                };

                s_db.Add(newEvent); // Agregamos el evento a nuestra "BD"

                // Devolvemos éxito y el ID del nuevo evento
                return new JsonResult(new { success = true, id = newEvent.Id });
            }
            catch (Exception ex)
            {
                return new JsonResult(new { success = false, message = ex.Message });
            }
        }
    }
}