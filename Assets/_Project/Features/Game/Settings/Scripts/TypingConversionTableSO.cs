using UnityEngine;
using Newtonsoft.Json;

namespace TypingSurvivor.Features.Game.Typing
{
    [CreateAssetMenu(fileName = "TypingConversionTable", menuName = "Typing/Conversion Table")]
    public class TypingConversionTableSO : ScriptableObject
    {
        [SerializeField] private TextAsset _conversionTableJson;
        
        public TypingConversionTable Table { get; private set; }

        private void OnEnable()
        {
            if (_conversionTableJson != null)
            {
                try
                {
                    var settings = new JsonSerializerSettings
                    {
                        NullValueHandling = NullValueHandling.Ignore,
                        MissingMemberHandling = MissingMemberHandling.Ignore
                    };
                    Table = JsonConvert.DeserializeObject<TypingConversionTable>(_conversionTableJson.text, settings);
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"[TypingConversionTableSO] Failed to parse conversion table JSON: {e.Message}");
                    Table = null;
                }
            }
            else
            {
                Debug.LogError("[TypingConversionTableSO] Conversion table JSON is not assigned.");
                Table = null;
            }
        }
    }
}
