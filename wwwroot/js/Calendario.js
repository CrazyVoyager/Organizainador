document.addEventListener('DOMContentLoaded', function () {
    var calendarEl = document.getElementById('calendar');

    // Token de seguridad para peticiones POST
    const token = document.querySelector('input[name="__RequestVerificationToken"]').value;

    // Actualizar reloj en tiempo real
    updateClock();
    setInterval(updateClock, 1000);

    var calendar = new FullCalendar.Calendar(calendarEl, {
        initialView: 'dayGridMonth',
        headerToolbar: {
            left: 'prev,next today',
            center: 'title',
            right: 'dayGridMonth,timeGridWeek,timeGridDay,listWeek'
        },
        locale: 'es',
        buttonText: {
            today: 'Hoy',
            month: 'Mes',
            week: 'Semana',
            day: 'Día',
            list: 'Lista'
        },

        // Mejoras de UX
        height: 'auto',
        expandRows: true,
        slotMinTime: '06:00:00',
        slotMaxTime: '23:00:00',
        allDaySlot: true,
        nowIndicator: true, // Línea que muestra la hora actual
        slotDuration: '00:30:00', // Intervalos de 30 minutos

        // Navegación mejorada
        navLinks: true, // Permite hacer clic en los días para ir a la vista de día

        // Selección de rangos de tiempo
        selectable: true,
        selectMirror: true, // Muestra una pre-visualización mientras seleccionas

        // Función para crear eventos
        select: async function (selectionInfo) {
            const title = prompt('📝 Introduce el título del evento:');

            if (title && title.trim()) {
                const description = prompt('📄 Descripción (opcional):');

                const formData = new FormData();
                formData.append('title', title.trim());
                formData.append('start', selectionInfo.startStr);
                formData.append('end', selectionInfo.endStr);
                if (description) {
                    formData.append('description', description.trim());
                }

                try {
                    const response = await fetch('/Calendario?handler=CreateEvent', {
                        method: 'POST',
                        headers: {
                            'RequestVerificationToken': token
                        },
                        body: formData
                    });

                    const result = await response.json();

                    if (result.success) {
                        // Mostrar notificación de éxito
                        showNotification('✅ Evento creado exitosamente', 'success');
                        calendar.refetchEvents();

                        // Actualizar lista de eventos del día si es hoy
                        if (isToday(selectionInfo.start)) {
                            updateDailyEvents();
                        }
                    } else {
                        showNotification('❌ Error: ' + result.message, 'error');
                    }

                } catch (error) {
                    console.error('Error en fetch:', error);
                    showNotification('❌ Error de conexión', 'error');
                }
            }

            calendar.unselect(); // Limpiar selección
        },

        // Permitir arrastrar y soltar eventos
        editable: true,

        // Función cuando se mueve un evento
        eventDrop: async function (info) {
            if (!confirm(`¿Mover "${info.event.title}" a ${formatDate(info.event.start)}?`)) {
                info.revert();
                return;
            }

            await updateEvent(info.event);
        },

        // Función cuando se redimensiona un evento
        eventResize: async function (info) {
            await updateEvent(info.event);
        },

        // Cargar eventos desde el servidor
        events: '/Calendario?handler=Events',

        // Función al cargar eventos exitosamente
        eventSourceSuccess: function (content, xhr) {
            console.log('Eventos cargados:', content.length);
        },

        // Función al cargar eventos con error
        eventSourceFailure: function (error) {
            console.error('Error al cargar eventos:', error);
            showNotification('❌ Error al cargar eventos', 'error');
        },

        // Función al hacer clic en un evento
        eventClick: function (info) {
            showEventDetails(info.event);
        },

        // Cambiar color según el tipo de evento
        eventDidMount: function (info) {
            // Agregar tooltip
            info.el.title = info.event.extendedProps.description || info.event.title;

            // Agregar clase personalizada si es necesario
            if (info.event.extendedProps.type === 'clase') {
                info.el.style.backgroundColor = '#667eea';
            } else if (info.event.extendedProps.type === 'actividad') {
                info.el.style.backgroundColor = '#38ef7d';
            }
        },

        // Función cuando cambia la vista
        datesSet: function (dateInfo) {
            console.log('Vista cambiada:', dateInfo.view.type);
        }
    });

    calendar.render();

    // Botón para agregar evento rápido
    const addEventBtn = document.querySelector('.add-event-btn');
    if (addEventBtn) {
        addEventBtn.addEventListener('click', function () {
            const now = new Date();
            calendar.select(now);
        });
    }

    // Funciones auxiliares

    function updateClock() {
        const clockElement = document.querySelector('.current-time');
        if (clockElement) {
            const now = new Date();
            clockElement.textContent = now.toLocaleTimeString('es-ES', {
                hour: '2-digit',
                minute: '2-digit',
                second: '2-digit'
            });
        }
    }

    function isToday(date) {
        const today = new Date();
        const checkDate = new Date(date);
        return today.toDateString() === checkDate.toDateString();
    }

    function formatDate(date) {
        return new Date(date).toLocaleDateString('es-ES', {
            weekday: 'long',
            year: 'numeric',
            month: 'long',
            day: 'numeric',
            hour: '2-digit',
            minute: '2-digit'
        });
    }

    async function updateEvent(event) {
        const formData = new FormData();
        formData.append('id', event.id);
        formData.append('title', event.title);
        formData.append('start', event.start.toISOString());
        if (event.end) {
            formData.append('end', event.end.toISOString());
        }

        try {
            const response = await fetch('/Calendario?handler=UpdateEvent', {
                method: 'POST',
                headers: {
                    'RequestVerificationToken': token
                },
                body: formData
            });

            const result = await response.json();

            if (result.success) {
                showNotification('✅ Evento actualizado', 'success');
            } else {
                showNotification('❌ Error al actualizar', 'error');
                calendar.refetchEvents();
            }
        } catch (error) {
            console.error('Error:', error);
            showNotification('❌ Error de conexión', 'error');
            calendar.refetchEvents();
        }
    }

    async function updateDailyEvents() {
        try {
            const response = await fetch('/Calendario?handler=DailyEvents');
            const events = await response.json();

            const eventList = document.querySelector('.event-list');
            if (eventList && events.length > 0) {
                eventList.innerHTML = events.map(event => `
                    <div class="event-item" style="border-left-color: ${event.color || '#ccc'};">
                        <strong>${event.title}</strong>
                        <div class="event-time">
                            ${new Date(event.start).toLocaleTimeString('es-ES', { hour: '2-digit', minute: '2-digit' })}
                            ${event.end ? ' - ' + new Date(event.end).toLocaleTimeString('es-ES', { hour: '2-digit', minute: '2-digit' }) : ''}
                        </div>
                        ${event.description ? `<p>${event.description}</p>` : ''}
                    </div>
                `).join('');
            }
        } catch (error) {
            console.error('Error al actualizar eventos del día:', error);
        }
    }

    function showEventDetails(event) {
        const details = `
📌 ${event.title}
⏰ ${formatDate(event.start)}${event.end ? ' - ' + formatDate(event.end) : ''}
${event.extendedProps.description ? '📄 ' + event.extendedProps.description : ''}

¿Qué deseas hacer?`;

        const action = confirm(details + '\n\n✅ OK para eliminar\n❌ Cancelar para cerrar');

        if (action) {
            deleteEvent(event);
        }
    }

    async function deleteEvent(event) {
        if (!confirm(`¿Estás seguro de eliminar "${event.title}"?`)) {
            return;
        }

        const formData = new FormData();
        formData.append('id', event.id);

        try {
            const response = await fetch('/Calendario?handler=DeleteEvent', {
                method: 'POST',
                headers: {
                    'RequestVerificationToken': token
                },
                body: formData
            });

            const result = await response.json();

            if (result.success) {
                showNotification('🗑️ Evento eliminado', 'success');
                calendar.refetchEvents();

                if (isToday(event.start)) {
                    updateDailyEvents();
                }
            } else {
                showNotification('❌ Error al eliminar', 'error');
            }
        } catch (error) {
            console.error('Error:', error);
            showNotification('❌ Error de conexión', 'error');
        }
    }

    function showNotification(message, type = 'info') {
        // Crear elemento de notificación
        const notification = document.createElement('div');
        notification.className = `notification notification-${type}`;
        notification.textContent = message;
        notification.style.cssText = `
            position: fixed;
            top: 20px;
            right: 20px;
            padding: 15px 25px;
            background: ${type === 'success' ? '#38ef7d' : type === 'error' ? '#ff6b6b' : '#667eea'};
            color: white;
            border-radius: 10px;
            box-shadow: 0 4px 12px rgba(0,0,0,0.2);
            z-index: 10000;
            font-weight: 600;
            animation: slideIn 0.3s ease-out;
        `;

        document.body.appendChild(notification);

        setTimeout(() => {
            notification.style.animation = 'slideOut 0.3s ease-out';
            setTimeout(() => notification.remove(), 300);
        }, 3000);
    }

    // Agregar animaciones CSS
    const style = document.createElement('style');
    style.textContent = `
        @keyframes slideIn {
            from {
                transform: translateX(400px);
                opacity: 0;
            }
            to {
                transform: translateX(0);
                opacity: 1;
            }
        }
        @keyframes slideOut {
            from {
                transform: translateX(0);
                opacity: 1;
            }
            to {
                transform: translateX(400px);
                opacity: 0;
            }
        }
    `;
    document.head.appendChild(style);
});