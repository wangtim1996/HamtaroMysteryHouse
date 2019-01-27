using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraShaker : MonoBehaviour
{
    private class Shake{
        public float duration;
        public float amplitude;
        public float currentTime;
        public float decay;
        
        public Shake(float d, float a, float c, float de){
            duration = d;
            amplitude = a;
            currentTime = c;
            decay = de;
        }
    }

    private List<Shake> shakes;
    private Vector3 originalPos;

    public Camera camera;

    // Start is called before the first frame update
    void Start()
    {
        shakes = new List<Shake>();
    }

    void OnEnable(){
        if(!camera){
            camera = Camera.main;
        }
        originalPos = camera.transform.localPosition;
    }

    // Update is called once per frame
    void Update()
    {
        float currAmplitude = 0;

        for(int i = 0; i < shakes.Count; ++i){
            Debug.Log(shakes[i].currentTime + ", " + shakes[i].amplitude + ", " + shakes[i].duration + ", " + shakes[i].decay);
            if(shakes[i].currentTime < shakes[i].duration){
                shakes[i].currentTime += Time.deltaTime;
                currAmplitude += shakes[i].amplitude;
                shakes[i].amplitude -= Time.deltaTime * shakes[i].decay;
            }
            else{
                shakes.RemoveAt(i);
                --i;
            }

        }


        camera.transform.localPosition = originalPos + Random.insideUnitSphere * currAmplitude;
    }

    public void ShakeCamera(float duration, float amplitude, float decay){
        Shake s = new Shake(duration, amplitude, 0, decay);
        shakes.Add(s);
    }
}
