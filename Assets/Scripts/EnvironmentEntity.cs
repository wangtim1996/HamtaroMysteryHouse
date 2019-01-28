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
    private int index = -1;
    // Start is called before the first frame update
    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {
        if (!added && Environment.instance) {
            index = Environment.instance.AddEntity(type, gameObject);
            added = true;
        }
    }

    void OnDestroy()
    {
        if(added && Environment.instance)
        {
            Environment.instance.RemoveEntity(type, gameObject);
        }
    }
}
