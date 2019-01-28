using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DigitalRuby.Tween;
using UnityEngine.PostProcessing;

public class Environment : MonoBehaviour
{

    public static Environment instance;
    // Start is called before the first frame update

    public float changeDuration = 1.0f;
    public PostProcessingProfile niceProfile;
    public PostProcessingProfile scaryProfile;


    private List<GameObject> materials;
    private List<GameObject> particles;
    private List<GameObject> cameras;
    private List<GameObject> ais;
    private List<GameObject> musics;

    private bool _nice = false;
    public bool nice {
        get{
            return _nice;
        }
        set{
            if(_nice && !value){
                _nice = value;
                SwitchToScary();
            }
            else if(!_nice && value){
                _nice = value;
                SwitchToNice();
            }
        }
    }

    public bool test = false;

    void Start()
    {
        materials = new List<GameObject>();
        particles = new List<GameObject>();
        cameras = new List<GameObject>();
        ais = new List<GameObject>();
        musics = new List<GameObject>();

        instance = this;
    }

    // Update is called once per frame
    void Update()
    {
        //nice = test;
    }

    private void SwitchToScary(){
        //Set model texture color
        foreach(GameObject material in materials){
            var skn = material.GetComponent<SkinnedMeshRenderer>();
            foreach(Material mat in skn.materials){
                TweenFactory.Tween(mat, Color.white, Color.black, changeDuration, TweenScaleFunctions.CubicEaseInOut, (color) => {
                    mat.SetColor("_Color", color.CurrentValue);
                });
            }
        }

        //Enable particle systems
        foreach(GameObject particle in particles){
            var par = particle.GetComponent<ParticleSystem>();
            var emission = par.emission;
            emission.enabled = true;
        }

        //Switch camera postprocessing
        foreach(GameObject camera in cameras){
            var cam = camera.GetComponent<PostProcessingBehaviour>();
            cam.profile = scaryProfile;
        }

        //Enable AI
        foreach(GameObject ai in ais){
            ai.GetComponent<BasicAISounds>().enabled = true;
            var bai = ai.GetComponent<BasicAI>();
            TweenFactory.Tween(ai, 0, 1, changeDuration, TweenScaleFunctions.CubicEaseInOut, (t) => {
                bai.speed = t.CurrentValue;
            });
        }

        //Swap music, 0 is scary and 1 is nice
        foreach(var music in musics){
            var sounds = music.GetComponents<AudioSource>();
            sounds[0].enabled = true; //enable scary bknd
            TweenFactory.Tween(sounds, 0, 1, changeDuration, TweenScaleFunctions.CubicEaseInOut, (t) => {
                //tween volumes
                sounds[1].volume = 0.5f * (1.0f - t.CurrentValue);
                sounds[0].volume = 0.5f * t.CurrentValue;
            }, (t) => {
                //disable nice bknd
                //sounds[1].enabled = false;
            });
        }
    }

    private void SwitchToNice(){
        //Set model texture color
        foreach(GameObject material in materials){
            var skn = material.GetComponent<SkinnedMeshRenderer>();
            foreach(Material mat in skn.materials){
                TweenFactory.Tween(mat, Color.black, Color.white, changeDuration, TweenScaleFunctions.CubicEaseInOut, (color) => {
                    mat.SetColor("_Color", color.CurrentValue);
                });
            }
        }

        //Disable particle systems
        foreach(GameObject particle in particles){
            var par = particle.GetComponent<ParticleSystem>();
            var emission = par.emission;
            emission.enabled = false;
        }

        //Switch camera postprocessing
        foreach(GameObject camera in cameras){
            var cam = camera.GetComponent<PostProcessingBehaviour>();
            cam.profile = niceProfile;
        }

        //Disable AI
        foreach(GameObject ai in ais){
            ai.GetComponent<BasicAISounds>().enabled = false;
            var bai = ai.GetComponent<BasicAI>();
            TweenFactory.Tween(ai, 1, 0, changeDuration, TweenScaleFunctions.CubicEaseInOut, (t) => {
                bai.speed = t.CurrentValue;
            });
        }

        //Swap music, 0 is scary and 1 is nice
        foreach(var music in musics){
            var sounds = music.GetComponents<AudioSource>();
            sounds[1].enabled = true; //enable nice bknd
            TweenFactory.Tween(sounds, 0, 1, changeDuration, TweenScaleFunctions.CubicEaseInOut, (t) => {
                //tween volumes
                sounds[0].volume = 0.5f * (1.0f - t.CurrentValue);
                sounds[1].volume = 0.5f * t.CurrentValue;
            }, (t) => {
                //disable scary bknd
                //sounds[0].enabled = false;
            });
        }
    }

    public int AddEntity(EntityType type, GameObject go){
        switch(type){
            case EntityType.Camera:
                cameras.Add(go);
                return cameras.Count-1;
                break;
            case EntityType.Material:
                materials.Add(go);
                return materials.Count-1;
                break;
            case EntityType.ParticleSystem:
                particles.Add(go);
                return particles.Count-1;
                break;
            case EntityType.AI:
                ais.Add(go);
                return ais.Count-1;
                break;
            case EntityType.Music:
                musics.Add(go);
                return musics.Count-1;
                break;
        }
        return 0;
    }

    public void RemoveEntity(EntityType type, GameObject go)
    {
        switch (type)
        {
            case EntityType.Camera:
                cameras.Remove(go);
                break;
            case EntityType.Material:
                materials.Remove(go);
                break;
            case EntityType.ParticleSystem:
                particles.Remove(go);
                break;
            case EntityType.AI:
                ais.Remove(go);
                break;
            case EntityType.Music:
                musics.Remove(go);
                break;
        }

    }
}
