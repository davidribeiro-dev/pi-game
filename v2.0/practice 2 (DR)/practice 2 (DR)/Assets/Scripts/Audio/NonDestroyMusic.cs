using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NonDestroyMusic : MonoBehaviour
{
    public static NonDestroyMusic musicPlayer;
    public GameObject music;

    private void Awake()
    {
        if (musicPlayer == null)
        {
            DontDestroyOnLoad(gameObject);
            musicPlayer = this;
        }
        else if (musicPlayer != this)
        {
            Destroy(gameObject);
        }
    }
}
