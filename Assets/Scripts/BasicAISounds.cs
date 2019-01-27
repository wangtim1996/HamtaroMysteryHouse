using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(AudioSource), typeof(Animator))]
public class BasicAISounds : MonoBehaviour
{

    public AudioClip footstep;
    private AudioSource audioSource;

    void Start(){
        Animator anim = GetComponent<Animator>();
        for(int i = 0; i < anim.runtimeAnimatorController.animationClips.Length; ++i){
            Debug.Log(anim.runtimeAnimatorController.animationClips[i].name + ", " + i);
        }

        audioSource = GetComponent<AudioSource>();

        AddEvent(3, 0.05f, "Footstep", 0);
        AddEvent(3, 0.6f, "Footstep", 0);
    }

    public void Footstep(){
        audioSource.PlayOneShot(footstep, 1.0f);
    }

    void AddEvent(int Clip, float time, string functionName, float floatParameter)
    {
        Animator anim = GetComponent<Animator>();
        AnimationEvent animationEvent = new AnimationEvent();
        animationEvent.functionName = functionName;
        animationEvent.floatParameter = floatParameter;
        animationEvent.time = time;
        AnimationClip clip = anim.runtimeAnimatorController.animationClips[Clip];
        clip.AddEvent(animationEvent);
    }
}
