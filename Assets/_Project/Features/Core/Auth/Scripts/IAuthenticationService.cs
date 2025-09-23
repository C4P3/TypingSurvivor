using System.Threading.Tasks;

namespace TypingSurvivor.Features.Core.Auth
{
    /// <summary>
    /// Defines the contract for an authentication service.
    /// </summary>
    public interface IAuthenticationService
    {
        /// <summary>
        /// Gets a value indicating whether the user is currently signed in.
        /// </summary>
        bool IsSignedIn { get; }

        /// <summary>
        /// Signs in the user anonymously.
        /// </summary>
        /// <returns>A task that represents the asynchronous sign-in operation. The task result contains true if the sign-in was successful, and false otherwise.</returns>
        Task<bool> SignInAnonymouslyAsync();
    }
}
