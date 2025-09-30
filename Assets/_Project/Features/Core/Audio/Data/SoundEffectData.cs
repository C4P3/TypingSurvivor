
using System.Collections.Generic;
using UnityEngine;

namespace TypingSurvivor.Features.Core.Audio.Data
{
    [CreateAssetMenu(fileName = "NewSoundEffect", menuName = "Typing Survivor/Audio/Sound Effect Data")]
    public class SoundEffectData : ScriptableObject
    {
        [Header("Sound Clips")]
        public List<AudioClip> Clips;

        [Header("Sound Settings")]
        [Range(0.0f, 1.0f)]
        public float Volume = 1.0f;

        public bool RandomizePitch = false;

        [Range(0.1f, 3.0f)]
        public float MinPitch = 0.9f;

        [Range(0.1f, 3.0f)]
        public float MaxPitch = 1.1f;
    }
}
