using System.Collections.Generic;
using Newtonsoft.Json;

namespace TypingSurvivor.Features.Game.Typing
{
    /// <summary>
    /// convertTable.jsonの構造に対応するC#クラス。
    /// JSONデシリアライザによって使用される。
    /// </summary>
    [System.Serializable]
    public class TypingConversionTable
    {
        [JsonProperty("definitions")]
        public Dictionary<string, List<string>> Definitions { get; set; }

        [JsonProperty("rules")]
        public Rules Rules { get; set; }
    }

    [System.Serializable]
    public class Rules
    {
        [JsonProperty("sokuon")]
        public SokuonRule Sokuon { get; set; }

        [JsonProperty("hatsuon")]
        public HatsuonRule Hatsuon { get; set; }
    }

    [System.Serializable]
    public class SokuonRule
    {
        [JsonProperty("char")]
        public string Character { get; set; }

        [JsonProperty("default")]
        public string Default { get; set; }

        [JsonProperty("consonants")]
        public Dictionary<string, string> Consonants { get; set; }
    }

    [System.Serializable]
    public class HatsuonRule
    {
        [JsonProperty("char")]
        public string Character { get; set; }

        [JsonProperty("default")]
        public string Default { get; set; }

        [JsonProperty("double_n_if_next_is")]
        public List<string> DoubleNIfNextIs { get; set; }
        
        [JsonProperty("double_n_exceptions")]
        public List<string> DoubleNExceptions { get; set; }
    }
}
