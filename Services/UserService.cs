using Dapper;
using Microsoft.Extensions.Logging;
using Npgsql;
using System.Data;

namespace Organizainador.Services
{
    /// <summary>
    /// DTO (Data Transfer Object) interno, utilizado solo para mapear el resultado 
    /// de la función auth_validate_user de PostgreSQL. Contiene los campos 
    /// esenciales para construir la identidad del usuario (Claims).
    /// </summary>
    public class LoginResultDto
    {
        /// <summary>
        /// ID del usuario convertido a texto. Mapea u.tus_id_usr::TEXT AS "UserId"
        /// </summary>
        public string UserId { get; set; } = string.Empty;
        
        /// <summary>
        /// Correo electrónico del usuario. Mapea u.tus_mail::TEXT AS "Email"
        /// </summary>
        public string Email { get; set; } = string.Empty;
        
        /// <summary>
        /// Rol del usuario en el sistema. Mapea u.tus_rol::TEXT AS "Role"
        /// </summary>
        public string Role { get; set; } = string.Empty;
    }

    /// <summary>
    /// Clase que maneja la lógica de autenticación utilizando Dapper para llamar al Stored Procedure.
    /// </summary>
    public class UserService
    {
        private readonly string _connectionString;
        private readonly ILogger<UserService> _logger;

        public UserService(string connectionString, ILogger<UserService> logger)
        {
            _connectionString = connectionString;
            _logger = logger;
        }

        /// <summary>
        /// Llama al Stored Procedure de PostgreSQL para validar credenciales y obtener datos del usuario.
        /// </summary>
        /// <param name="email">Correo electrónico del usuario a validar.</param>
        /// <param name="contrasena">Contraseña del usuario a validar.</param>
        /// <returns>El objeto LoginResultDto si la autenticación es exitosa, o null si falla.</returns>
        public async Task<LoginResultDto?> ValidateCredentialsAsync(string email, string contrasena)
        {
            const string spName = "auth_validate_user";

            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            try
            {
                var parameters = new DynamicParameters();
                // Los nombres de los parámetros deben coincidir con la definición en el SP
                parameters.Add("_email", email, DbType.String);
                parameters.Add("_password", contrasena, DbType.String);

                // Ejecutar la función y mapear el resultado directamente a LoginResultDto
                // El SP debe devolver columnas llamadas "UserId", "Email" y "Role"
                var user = await connection.QueryFirstOrDefaultAsync<LoginResultDto>(
                    $"SELECT * FROM {spName}(@_email, @_password)",
                    parameters,
                    commandType: CommandType.Text);

                return user;
            }
            catch (Exception ex)
            {
                // Se registra el error y se devuelve null (falla de login)
                _logger.LogError(ex, "Error al validar credenciales con SP (Dapper) para email: {Email}", email);
                return null;
            }
        }
    }
}