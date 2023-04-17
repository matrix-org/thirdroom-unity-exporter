#if UNITY_EDITOR

using System;
using System.IO;
using System.Collections.Generic;
using GLTF.Schema;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;
using UnityGLTF;
using ThirdRoom.Exporter;

namespace ThirdRoom.Exporter
{

  public class AudioExtensionConfig : ScriptableObject
  {

    static List<AudioClip> audioDataClips = new List<AudioClip>();
    static List<AudioSourceScriptableObject> audioSourceObjects = new List<AudioSourceScriptableObject>();
    static List<KHR_AudioEmitter> audioEmitters = new List<KHR_AudioEmitter>();

    [InitializeOnLoadMethod]
    static void InitExt()
    {
      GLTFSceneExporter.AfterNodeExport += OnAfterNodeExport;
      GLTFSceneExporter.AfterSceneExport += OnAfterSceneExport;
    }

    private static void OnAfterNodeExport(GLTFSceneExporter exporter, GLTFRoot gltfRoot, Transform transform, Node node)
    {
      var audioEmitterBehavior = transform.GetComponent<KHRPositionalAudioEmitterBehavior>();

      if (audioEmitterBehavior != null) {

        var audioSourceIds = AddAudioSources(gltfRoot, audioEmitterBehavior.sources);

        var emitterId = new AudioEmitterId() {
          Id = audioEmitters.Count,
          Root = gltfRoot
        };

        var emitter = new KHR_PositionalAudioEmitter() {
          type = "positional",
          sources = audioSourceIds,
          gain = audioEmitterBehavior.gain,
          coneInnerAngle = audioEmitterBehavior.coneInnerAngle * Mathf.Deg2Rad,
          coneOuterAngle = audioEmitterBehavior.coneOuterAngle * Mathf.Deg2Rad,
          coneOuterGain = audioEmitterBehavior.coneOuterGain,
          distanceModel = audioEmitterBehavior.distanceModel,
          refDistance = audioEmitterBehavior.refDistance,
          maxDistance = audioEmitterBehavior.maxDistance,
          rolloffFactor = audioEmitterBehavior.rolloffFactor
        };

        audioEmitters.Add(emitter);

        var extension = new KHR_NodeAudioEmitterRef() {
          emitter = emitterId
        };

        node.AddExtension(KHR_audio.ExtensionName, extension);
        exporter.DeclareExtensionUsage(KHR_audio.ExtensionName, false);
      }
    }

    private static void OnAfterSceneExport(GLTFSceneExporter exporter, GLTFRoot gltfRoot)
    {
      var globalEmitterBehaviors = UnityEngine.Object.FindObjectsOfType<KHRGlobalAudioEmitterBehavior>();

      if (globalEmitterBehaviors.Length > 0) {
        var globalEmitterIds = new List<AudioEmitterId>();

        foreach (var emitterBehavior in globalEmitterBehaviors)
        {
          var audioSourceIds = AddAudioSources(gltfRoot, emitterBehavior.sources);

          var emitterId = new AudioEmitterId() {
            Id = audioEmitters.Count,
            Root = gltfRoot
          };

          globalEmitterIds.Add(emitterId);

          var globalEmitter = new KHR_AudioEmitter() {
            type = "global",
            sources = audioSourceIds,
            gain = emitterBehavior.gain
          };

          audioEmitters.Add(globalEmitter);
        }

        var extension = new KHR_SceneAudioEmittersRef() {
          emitters = globalEmitterIds
        };

        var scene = gltfRoot.Scenes[gltfRoot.Scene.Id];

        scene.AddExtension(KHR_audio.ExtensionName, extension);
        exporter.DeclareExtensionUsage(KHR_audio.ExtensionName, false);
      }
    
      if (audioEmitters.Count > 0) {
        var audioData = new List<KHR_AudioData>();

        for (int i = 0; i < audioDataClips.Count; i++) {
          var audioClip = audioDataClips[i];

          var path = AssetDatabase.GetAssetPath(audioClip.GetInstanceID());

          var fileExtension = Path.GetExtension(path);

          if (fileExtension != ".mp3") {
            audioDataClips.Clear();
            audioSourceObjects.Clear();
            audioEmitters.Clear();
            throw new Exception("Unsupported audio file type \"" + fileExtension + "\", only .mp3 is supported.");
          }

          var fileName = Path.GetFileName(path);
          var fileStream = new FileStream(path, FileMode.Open, FileAccess.Read);
          var result = exporter.ExportFile(fileName, "audio/mpeg", fileStream);
          var audio = new KHR_AudioData() {
            uri = result.uri,
            mimeType = result.mimeType,
            bufferView = result.bufferView,
          };

          audioData.Add(audio);
        }

        var audioSources = new List<KHR_AudioSource>();

        for (int i = 0; i < audioSourceObjects.Count; i++) {
          var audioSourceObject = audioSourceObjects[i];
          var audioDataIndex = audioDataClips.IndexOf(audioSourceObject.clip);

          var audioSource = new KHR_AudioSource() {
            audio = audioDataIndex == -1 ? null : new AudioDataId() { Id = audioDataIndex, Root = gltfRoot },
            autoPlay = audioSourceObject.autoPlay,
            loop = audioSourceObject.loop,
            gain = audioSourceObject.gain,
          };

          audioSources.Add(audioSource);
        }

        var extension = new KHR_audio() {
          audio = new List<KHR_AudioData>(audioData),
          sources = new List<KHR_AudioSource>(audioSources),
          emitters = new List<KHR_AudioEmitter>(audioEmitters),
        };

        gltfRoot.AddExtension(KHR_audio.ExtensionName, extension);
      }

      audioDataClips.Clear();
      audioSourceObjects.Clear();
      audioEmitters.Clear();
    }

    private static List<AudioSourceId> AddAudioSources(GLTFRoot gltfRoot, List<AudioSourceScriptableObject> sources) {
      var audioSourceIds = new List<AudioSourceId>();

      foreach (var audioSource in sources) {
        var audioSourceIndex = audioSourceObjects.IndexOf(audioSource);

        if (audioSourceIndex == -1) {
          audioSourceIndex = audioSourceObjects.Count;
          audioSourceObjects.Add(audioSource);
        }

        if (!audioDataClips.Contains(audioSource.clip)) {
          audioDataClips.Add(audioSource.clip);
        }

        var sourceId = new AudioSourceId() {
          Id = audioSourceIndex,
          Root = gltfRoot
        };

        audioSourceIds.Add(sourceId);
      }

      return audioSourceIds;
    }
  }
}

namespace GLTF.Schema
{
  [Serializable]
  public class AudioEmitterId : GLTFId<KHR_AudioEmitter> {
    public AudioEmitterId()
    {
    }

    public AudioEmitterId(AudioEmitterId id, GLTFRoot newRoot) : base(id, newRoot)
    {
    }

    public override KHR_AudioEmitter Value
    {
      get
      {
        if (Root.Extensions.TryGetValue(KHR_audio.ExtensionName, out IExtension iextension))
        {
          KHR_audio extension = iextension as KHR_audio;
          return extension.emitters[Id];
        }
        else
        {
          throw new Exception("KHR_audio not found on root object");
        }
      }
    }
  }

  [Serializable]
  public class AudioSourceId : GLTFId<KHR_AudioSource> {
    public AudioSourceId()
    {
    }

    public AudioSourceId(AudioSourceId id, GLTFRoot newRoot) : base(id, newRoot)
    {
    }

    public override KHR_AudioSource Value
    {
      get
      {
        if (Root.Extensions.TryGetValue(KHR_audio.ExtensionName, out IExtension iextension))
        {
          KHR_audio extension = iextension as KHR_audio;
          return extension.sources[Id];
        }
        else
        {
          throw new Exception("KHR_audio not found on root object");
        }
      }
    }
  }

  [Serializable]
  public class AudioDataId : GLTFId<KHR_AudioData> {
    public AudioDataId()
    {
    }

    public AudioDataId(AudioDataId id, GLTFRoot newRoot) : base(id, newRoot)
    {
    }

    public override KHR_AudioData Value
    {
      get
      {
        if (Root.Extensions.TryGetValue(KHR_audio.ExtensionName, out IExtension iextension))
        {
          KHR_audio extension = iextension as KHR_audio;
          return extension.audio[Id];
        }
        else
        {
          throw new Exception("KHR_audio not found on root object");
        }
      }
    }
  }

  [Serializable]
  public class KHR_SceneAudioEmittersRef : IExtension {
    public List<AudioEmitterId> emitters;

    public JProperty Serialize() {
      var jo = new JObject();
      JProperty jProperty = new JProperty(KHR_audio.ExtensionName, jo);  

      JArray arr = new JArray();

      foreach (var emitter in emitters) {
        arr.Add(emitter.Id);
      }

      jo.Add(new JProperty(nameof(emitters), arr));

      return jProperty;
    }

    public IExtension Clone(GLTFRoot root)
    {
      return new KHR_SceneAudioEmittersRef() { emitters = emitters };
    }
  }

  [Serializable]
  public class KHR_NodeAudioEmitterRef : IExtension {
    public AudioEmitterId emitter;

    public JProperty Serialize() {
      var jo = new JObject();
      JProperty jProperty = new JProperty(KHR_audio.ExtensionName, jo);      
      jo.Add(new JProperty(nameof(emitter), emitter.Id));
      return jProperty;
    }

    public IExtension Clone(GLTFRoot root)
    {
      return new KHR_NodeAudioEmitterRef() { emitter = emitter };
    }
  }

  [Serializable]
  public class KHR_AudioEmitter : GLTFChildOfRootProperty {

    public string type;
    public float gain;
    public List<AudioSourceId> sources;

    public virtual JObject Serialize() {
      var jo = new JObject();

      jo.Add(nameof(type), type);

      if (gain != 1.0f) {
        jo.Add(nameof(gain), gain);
      }

      if (sources != null && sources.Count > 0) {
        JArray arr = new JArray();

        foreach (var source in sources) {
          arr.Add(source.Id);
        }

        jo.Add(new JProperty(nameof(sources), arr));
      }

      return jo;
    }
  }

  [Serializable]
  public class KHR_PositionalAudioEmitter : KHR_AudioEmitter {

    public float coneInnerAngle;
    public float coneOuterAngle;
    public float coneOuterGain;
    public PositionalAudioDistanceModel distanceModel;
    public float maxDistance;
    public float refDistance;
    public float rolloffFactor;

    public override JObject Serialize() {
      var jo = base.Serialize();

      var positional = new JObject();

      if (!Mathf.Approximately(coneInnerAngle, Mathf.PI * 2)) {
        positional.Add(new JProperty(nameof(coneInnerAngle), coneInnerAngle));
      }
      
      if (!Mathf.Approximately(coneInnerAngle, Mathf.PI * 2)) {
        positional.Add(new JProperty(nameof(coneOuterAngle), coneOuterAngle));
      }

      if (coneOuterGain != 0.0f) {
        positional.Add(new JProperty(nameof(coneOuterGain), coneOuterGain));
      }
      
      if (distanceModel != PositionalAudioDistanceModel.inverse) {
        positional.Add(new JProperty(nameof(distanceModel), distanceModel.ToString()));
      }
      
      if (maxDistance != 10000.0f) {
        positional.Add(new JProperty(nameof(maxDistance), maxDistance));
      }
      
      if (refDistance != 1.0f) {
        positional.Add(new JProperty(nameof(refDistance), refDistance));
      }
      
      if (rolloffFactor != 1.0f) {
        positional.Add(new JProperty(nameof(rolloffFactor), rolloffFactor));
      }

      jo.Add("positional", positional);

      return jo;
    }
  }

  [Serializable]
  public class KHR_AudioSource : GLTFChildOfRootProperty {

    public bool autoPlay;
    public float gain;
    public bool loop;
    public AudioDataId audio;

    public JObject Serialize() {
      var jo = new JObject();

      if (autoPlay) {
        jo.Add(nameof(autoPlay), autoPlay);
      }

      if (gain != 1.0f) {
        jo.Add(nameof(gain), gain);
      }
      
      if (loop) {
        jo.Add(nameof(loop), loop);
      }
      
      if (audio != null) {
        jo.Add(nameof(audio), audio.Id);  
      }

      return jo;
    }
  }

  [Serializable]
  public class KHR_AudioData : GLTFChildOfRootProperty {

    public string uri;
    public string mimeType;
    public BufferViewId bufferView;

    public JObject Serialize() {
      var jo = new JObject();

      if (uri != null) {
        jo.Add(nameof(uri), uri);
      } else {
        jo.Add(nameof(mimeType), mimeType);
        jo.Add(nameof(bufferView), bufferView.Id);
      }

      return jo;
    }
  }

  [Serializable]
  public class KHR_audio : IExtension
  {
    public const string ExtensionName = "KHR_audio";

    public List<KHR_AudioData> audio;
    public List<KHR_AudioSource> sources;
    public List<KHR_AudioEmitter> emitters;

    public JProperty Serialize()
    {
      var jo = new JObject();
      JProperty jProperty = new JProperty(ExtensionName, jo);

      if (audio != null && audio.Count > 0) {
        JArray audioArr = new JArray();

        foreach (var audioData in audio) {
          audioArr.Add(audioData.Serialize());
        }

        jo.Add(new JProperty(nameof(audio), audioArr));
      }
    

      if (sources != null && sources.Count > 0) {
        JArray sourceArr = new JArray();

        foreach (var source in sources) {
          sourceArr.Add(source.Serialize());
        }

        jo.Add(new JProperty(nameof(sources), sourceArr));
      }

      if (emitters != null && emitters.Count > 0) {
        JArray emitterArr = new JArray();

        foreach (var emitter in emitters) {
          emitterArr.Add(emitter.Serialize());
        }

        jo.Add(new JProperty(nameof(emitters), emitterArr));
      }

      return jProperty;
    }

    public IExtension Clone(GLTFRoot root)
    {
      return new KHR_audio() {
        audio = audio,
        sources = sources,
        emitters = emitters,
      };
    }
  }
}

#endif