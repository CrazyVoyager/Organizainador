/**
 * Login Handler para enviar el formulario de inicio de sesión
 * de forma asíncrona (AJAX) a la Page Handler 'Auth'.
 */

// 1. LISTENER DE INICIALIZACIÓN: Espera a que el DOM esté listo
document.addEventListener('DOMContentLoaded', function () {
    const loginForm = document.getElementById('loginForm');

    if (loginForm) {
        // Enlaza el evento 'submit' del formulario a la función handleLoginSubmit
        loginForm.addEventListener('submit', handleLoginSubmit);
    } else {
        // Muestra un error en consola si el formulario no se encuentra (solo para depuración)
        console.error('ERROR: El formulario con ID "loginForm" no fue encontrado en la página.');
    }
});


// 2. FUNCIÓN PRINCIPAL DE ENVÍO ASÍNCRONO
async function handleLoginSubmit(e) {
    // CLAVE: Detiene el envío tradicional del formulario (previene la recarga de la página)
    e.preventDefault();

    // 🔑 OBTENER EL TOKEN ANTI-FORGERY
    const antiForgeryToken = document.querySelector('input[name="__RequestVerificationToken"]')?.value;

    // Elementos DOM para interacción
    const emailInput = document.getElementById('Email');
    const passwordInput = document.getElementById('Contrasena');
    const errorDisplay = document.getElementById('errorDisplay');

    // Obtener datos
    const email = emailInput.value;
    const password = passwordInput.value;

    // Ocultar errores anteriores
    errorDisplay.textContent = '';
    errorDisplay.classList.add('d-none');

    // Simple validación del lado del cliente
    if (!email || !password) {
        errorDisplay.textContent = 'Por favor, ingrese el correo electrónico y la contraseña.';
        errorDisplay.classList.remove('d-none');
        return;
    }

    // Validar que el token exista antes de enviarlo
    if (!antiForgeryToken) {
        errorDisplay.textContent = 'Error de seguridad: Token Anti-Forgery no encontrado.';
        errorDisplay.classList.remove('d-none');
        console.error('Fallo al encontrar el token anti-falsificación.');
        return;
    }


    try {
        // 🔑 MODIFICACIÓN CRUCIAL: JSON PLANO
        // Al usar [FromBody] en C#, el JSON no debe estar envuelto en 'Input' o 'data'.
        // Debe ser directamente el objeto que coincide con LoginInputModel.
        const payload = {
            Email: email,
            Contrasena: password
        };

        // Enviar datos al Handler del backend usando fetch (dirigido al método OnPostAuth)
        const response = await fetch('?handler=Auth', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                // ENVIAR EL TOKEN EN EL HEADER
                'RequestVerificationToken': antiForgeryToken
            },
            body: JSON.stringify(payload)
        });

        // Procesar la respuesta
        if (response.ok) {
            // Login exitoso, redirigir
            window.location.href = '/PaginaPrincipal';
        } else if (response.status === 401) {
            // 401 Unauthorized: Credenciales incorrectas
            errorDisplay.textContent = 'Usuario o contraseña incorrectos.';
            errorDisplay.classList.remove('d-none');
        } else if (response.status === 400) {
            // 400 Bad Request (Puede ser Anti-Forgery o JSON). Ahora que el JSON es plano,
            // el problema es casi seguro el Anti-Forgery Token.
            errorDisplay.textContent = 'Error de solicitud. Revise la seguridad del formulario (Token).';
            errorDisplay.classList.remove('d-none');
        } else {
            // Manejar otros errores (ej. 500 Server Error)
            console.error(`Error de servidor. Código: ${response.status}`);
            errorDisplay.textContent = `Ocurrió un error inesperado al intentar iniciar sesión (Código: ${response.status}).`;
            errorDisplay.classList.remove('d-none');
        }
    } catch (error) {
        console.error('Error de red o excepción inesperada:', error);
        errorDisplay.textContent = 'No se pudo conectar con el servidor. Revise su conexión.';
        errorDisplay.classList.remove('d-none');
    }
}