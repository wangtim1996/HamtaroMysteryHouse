using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenu : MonoBehaviour
{
    private AsyncOperation loadGame;
    public GameObject loadingBar;
    public GameObject[] deactivateDuringLoad;

    public void StartGame()
    {
        loadGame = SceneManager.LoadSceneAsync("main");
        loadingBar.SetActive(true);
        foreach(var o in deactivateDuringLoad)
        {
            o.SetActive(false);
        }
    }

    public void QuitGame()
    {
        Application.Quit();
    }

    void Update()
    {
        if(loadGame != null)
        {
            var progress = loadGame.progress;
            loadingBar.GetComponent<Slider>().value = progress;
        }
    }
}
