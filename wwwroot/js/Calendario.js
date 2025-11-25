/**
 * CALENDARIO PROFESIONAL - MODO CLARO
 * Sistema de gestión de horarios y actividades
 */

document.addEventListener('DOMContentLoaded', function () {
    // ==================== INICIALIZACIÓN ====================
    const calendarEl = document.getElementById('calendar');
    const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value;

    if (!calendarEl) {
        console.error('Elemento de calendario no encontrado');
        return;
    }

    // Iniciar reloj en tiempo real
    initClock();

    // ==================== CONFIGURACIÓN DEL CALENDARIO ====================
    const calendar = new FullCalendar.Calendar(calendarEl, {
        // Configuración inicial
        initialView: 'dayGridMonth',
        locale: 'es',
        timeZone: 'America/Santiago',
        firstDay: 1, // Lunes como primer día

        // Toolbar
        headerToolbar: {
            left: 'prev,next today',
            center: 'title',
            right: 'dayGridMonth,timeGridWeek,timeGridDay,listWeek'
        },

        buttonText: {
            today: 'Hoy',
            month: 'Mes',
            week: 'Semana',
            day: 'Día',
            list: 'Agenda'
        },

        // ⭐ CONFIGURACIÓN DE FORMATO DE HORA
        eventTimeFormat: {
            hour: '2-digit',
            minute: '2-digit',
            hour12: false,
            meridiem: false
        },

        // Configuración de visualización
        height: 'auto',
        expandRows: true,
        slotMinTime: '06:00:00',
        slotMaxTime: '23:00:00',
        slotDuration: '00:30:00',
        allDaySlot: true,
        nowIndicator: true,
        navLinks: true,
        weekNumbers: false,
        dayMaxEvents: 3,
        displayEventTime: true, // ⭐ Mostrar hora en eventos

        // Interactividad
        selectable: false,
        editable: true,
        droppable: false,

        // Callbacks principales
        events: fetchEvents,
        eventDidMount: handleEventMount,
        eventClick: handleEventClick,
        eventDrop: handleEventDrop,
        eventResize: handleEventResize,
        datesSet: handleDatesSet,

        // ⭐ FORMATO DEL CONTENIDO DEL EVENTO
        eventContent: function(arg) {
            const eventType = arg.event.extendedProps.eventType || 'default';
            const colors = getEventColors(eventType);
            
            // Formatear hora de inicio y fin
            const startTime = arg.event.start ? formatTime(arg.event.start) : '';
            const endTime = arg.event.end ? formatTime(arg.event.end) : '';
            const timeRange = endTime ? `${startTime} - ${endTime}` : startTime;
            
            return {
                html: `
                    <div class="fc-event-main-frame">
                        <div class="fc-event-time">${timeRange}</div>
                        <div class="fc-event-title-container">
                            <div class="fc-event-title fc-sticky">
                                ${colors.icon} ${arg.event.title}
                            </div>
                        </div>
                    </div>
                `
            };
        },

        // Mensajes
        moreLinkText: function(num) {
            return `+${num} más`;
        },

        // Configuración de loading
        loading: function(isLoading) {
            if (isLoading) {
                showLoadingState();
            } else {
                hideLoadingState();
            }
        }
    });

    calendar.render();

    // ==================== DEFINICIÓN DE COLORES ====================
    const eventColors = {
        'Clase': {
            background: '#EFF6FF',
            text: '#1E40AF',
            border: '#2563EB',
            icon: '📚'
        },
        'Actividad': {
            background: '#D1FAE5',
            text: '#065F46',
            border: '#10B981',
            icon: '✅'
        },
        'Tarea': {
            background: '#FEF3C7',
            text: '#92400E',
            border: '#F59E0B',
            icon: '📝'
        },
        'default': {
            background: '#F3F4F6',
            text: '#374151',
            border: '#9CA3AF',
            icon: '📌'
        }
    };

    // ==================== FUNCIONES DE EVENTOS ====================

    /**
     * Cargar eventos desde el servidor
     */
    async function fetchEvents(fetchInfo, successCallback, failureCallback) {
        try {
            const response = await fetch('/Calendario?handler=Events');
            
            if (!response.ok) {
                throw new Error('Error al cargar eventos');
            }

            const events = await response.json();
            successCallback(events);
            
            // Actualizar lista de eventos del día
            updateDailyEvents();
            
        } catch (error) {
            console.error('Error fetching events:', error);
            showNotification('❌ Error al cargar eventos', 'error');
            failureCallback(error);
        }
    }

    /**
     * Obtener colores según tipo de evento
     */
    function getEventColors(eventType) {
        return eventColors[eventType] || eventColors['default'];
    }

    /**
     * Configurar evento al montarse
     */
    function handleEventMount(info) {
        // ⭐ Tooltip con hora de inicio y fin
        const startTime = info.event.start ? formatTime(info.event.start) : '';
        const endTime = info.event.end ? formatTime(info.event.end) : '';
        const timeRange = endTime ? `${startTime} - ${endTime}` : startTime;
        const tooltip = `${info.event.title}\n${timeRange}${info.event.extendedProps.description ? '\n' + info.event.extendedProps.description : ''}`;
        info.el.title = tooltip;

        // Obtener tipo de evento
        const eventType = info.event.extendedProps.eventType || 'default';
        const colors = getEventColors(eventType);
        
        // Añadir atributo data-type para CSS
        info.el.setAttribute('data-type', eventType.toLowerCase());
        
        // Aplicar colores personalizados
        info.el.style.background = colors.background;
        info.el.style.color = colors.text;
        info.el.style.borderLeft = `4px solid ${colors.border}`;
        info.el.style.borderRadius = '0.375rem';
        info.el.style.padding = '4px 8px';
        info.el.style.fontWeight = '600';

        // Añadir botón de menú
        addEventMenuButton(info.el);
    }

    /**
     * Añadir botón de menú a evento
     */
    function addEventMenuButton(element) {
        const menuButton = document.createElement('span');
        menuButton.innerHTML = '⋮';
        menuButton.className = 'event-menu-btn';
        menuButton.onclick = function(e) {
            e.stopPropagation();
        };
        element.style.position = 'relative';
        element.appendChild(menuButton);
    }

    /**
     * Manejar click en evento
     */
    function handleEventClick(info) {
        info.jsEvent.preventDefault();
        showEventModal(info.event);
    }

    /**
     * Manejar arrastre de evento
     */
    async function handleEventDrop(info) {
        const startTime = formatTime(info.event.start);
        const endTime = info.event.end ? formatTime(info.event.end) : '';
        const timeRange = endTime ? `${startTime} - ${endTime}` : startTime;
        
        if (!confirm(`¿Mover "${info.event.title}" a ${timeRange}?`)) {
            info.revert();
            return;
        }

        const success = await updateEventOnServer(info.event);
        if (!success) {
            info.revert();
        }
    }

    /**
     * Manejar redimensión de evento
     */
    async function handleEventResize(info) {
        const success = await updateEventOnServer(info.event);
        if (!success) {
            info.revert();
        }
    }

    /**
     * Manejar cambio de fechas
     */
    function handleDatesSet(dateInfo) {
        console.log('Vista cambiada a:', dateInfo.view.type);
        updateDailyEvents();
    }

    // ==================== ACTUALIZACIÓN DE EVENTOS ====================

    /**
     * Actualizar evento en el servidor
     */
    async function updateEventOnServer(event) {
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
                showNotification('✅ Evento actualizado correctamente', 'success');
                updateDailyEvents();
                return true;
            } else {
                showNotification('❌ Error al actualizar evento', 'error');
                return false;
            }
        } catch (error) {
            console.error('Error:', error);
            showNotification('❌ Error de conexión', 'error');
            return false;
        }
    }

    /**
     * Eliminar evento
     */
    async function deleteEvent(event) {
        if (!confirm(`¿Estás seguro de eliminar "${event.title}"?\n\nEsta acción no se puede deshacer.`)) {
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
                showNotification('🗑️ Evento eliminado correctamente', 'success');
                calendar.refetchEvents();
                updateDailyEvents();
            } else {
                showNotification('❌ Error al eliminar evento', 'error');
            }
        } catch (error) {
            console.error('Error:', error);
            showNotification('❌ Error de conexión', 'error');
        }
    }

    // ==================== ACTUALIZAR EVENTOS DEL DÍA ====================

    /**
     * Actualizar lista de eventos del día
     */
    async function updateDailyEvents() {
        try {
            const today = new Date().toISOString().split('T')[0];
            const response = await fetch(`/Calendario?handler=DailyEvents&date=${today}`);
            
            if (!response.ok) throw new Error('Error al cargar eventos del día');

            const data = await response.json();
            const events = data.events || data || [];

            renderDailyEvents(events);
        } catch (error) {
            console.error('Error al actualizar eventos del día:', error);
        }
    }

    /**
     * Renderizar eventos del día
     */
    function renderDailyEvents(events) {
        const eventList = document.querySelector('.event-list');
        if (!eventList) return;

        if (events.length === 0) {
            eventList.innerHTML = `
                <div class="no-events">
                    <strong>No hay eventos</strong>
                    <p>No tienes actividades programadas para hoy.</p>
                </div>
            `;
            return;
        }

        eventList.innerHTML = events.map(event => {
            // Determinar el tipo de evento
            let eventType = event.eventType || 'default';
            if (event.color === '#2563EB') eventType = 'Clase';
            else if (event.color === '#10B981') eventType = 'Actividad';
            else if (event.color === '#F59E0B') eventType = 'Tarea';

            const colors = getEventColors(eventType);
            
            return `
                <div class="event-item event-item-${eventType.toLowerCase()}" 
                     style="border-left-color: ${colors.border}; background: ${colors.background};">
                    <div style="display: flex; align-items: center; gap: 0.5rem; margin-bottom: 0.375rem;">
                        <span style="font-size: 1rem;">${colors.icon}</span>
                        <strong style="color: ${colors.text};">${escapeHtml(event.title)}</strong>
                    </div>
                    <div class="event-time" style="color: ${colors.text}; opacity: 0.8; font-weight: 600;">
                        ⏰ ${event.time}
                    </div>
                    ${event.description ? `<p style="color: ${colors.text}; opacity: 0.7; font-size: 0.875rem; margin-top: 0.25rem;">${escapeHtml(event.description)}</p>` : ''}
                </div>
            `;
        }).join('');
    }

    // ==================== MODAL DE EVENTO ====================

    /**
     * Mostrar modal de opciones
     */
    function showEventModal(event) {
        const modal = document.getElementById('eventOptionsModal');
        if (!modal) return;

        // Actualizar contenido
        updateModalContent(event);

        // Configurar botones
        setupModalButtons(event);

        // Mostrar modal
        modal.classList.add('show');
        modal.style.display = 'flex';

        // Configurar cierre
        setupModalClose(modal);
    }

    /**
     * Actualizar contenido del modal
     */
    function updateModalContent(event) {
        const modalTitle = document.getElementById('modalEventTitle');
        const modalTime = document.getElementById('modalEventTime');
        const btnDetails = document.getElementById('btnDetails');

        const eventType = event.extendedProps.eventType || 'default';
        const colors = getEventColors(eventType);

        if (modalTitle) {
            modalTitle.innerHTML = `${colors.icon} ${escapeHtml(event.title)}`;
            modalTitle.style.color = colors.text;
        }

        if (modalTime) {
            // ⭐ Mostrar solo las horas
            const startTime = event.start ? formatTime(event.start) : '';
            const endTime = event.end ? formatTime(event.end) : '';
            const timeRange = endTime ? `${startTime} - ${endTime}` : startTime;
            modalTime.textContent = `⏰ ${timeRange}`;
        }

        // Mostrar/ocultar botón de detalles
        if (btnDetails) {
            const isActivity = event.extendedProps.eventType === 'Actividad';
            btnDetails.style.display = isActivity ? 'block' : 'none';
        }
    }

    /**
     * Configurar botones del modal
     */
    function setupModalButtons(event) {
        setupButton('btnEdit', () => handleEditEvent(event));
        setupButton('btnDelete', () => deleteEvent(event));
        setupButton('btnDetails', () => showActivityDetails(event));
    }

    /**
     * Configurar un botón individual
     */
    function setupButton(buttonId, action) {
        const button = document.getElementById(buttonId);
        if (!button) return;

        const newButton = button.cloneNode(true);
        button.parentNode.replaceChild(newButton, button);

        newButton.onclick = function() {
            closeModal();
            action();
        };
    }

    /**
     * Configurar cierre del modal
     */
    function setupModalClose(modal) {
        const closeButton = document.getElementById('modalCloseButton');
        
        if (closeButton) {
            closeButton.onclick = closeModal;
        }

        modal.onclick = function(e) {
            if (e.target.id === 'eventOptionsModal') {
                closeModal();
            }
        };
    }

    /**
     * Cerrar modal
     */
    function closeModal() {
        const modal = document.getElementById('eventOptionsModal');
        if (!modal) return;

        modal.classList.remove('show');
        setTimeout(() => {
            modal.style.display = 'none';
        }, 300);
    }

    // ==================== ACCIONES DE EVENTO ====================

    /**
     * Editar evento
     */
    function handleEditEvent(event) {
        showNotification('🔧 Función de edición en desarrollo...', 'info');
        // TODO: Implementar edición
    }

    /**
     * Mostrar detalles de actividad
     */
    function showActivityDetails(event) {
        const eventType = event.extendedProps.eventType || 'default';
        const colors = getEventColors(eventType);
        
        const startTime = event.start ? formatTime(event.start) : '';
        const endTime = event.end ? formatTime(event.end) : '';
        const timeRange = endTime ? `${startTime} - ${endTime}` : startTime;
        
        const details = `
${colors.icon} Detalles del Evento
━━━━━━━━━━━━━━━━━━━━━

📌 Título: ${event.title}
🏷️ Tipo: ${eventType}

${event.extendedProps.description ? `📄 Descripción: ${event.extendedProps.description}` : '📄 Sin descripción'}

⏰ Horario: ${timeRange}
📅 Fecha: ${formatDate(event.start)}
        `;

        alert(details.trim());
    }

    // ==================== UTILIDADES ====================

    /**
     * Inicializar reloj en tiempo real
     */
    function initClock() {
        updateClock();
        setInterval(updateClock, 1000);
    }

    /**
     * Actualizar reloj
     */
    function updateClock() {
        const clockElement = document.querySelector('.current-time');
        if (!clockElement) return;

        const now = new Date();
        clockElement.textContent = now.toLocaleTimeString('es-CL', {
            hour: '2-digit',
            minute: '2-digit',
            second: '2-digit',
            hour12: false
        });
    }

    /**
     * ⭐ Formatear solo la hora (HH:MM)
     */
    function formatTime(date) {
        if (!date) return '';
        
        return new Date(date).toLocaleTimeString('es-CL', {
            hour: '2-digit',
            minute: '2-digit',
            hour12: false
        });
    }

    /**
     * ⭐ Formatear solo la fecha
     */
    function formatDate(date) {
        if (!date) return '';
        
        return new Date(date).toLocaleDateString('es-CL', {
            weekday: 'long',
            day: 'numeric',
            month: 'long',
            year: 'numeric'
        });
    }

    /**
     * Formatear fecha y hora completa (solo para casos especiales)
     */
    function formatDateTime(date) {
        if (!date) return '';
        
        return new Date(date).toLocaleString('es-CL', {
            weekday: 'long',
            day: 'numeric',
            month: 'long',
            year: 'numeric',
            hour: '2-digit',
            minute: '2-digit'
        });
    }

    /**
     * Escapar HTML
     */
    function escapeHtml(text) {
        if (!text) return '';
        const div = document.createElement('div');
        div.textContent = text;
        return div.innerHTML;
    }

    /**
     * Mostrar estado de carga
     */
    function showLoadingState() {
        const calendar = document.getElementById('calendar');
        if (calendar) {
            calendar.style.opacity = '0.6';
            calendar.style.pointerEvents = 'none';
        }
    }

    /**
     * Ocultar estado de carga
     */
    function hideLoadingState() {
        const calendar = document.getElementById('calendar');
        if (calendar) {
            calendar.style.opacity = '1';
            calendar.style.pointerEvents = 'auto';
        }
    }

    /**
     * Mostrar notificación profesional
     */
    function showNotification(message, type = 'info') {
        const notification = document.createElement('div');
        notification.className = `app-notification notification-${type}`;
        notification.textContent = message;
        
        const colors = {
            success: '#10B981',
            error: '#EF4444',
            info: '#2563EB',
            warning: '#F59E0B'
        };

        notification.style.cssText = `
            position: fixed;
            top: 2rem;
            right: 2rem;
            padding: 1rem 1.5rem;
            background: ${colors[type] || colors.info};
            color: white;
            border-radius: 0.5rem;
            box-shadow: 0 10px 15px -3px rgba(0, 0, 0, 0.1);
            z-index: 10000;
            font-weight: 600;
            font-size: 0.875rem;
            animation: slideInRight 0.3s ease-out;
            max-width: 400px;
        `;

        document.body.appendChild(notification);

        setTimeout(() => {
            notification.style.animation = 'slideOutRight 0.3s ease-out';
            setTimeout(() => notification.remove(), 300);
        }, 4000);
    }

    // ==================== ESTILOS DINÁMICOS ====================
    
    const style = document.createElement('style');
    style.textContent = `
        @keyframes slideInRight {
            from {
                transform: translateX(100%);
                opacity: 0;
            }
            to {
                transform: translateX(0);
                opacity: 1;
            }
        }
        
        @keyframes slideOutRight {
            from {
                transform: translateX(0);
                opacity: 1;
            }
            to {
                transform: translateX(100%);
                opacity: 0;
            }
        }

        /* Estilos para items de eventos en la lista lateral */
        .event-item-clase {
            border-left-width: 4px !important;
            transition: all 0.2s ease;
        }

        .event-item-actividad {
            border-left-width: 4px !important;
            transition: all 0.2s ease;
        }

        .event-item-tarea {
            border-left-width: 4px !important;
            transition: all 0.2s ease;
        }

        .event-item:hover {
            transform: translateX(4px) !important;
            box-shadow: 0 4px 6px -1px rgba(0, 0, 0, 0.1) !important;
        }

        /* ⭐ Estilos mejorados para mostrar hora en eventos del calendario */
        .fc-event-time {
            font-weight: 700;
            font-size: 0.75rem;
            margin-bottom: 2px;
        }

        .fc-event-title {
            font-size: 0.8rem;
            line-height: 1.2;
        }

        .fc-daygrid-event {
            padding: 3px 5px !important;
        }
    `;
    document.head.appendChild(style);
});