using K_PathFinder;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ButtonControlerForNavmeh : MonoBehaviour {
    public GameObject area; //reference to Area Modifyer
    public GameObject blueThing; //reference to blue box (blue box only for graphics)

    Animator animator; //reference to animator
    CellPathContentPassControl buttonState; //reference to object added to navmesh
    
    void Start () {        
        animator = GetComponent<Animator>();//get animator
        buttonState = new CellPathContentPassControl(transform.position, false); //create instance of CellPathContentPassControl (this class below) with position and state of button
        area.GetComponent<AreaWorldMod>().AddCellPathContent(buttonState);//add it to AreaWorldMod in target gameObject
    }

    //then player collider with button trigget then it trigger animation and coroutine
    void OnTriggerEnter(Collider other) {
        if (other.gameObject.tag == "Player") {
            animator.SetTrigger("Press");
            StartCoroutine(PerformButtomPress());
        }
    }

    //coroutine after 0.5 second destroy blue box and change state of button
    IEnumerator PerformButtomPress() {
        yield return new WaitForSeconds(0.5f);
        Destroy(blueThing);
        buttonState.state = true;
    }
}

class CellPathContentPassControl : CellPathContentAbstract {
    public Vector3 position;//for button
    public bool state;//is button pressed
    
    public CellPathContentPassControl(Vector3 position, bool state) {
        this.position = position;
        this.state = state;
    }
}