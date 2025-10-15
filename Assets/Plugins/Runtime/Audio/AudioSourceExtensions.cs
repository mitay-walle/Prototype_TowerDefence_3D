using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public static class AudioSourceExtensions
{
    #if UNITY_EDITOR
    [MenuItem("CONTEXT/AudioSource/Ralistic Setup")]
    public static void RalisticRolloff(MenuCommand command)
    {
        Undo.RecordObject(command.context,"AudioSource/Ralistic Setup");
        ((AudioSource)command.context).RealisticRolloff();
        EditorUtility.SetDirty(command.context);
    }
    #endif

    public static void RealisticRolloff(this AudioSource AS)
    {
        var animCurve = new AnimationCurve(
            new Keyframe(AS.minDistance,1f),
            new Keyframe(AS.minDistance + (AS.maxDistance - AS.minDistance ) / 4f,.35f),
            new Keyframe(AS.maxDistance,0f)); 
        
        AS.rolloffMode = AudioRolloffMode.Custom;
        AS.dopplerLevel = 0f;
        AS.spread = 60f;
        animCurve.SmoothTangents(1,.025f);
        AS.SetCustomCurve(AudioSourceCurveType.CustomRolloff,animCurve);
    }
	
	public static AudioClip Random(this  AudioClip[] clips)
    {
        if (clips == null) return null;
        if (clips.Length == 0) return null;

        return clips[UnityEngine.Random.Range(0, clips.Length)];
    }
}