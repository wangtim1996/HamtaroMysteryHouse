using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum EntityType{
    Material,
    ParticleSystem,
    Camera,
    AI,
    Music
}

public class EnvironmentEntity : MonoBehaviour
{


    public EntityType type;
    private bool added = false;
    // Start is called before the first frame update
    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {
        if(!added && Environment.instance){
            Environment.instance.AddEntity(type, gameObject);
            added = true;
        }
    }
}
