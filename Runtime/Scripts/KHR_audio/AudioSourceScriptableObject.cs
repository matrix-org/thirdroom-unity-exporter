using System;
using UnityEngine;

namespace ThirdRoom
{
    [CreateAssetMenu(fileName = "AudioSource", menuName = "UnityGLTF/AudioSource", order = 1)]
    public class AudioSourceScriptableObject : ScriptableObject
    {
        public AudioClip clip;
        public float gain = 1.0f;
        public bool autoPlay = true;
        public bool loop = true;
    }
}

