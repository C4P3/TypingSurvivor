using System.Threading.Tasks;
using UnityEngine;
using Unity.Services.Authentication;
using Unity.Services.Core;

namespace TypingSurvivor.Features.Core.Auth
{
    /// <summary>
    /// Implements the IAuthenticationService using Unity Gaming Services.
    /// </summary
    public class ClientAuthenticationService : IAuthenticationService
    {
        public bool IsSignedIn => AuthenticationService.Instance.IsSignedIn;

        public async Task<bool> SignInAnonymouslyAsync()
        {
            if (IsSignedIn)
            {
                Debug.Log("User is already signed in.");
                return true;
            }

            Debug.Log("Attempting to sign in anonymously with UGS...");

            try
            {
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
                Debug.Log($"Signed in anonymously successfully. PlayerID: {AuthenticationService.Instance.PlayerId}");
                return true;
            }
            catch (AuthenticationException ex)
            {
                // Compare error code to AuthenticationErrorCodes
                // Notify the player with the proper error message
                Debug.LogError($"Sign-in failed: {ex.Message}");
                return false;
            }
            catch (RequestFailedException ex)
            {
                // Compare error code to CommonErrorCodes
                // Notify the player with the proper error message
                Debug.LogError($"Sign-in failed: {ex.Message}");
                return false;
            }
        }
    }
}
