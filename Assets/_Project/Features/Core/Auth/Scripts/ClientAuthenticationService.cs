using System.Threading.Tasks;
using UnityEngine;
using Unity.Services.Authentication;
using Unity.Services.Core;
using System.Collections.Generic;
using System.Linq;

namespace TypingSurvivor.Features.Core.Auth
{
    // Custom serializable dictionary for Unity
        [System.Serializable]
        public class SerializableDictionary<TKey, TValue> : Dictionary<TKey, TValue>, ISerializationCallbackReceiver
        {
            [SerializeField]
            private List<TKey> keys = new List<TKey>();
    
            [SerializeField]
            private List<TValue> values = new List<TValue>();
    
            public void OnBeforeSerialize()
            {
                keys.Clear();
                values.Clear();
                foreach (KeyValuePair<TKey, TValue> pair in this)
                {
                    keys.Add(pair.Key);
                    values.Add(pair.Value);
                }
            }
    
            public void OnAfterDeserialize()
            {
                this.Clear();
    
                if (keys.Count != values.Count)
                    throw new System.Exception($"There are {keys.Count} keys and {values.Count} values after deserialization. Make sure that both key and value types are serializable.");
    
                for (int i = 0; i < keys.Count; i++)
                    this.Add(keys[i], values[i]);
            }
        }
    
        /// <summary>
        /// Implements the IAuthenticationService using Unity Gaming Services
        /// and local PlayerPrefs for profile management.
        /// </summary>
        public class ClientAuthenticationService : IAuthenticationService
        {
            public bool IsSignedIn => AuthenticationService.Instance.IsSignedIn;
            public string CurrentProfile => AuthenticationService.Instance.Profile;
    
            private const string KnownProfilesKey = "TypingSurvivor.KnownProfiles";
            private const string DisplayNamesKey = "TypingSurvivor.ProfileDisplayNames";
    
            // Helper class for JSON serialization
            [System.Serializable]
            private class ProfileListWrapper
            {
                public List<string> profiles = new List<string>();
            }
    
            [System.Serializable]
            private class DisplayNameDictWrapper
            {
                public SerializableDictionary<string, string> displayNames = new SerializableDictionary<string, string>();
            }
    
            private SerializableDictionary<string, string> GetDisplayNames()
            {
                var json = PlayerPrefs.GetString(DisplayNamesKey, "{}");
                var wrapper = JsonUtility.FromJson<DisplayNameDictWrapper>(json);
                return wrapper.displayNames ?? new SerializableDictionary<string, string>();
            }
    
            public void UpdateCachedDisplayName(string profileId, string newName)
            {
                if (string.IsNullOrEmpty(profileId) || string.IsNullOrEmpty(newName)) return;
    
                var displayNames = GetDisplayNames();
                displayNames[profileId] = newName;
    
                var wrapper = new DisplayNameDictWrapper { displayNames = displayNames };
                var json = JsonUtility.ToJson(wrapper);
                PlayerPrefs.SetString(DisplayNamesKey, json);
                PlayerPrefs.Save();
                Debug.Log($"Updated display name cache for profile '{profileId}' to '{newName}'.");
            }
    
            public IReadOnlyDictionary<string, string> GetProfileDisplayData()
            {
                var profileIds = ListProfileIds();
                var displayNames = GetDisplayNames();
                var result = new Dictionary<string, string>();
    
                foreach (var id in profileIds)
                {
                    if (displayNames.TryGetValue(id, out var displayName) && !string.IsNullOrEmpty(displayName))
                    {
                        result[id] = displayName;
                    }
                    else
                    {
                        result[id] = id; // Fallback to the profile ID itself
                    }
                }
                return result;
            }
    
            private IReadOnlyList<string> ListProfileIds()        {
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
                PlayerPrefs.SetString("LastUsedProfile", profileName);
                PlayerPrefs.Save();
                Debug.Log($"Set '{profileName}' as the last used profile.");
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
            var profiles = ListProfileIds().ToList();
            if (!profiles.Contains(profileName))
            {
                profiles.Add(profileName);
                
                var wrapper = new ProfileListWrapper { profiles = profiles };
                var json = JsonUtility.ToJson(wrapper);
                
                PlayerPrefs.SetString(KnownProfilesKey, json);
                // PlayerPrefs.Save() is called after setting LastUsedProfile
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

        

                public async Task<bool> SignInWithLastUsedProfileAsync()

                {

                    string lastProfile = PlayerPrefs.GetString("LastUsedProfile", null);

                    if (!string.IsNullOrEmpty(lastProfile))

                    {

                        Debug.Log($"Found last used profile: '{lastProfile}'. Attempting to sign in...");

                        return await SwitchProfileAndSignInAsync(lastProfile);

                    }

                    else

                    {

                        Debug.Log("No last used profile found. Signing in with default profile...");

                        return await SignInAnonymouslyAsync();

                    }

                }


            }

        }

        