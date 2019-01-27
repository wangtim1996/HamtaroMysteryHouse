using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(AudioSource), typeof(Animator), typeof(CameraShaker))]
public class BasicAISounds : MonoBehaviour
{

    public AudioClip[] footsteps;
    private AudioSource audioSource;
    private CameraShaker shaker;

    void Start(){
        Animator anim = GetComponent<Animator>();
        for(int i = 0; i < anim.runtimeAnimatorController.animationClips.Length; ++i){
            Debug.Log(anim.runtimeAnimatorController.animationClips[i].name + ", " + i);
        }

        audioSource = GetComponent<AudioSource>();
        shaker = GetComponent<CameraShaker>();

        AddEvent(1, 0.08f, "Footstep", 0);
        AddEvent(1, 0.6f, "Footstep", 0);
    }

    public void Footstep(){
        var rand = Random.Range(0, footsteps.Length);
        audioSource.PlayOneShot(footsteps[rand], 1.0f);

        float dist = Vector3.Distance(Camera.main.transform.position, transform.position);
        float magnitude = 0.1f / Mathf.Max(1.0f, dist);
        shaker.ShakeCamera(0.5f, magnitude, 0);
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
