using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Organizainador.Controllers
{
    /// <summary>
    /// Base controller providing common functionality for authenticated controllers.
    /// </summary>
    public abstract class BaseController : Controller
    {
        /// <summary>
        /// Gets the current user's ID as a string from claims.
        /// </summary>
        /// <returns>The user ID string, or empty string if not found.</returns>
        protected string GetCurrentUserIdString()
        {
            return User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        }

        /// <summary>
        /// Gets the current user's ID as an integer from claims.
        /// </summary>
        /// <returns>The user ID integer, or 0 if not found or invalid.</returns>
        protected int GetCurrentUserIdInt()
        {
            return int.TryParse(GetCurrentUserIdString(), out int id) ? id : 0;
        }

        /// <summary>
        /// Sets a success message in TempData for display to the user.
        /// </summary>
        /// <param name="message">The success message to display.</param>
        protected void SetSuccessMessage(string message)
        {
            TempData["SuccessMessage"] = message;
        }

        /// <summary>
        /// Sets an error message in TempData for display to the user.
        /// </summary>
        /// <param name="message">The error message to display.</param>
        protected void SetErrorMessage(string message)
        {
            TempData["ErrorMessage"] = message;
        }
    }
}
