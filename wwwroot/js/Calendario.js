document.addEventListener('DOMContentLoaded', function () {
    var calendarEl = document.getElementById('calendar');

    // Verificar que el calendario existe
    if (!calendarEl) {
        console.error('No se encontró el elemento #calendar');
        return;
    }

    // Token de seguridad para peticiones POST
    const tokenElement = document.querySelector('input[name="__RequestVerificationToken"]');
    if (!tokenElement) {
        console.error('No se encontró el token anti-forgery');
        return;
    }
    const token = tokenElement.value;

    // Actualizar reloj en tiempo real
    updateClock();
    setInterval(updateClock, 1000);

    // Cargar eventos del día actual al iniciar
    const today = new Date().toISOString().split('T')[0];
    updateDailyEvents(today);

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
        
        // Cuando se hace clic en una fecha
        dateClick: function(info) {
            // Actualizar eventos del día en la barra lateral
            const clickedDate = info.dateStr;
            updateDailyEvents(clickedDate);
        },

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

                    if (!response.ok) {
                        throw new Error(`HTTP error! status: ${response.status}`);
                    }

                    const result = await response.json();

                    if (result.success) {
                        // Mostrar notificación de éxito
                        showNotification('✅ Evento creado exitosamente', 'success');
                        calendar.refetchEvents();

                        // Actualizar lista de eventos del día si es hoy
                        if (isToday(selectionInfo.start)) {
                            const dateStr = new Date(selectionInfo.start).toISOString().split('T')[0];
                            updateDailyEvents(dateStr);
                        }
                    } else {
                        showNotification('❌ Error: ' + (result.message || 'No se pudo crear el evento'), 'error');
                    }

                } catch (error) {
                    console.error('Error al crear evento:', error);
                    showNotification('❌ Error de conexión: ' + error.message, 'error');
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
        events: function(info, successCallback, failureCallback) {
            fetch('/Calendario?handler=Events')
                .then(response => {
                    if (!response.ok) {
                        throw new Error(`HTTP error! status: ${response.status}`);
                    }
                    return response.json();
                })
                .then(data => {
                    console.log('Eventos cargados:', data.length);
                    successCallback(data);
                })
                .catch(error => {
                    console.error('Error al cargar eventos:', error);
                    showNotification('❌ Error al cargar eventos: ' + error.message, 'error');
                    failureCallback(error);
                });
        },

        // Función al hacer clic en un evento
        eventClick: function (info) {
            // Detenemos la propagación para evitar que FullCalendar maneje el click si es en el botón de 3 puntos
            if (info.jsEvent.target.classList.contains('event-menu-btn')) {
                info.jsEvent.stopPropagation();
                showEventOptionsMenu(info.event);
            } else {
                // Si se hace clic en cualquier otra parte del evento, mostramos el modal de opciones
                showEventOptionsMenu(info.event);
            }
        },

        // Cambiar color y añadir botón de menú
        eventDidMount: function (info) {
            // Agregar tooltip
            info.el.title = info.event.extendedProps.description || info.event.title;

            // Ajuste de colores (basado en el Calendario.cshtml.cs)
            if (info.event.extendedProps.eventType === 'Clase') {
                info.el.style.backgroundColor = '#0d6efd'; // Azul
            } else if (info.event.extendedProps.eventType === 'Actividad') {
                info.el.style.backgroundColor = '#dc3545'; // Rojo
            }

            // Añadir botón de menú de 3 puntos
            const menuButton = document.createElement('span');
            menuButton.innerHTML = '⋮'; // Símbolo de tres puntos vertical
            menuButton.className = 'event-menu-btn';

            // Estilos básicos para el botón
            menuButton.style.cssText = `
                position: absolute;
                top: 0;
                right: 5px;
                font-weight: bold;
                cursor: pointer;
                color: white;
                font-size: 1.2em;
            `;

            info.el.appendChild(menuButton);
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

    async function updateDailyEvents(date) {
        try {
            // Si no se proporciona fecha, usar hoy
            const targetDate = date || new Date().toISOString().split('T')[0];
            const response = await fetch('/Calendario?handler=DailyEvents&date=' + targetDate);
            
            if (!response.ok) {
                throw new Error(`HTTP error! status: ${response.status}`);
            }

            const result = await response.json();
            
            if (!result.success) {
                console.error('Error en el servidor:', result.message);
                return;
            }

            const events = result.events || [];

            const eventList = document.querySelector('.event-list');
            if (eventList) {
                if (events.length > 0) {
                    eventList.innerHTML = events.map(event => `
                        <div class="event-item" style="border-left-color: ${event.color || '#ccc'};">
                            <strong>${escapeHtml(event.title)}</strong>
                            <div class="event-time">
                                ${escapeHtml(event.time)}
                            </div>
                            ${event.description ? `<p>${escapeHtml(event.description)}</p>` : ''}
                        </div>
                    `).join('');
                } else {
                    eventList.innerHTML = `
                        <div class="no-events">
                            <strong>No hay eventos</strong>
                            <p>No tienes actividades programadas para hoy.</p>
                        </div>
                    `;
                }
            }
        } catch (error) {
            console.error('Error al actualizar eventos del día:', error);
            showNotification('❌ Error al actualizar eventos del día', 'error');
        }
    }

    // Función auxiliar para escapar HTML y prevenir XSS
    function escapeHtml(text) {
        const div = document.createElement('div');
        div.textContent = text;
        return div.innerHTML;
    }

    // --- Lógica del Modal de Opciones (Manipula el HTML de _Modal.cshtml) ---
    function showEventOptionsMenu(event) {
        const modal = document.getElementById('eventOptionsModal');
        if (!modal) {
            console.error('No se encontró el modal #eventOptionsModal');
            return;
        }

        // Asegúrate que los IDs de los elementos internos del modal coincidan con tu _Modal.cshtml
        const modalTitle = document.getElementById('modalEventTitle');
        const modalTime = document.getElementById('modalEventTime');
        const btnDetails = document.getElementById('btnDetails');
        const btnDelete = document.getElementById('btnDelete');
        const modalCloseButton = document.getElementById('modalCloseButton');

        if (!modalTitle || !modalTime || !btnDetails || !btnDelete || !modalCloseButton) {
            console.error('Faltan elementos del modal');
            return;
        }

        // 1. Llenar el contenido del modal
        modalTitle.textContent = event.title;
        modalTime.textContent = `${formatDate(event.start)}${event.end ? ' - ' + formatDate(event.end) : ''}`;

        const isActivity = event.extendedProps.eventType === 'Actividad';

        // 2. Controlar la visibilidad de "Ver Detalles" (solo para Actividades)
        // El botón debe tener display:none por defecto en el HTML.
        if (isActivity) {
            btnDetails.style.display = 'block';
        } else {
            btnDetails.style.display = 'none';
        }

        // 3. Mostrar el modal con transición
        modal.classList.add('show');
        modal.style.display = 'flex';

        // 4. Limpiar Event Listeners anteriores (Delegación o Clone Node)
        // Usaremos 'Clone Node' en los botones importantes para garantizar que no haya múltiples listeners
        const cleanAndAttachListeners = (button, action) => {
            const newButton = button.cloneNode(true);
            button.parentNode.replaceChild(newButton, button);

            newButton.onclick = function () {
                closeModal();
                handleModalAction(action, event);
            };
            return newButton;
        };

        const btnEdit = document.getElementById('btnEdit');
        if (btnEdit) {
            cleanAndAttachListeners(btnEdit, 'edit');
        }
        cleanAndAttachListeners(btnDelete, 'delete');
        cleanAndAttachListeners(btnDetails, 'details');

        // Función para cerrar el modal
        const closeModal = () => {
            modal.classList.remove('show');
            setTimeout(() => { modal.style.display = 'none'; }, 300); // Esperar la transición
        };

        // Manejadores de eventos de cierre
        // Al hacer clic en el fondo
        modal.onclick = function (e) {
            if (e.target.id === 'eventOptionsModal') { // Usa el ID del div de fondo
                closeModal();
            }
        };

        // Al hacer clic en el botón de cerrar
        modalCloseButton.onclick = closeModal;
    }

    // Función para manejar las acciones del modal
    function handleModalAction(action, event) {
        switch (action) {
            case 'edit':
                editEvent(event);
                break;
            case 'delete':
                deleteEvent(event);
                break;
            case 'details':
                showActivityDetails(event);
                break;
        }
    }

    // --- Función para editar un evento ---
    function editEvent(event) {
        const eventType = event.extendedProps.eventType;
        
        if (eventType === 'Actividad') {
            // Redirigir a la página de edición de actividades
            window.location.href = `/Actividades/Edit/${event.id}`;
        } else if (eventType === 'Clase') {
            // Redirigir a la página de edición de horarios (para clases)
            window.location.href = `/Horarios/Edit/${event.id}`;
        } else {
            showNotification('⚠️ No se puede editar este tipo de evento', 'error');
        }
    }

    // --- Función para mostrar detalles de una Actividad ---
    function showActivityDetails(event) {
        const details = `
        **Detalles de la Actividad**
        ---
        📌 **Título**: ${event.title}
        ${event.extendedProps.description ? '📄 **Descripción**: ' + event.extendedProps.description : '📄 **Descripción**: N/A'}
        ⏰ **Inicio**: ${formatDate(event.start)}
        ${event.end ? '⏰ **Fin**: ' + formatDate(event.end) : ''}
        `;

        alert(details); // Usamos un simple `alert` para mostrar los detalles
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
                    const dateStr = new Date(event.start).toISOString().split('T')[0];
                    updateDailyEvents(dateStr);
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

    // Agregar animaciones CSS para la notificación
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