using Dapper;
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
        // Mapea u.tus_id_usr::TEXT AS "UserId"
        public string UserId { get; set; } = string.Empty;
        // Mapea u.tus_mail::TEXT AS "Email"
        public string Email { get; set; } = string.Empty;
        // Mapea u.tus_rol::TEXT AS "Role"
        public string Role { get; set; } = string.Empty;
    }

    /// <summary>
    /// Clase que maneja la lógica de autenticación utilizando Dapper para llamar al Stored Procedure.
    /// </summary>
    public class UserService
    {
        private readonly string _connectionString;

        public UserService(string connectionString)
        {
            _connectionString = connectionString;
        }

        /// <summary>
        /// Llama al Stored Procedure de PostgreSQL para validar credenciales y obtener datos.
        /// </summary>
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
                Console.WriteLine($"Error al validar credenciales con SP (Dapper): {ex.Message}");
                return null;
            }
        }
    }
}