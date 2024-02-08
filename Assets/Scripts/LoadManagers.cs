using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LoadManagers : MonoBehaviour
{
    private void Awake()
    {
        bool prodDoNotDestroyLoaded = false;
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            if (SceneManager.GetSceneAt(i).name == "PROD_DoNotDestroy")
            {
                prodDoNotDestroyLoaded = true;
                break;
            }
        }
        if (prodDoNotDestroyLoaded == false)
            SceneManager.LoadScene("PROD_DoNotDestroy", LoadSceneMode.Additive);
    }
}
