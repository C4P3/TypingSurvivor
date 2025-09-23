using System.Threading.Tasks;
using UnityEngine;

namespace TypingSurvivor.Features.Core.Auth
{
    /// <summary>
    /// A mock implementation of the IAuthenticationService.
    /// Simulates an asynchronous sign-in process.
    /// </summary>
    public class AuthenticationService : IAuthenticationService
    {
        public bool IsSignedIn { get; private set; }

        public async Task<bool> SignInAnonymouslyAsync()
        {
            if (IsSignedIn)
            {
                Debug.Log("User is already signed in.");
                return true;
            }

            Debug.Log("Attempting to sign in anonymously...");

            // Simulate a network request delay
            await Task.Delay(2000);

            // In a real implementation, you would call Unity's AuthenticationService here.
            // For now, we'll just simulate a successful sign-in.
            IsSignedIn = true;
            Debug.Log("Signed in anonymously successfully.");
            
            return true;
        }
    }
}
