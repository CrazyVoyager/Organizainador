/**
 * @module Clima
 * @description Módulo para cargar y mostrar información del clima de Los Ángeles, Chile
 */

document.addEventListener('DOMContentLoaded', () => {
    cargarClimaLosAngeles();
    configurarBotonToggle();
});

/**
 * Carga los datos del clima desde la API de Open-Meteo
 * @async
 * @returns {Promise<void>}
 */
async function cargarClimaLosAngeles() {
    const lat = -37.4697;
    const lon = -72.3537;
    const url = `https://api.open-meteo.com/v1/forecast?latitude=${lat}&longitude=${lon}&current_weather=true`;

    try {
        const response = await fetch(url);
        const data = await response.json();
        const clima = data.current_weather;

        // Actualizar textos
        const tempElem = document.getElementById('weather-temp');
        if (tempElem) tempElem.innerText = `${Math.round(clima.temperature)}°C`;

        // Lógica de Iconos
        const codigo = clima.weathercode;
        let icono = "fa-sun";
        let desc = "Despejado";
        let color = "#f6c23e";

        if (codigo >= 1 && codigo <= 3) {
            icono = "fa-cloud-sun"; desc = "Nublado"; color = "#858796";
        } else if (codigo >= 45 && codigo <= 48) {
            icono = "fa-smog"; desc = "Niebla"; color = "#858796";
        } else if (codigo >= 51 && codigo <= 67) {
            icono = "fa-cloud-rain"; desc = "Lluvia"; color = "#4e73df";
        } else if (codigo >= 71) {
            icono = "fa-snowflake"; desc = "Nieve"; color = "#36b9cc";
        } else if (codigo >= 95) {
            icono = "fa-bolt"; desc = "Tormenta"; color = "#e74a3b";
        }

        // Actualizar visuales de la tarjeta
        const iconElem = document.getElementById('weather-icon');
        const descElem = document.getElementById('weather-desc');

        if (iconElem) {
            iconElem.className = `fas ${icono} fa-2x`;
            iconElem.style.color = color;
        }
        if (descElem) descElem.innerText = desc;

    } catch (error) {
        console.error("Error cargando clima:", error);
        const descElem = document.getElementById('weather-desc');
        if (descElem) descElem.innerText = "Sin conexión";
    }
}

/**
 * Configura el botón de toggle para mostrar/ocultar el widget del clima
 */
function configurarBotonToggle() {
    const btn = document.getElementById('weather-toggle');
    const card = document.getElementById('weather-card');
    const icon = document.getElementById('toggle-icon');

    if (!btn || !card) return;

    btn.addEventListener('click', () => {
        // Alternar la visibilidad usando la clase d-none de Bootstrap
        if (card.classList.contains('d-none')) {
            // ABRIR
            card.classList.remove('d-none');
            // Cambiar icono a una flecha hacia abajo (para cerrar)
            icon.className = "fas fa-chevron-down fs-4";
        } else {
            // CERRAR
            card.classList.add('d-none');
            // Volver al icono del clima o una nube
            icon.className = "fas fa-cloud-sun fs-4";
        }
    });
}