using System.Threading.Tasks;
using UnityEngine;
using Unity.Services.Authentication;
using Unity.Services.Core;
using System.Collections.Generic;
using System.Linq;

namespace TypingSurvivor.Features.Core.Auth
{
    /// <summary>
    /// Implements the IAuthenticationService using Unity Gaming Services
    /// and local PlayerPrefs for profile management.
    /// </summary>
    public class ClientAuthenticationService : IAuthenticationService
    {
        public bool IsSignedIn => AuthenticationService.Instance.IsSignedIn;
        public string CurrentProfile => AuthenticationService.Instance.Profile;

        private const string KnownProfilesKey = "TypingSurvivor.KnownProfiles";

        // Helper class for JSON serialization
        [System.Serializable]
        private class ProfileListWrapper
        {
            public List<string> profiles = new List<string>();
        }

        public IReadOnlyList<string> ListProfiles()
        {
            var json = PlayerPrefs.GetString(KnownProfilesKey, "{}");
            var wrapper = JsonUtility.FromJson<ProfileListWrapper>(json);
            return wrapper.profiles ?? new List<string>();
        }

        public async Task<bool> SwitchProfileAndSignInAsync(string profileName)
        {
            if (string.IsNullOrWhiteSpace(profileName))
            {
                Debug.LogError("Profile name cannot be empty.");
                return false;
            }

            // Must sign out before switching profiles
            if (IsSignedIn)
            {
                SignOut();
            }

            AuthenticationService.Instance.SwitchProfile(profileName);

            if (await SignInAnonymouslyAsync())
            {
                AddProfileToLocalCache(profileName);
                return true;
            }

            return false;
        }

        public void SignOut()
        {
            if (!IsSignedIn) return;
            
            Debug.Log($"Signing out profile: {CurrentProfile}");
            AuthenticationService.Instance.SignOut();
        }

        private void AddProfileToLocalCache(string profileName)
        {
            var profiles = ListProfiles().ToList();
            if (!profiles.Contains(profileName))
            {
                profiles.Add(profileName);
                
                var wrapper = new ProfileListWrapper { profiles = profiles };
                var json = JsonUtility.ToJson(wrapper);
                
                PlayerPrefs.SetString(KnownProfilesKey, json);
                PlayerPrefs.Save();
                Debug.Log($"Saved new profile '{profileName}' to local cache.");
            }
        }

        public async Task<bool> SignInAnonymouslyAsync()
        {
            if (IsSignedIn)
            {
                // If we switch profile, the IsSignedIn flag becomes false,
                // so this check is for avoiding re-login with the same profile.
                Debug.Log($"User is already signed in with profile: {CurrentProfile}.");
                return true;
            }

            Debug.Log($"Attempting to sign in anonymously with profile: {CurrentProfile}...");

            try
            {
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
                Debug.Log($"Signed in anonymously successfully. Profile: {CurrentProfile}, PlayerID: {AuthenticationService.Instance.PlayerId}");
                return true;
            }
            catch (AuthenticationException ex)
            {
                Debug.LogError($"Sign-in failed: {ex.Message}");
                return false;
            }
            catch (RequestFailedException ex)
            {
                Debug.LogError($"Sign-in failed: {ex.Message}");
                return false;
            }
        }

        public async Task UpdatePlayerNameAsync(string newName)
        {
            try
            {
                await AuthenticationService.Instance.UpdatePlayerNameAsync(newName);
                Debug.Log($"Player name updated to: {newName}");
            }
            catch (AuthenticationException ex)
            {
                Debug.LogError($"Failed to update player name: {ex.Message}");
            }
            catch (RequestFailedException ex)
            {
                Debug.LogError($"Failed to update player name: {ex.Message}");
            }
        }
    }
}