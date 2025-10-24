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

        /// <summary>
        /// Gets the name of the current profile.
        /// </summary>
        string CurrentProfile { get; }

        /// <summary>
        /// Gets a dictionary of all available profiles, mapping profile ID to display name.
        /// </summary>
        /// <returns>A dictionary of profile data.</returns>
        System.Collections.Generic.IReadOnlyDictionary<string, string> GetProfileDisplayData();

        /// <summary>
        /// Switches to a different profile and signs in anonymously.
        /// </summary>
        /// <param name="profileName">The name of the profile to switch to.</param>
        /// <returns>True if the switch and sign-in were successful, false otherwise.</returns>
        Task<bool> SwitchProfileAndSignInAsync(string profileName);

        /// <summary>
        /// Updates the display name of the currently signed-in player.
        /// </summary>
        /// <param name="newName">The new name for the player.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        Task UpdatePlayerNameAsync(string newName);

        /// <summary>
        /// Signs out the current player.
        /// </summary>
        void SignOut();

        /// <summary>
        /// Signs in with the last used profile, or the default profile if none is found.
        /// </summary>
        /// <returns>True if sign-in was successful, false otherwise.</returns>
        Task<bool> SignInWithLastUsedProfileAsync();
    }
}
