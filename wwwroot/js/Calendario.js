document.addEventListener('DOMContentLoaded', function () {
    var calendarEl = document.getElementById('calendar');

    // ---- ¡IMPORTANTE! Obtenemos el Token de seguridad ----
    // Lo necesitamos para enviar datos al C# (OnPostCreateEvent)
    // Esto funciona porque el script se carga DESPUÉS de que el HTML
    // (incluyendo el @Html.AntiForgeryToken()) se haya renderizado.
    const token = document.querySelector('input[name="__RequestVerificationToken"]').value;

    var calendar = new FullCalendar.Calendar(calendarEl, {
        initialView: 'dayGridMonth',
        headerToolbar: {
            left: 'prev,next today',
            center: 'title',
            right: 'dayGridMonth,timeGridWeek,timeGridDay' // Agregamos vistas
        },
        locale: 'es',
        buttonText: {
            today: 'Hoy',
            month: 'Mes',
            week: 'Semana',
            day: 'Día'
        },

        // --- ¡NUEVA CONFIGURACIÓN PARA CREAR EVENTOS! ---

        // 1. Permite que el usuario "seleccione" días
        selectable: true,

        // 2. Esta función se ejecuta CUANDO el usuario selecciona un día
        select: async function (selectionInfo) {

            // 3. Preguntamos por el título del evento
            const title = prompt('Introduce el título del nuevo evento:');

            if (title) { // Si el usuario escribió algo y no canceló

                // 4. Preparamos los datos para enviar al C#
                const formData = new FormData();
                formData.append('title', title);
                formData.append('start', selectionInfo.startStr); // 'startStr' es la fecha/hora de inicio
                // 'selectionInfo.endStr' también está disponible si lo necesitas

                try {
                    // 5. Enviamos los datos al método OnPostCreateEvent
                    // NOTA: La URL es '/Calendar' (el nombre de tu página)
                    const response = await fetch('/Calendario?handler=CreateEvent', {
                        method: 'POST',
                        headers: {
                            // ¡El token es OBLIGATORIO!
                            'RequestVerificationToken': token
                        },
                        body: formData
                    });

                    const result = await response.json();

                    // 6. Si C# dice que todo salió bien...
                    if (result.success) {
                        // ¡Refrescamos el calendario para ver el nuevo evento!
                        calendar.refetchEvents();
                    } else {
                        alert('Error al guardar el evento: ' + result.message);
                    }

                } catch (error) {
                    console.error('Error en fetch:', error);
                    alert('Error de conexión al crear el evento.');
                }
            }
        },

        // 3. Permite que los eventos se puedan arrastrar y soltar
        editable: true,

        // --- FIN DE LA NUEVA CONFIGURACIÓN ---


        // URL para obtener los eventos (llama a OnGetEvents)
        events: '/Calendario?handler=Events',

        // (Opcional) Función para cuando haces clic en un evento existente
        eventClick: function (info) {
            if (confirm(`¿Deseas eliminar el evento "${info.event.title}"?`)) {
                // (Esto es un desafío para ti:
                // deberías crear un handler OnPostDeleteEvent en C#
                // y llamarlo aquí con fetch, similar a como creamos)
                alert('¡Funcionalidad de borrado pendiente!');
            }
        }
    });

    calendar.render();
});