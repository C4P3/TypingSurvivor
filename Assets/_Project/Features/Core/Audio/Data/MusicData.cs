using UnityEngine;

namespace TypingSurvivor.Features.Core.Audio.Data
{
    [CreateAssetMenu(fileName = "NewMusic", menuName = "Typing Survivor/Audio/Music Data")]
    public class MusicData : ScriptableObject
    {
        public AudioClip Clip;

        [Range(0.0f, 1.0f)]
        public float Volume = 1.0f;

        public bool Loop = true;
    }
}
