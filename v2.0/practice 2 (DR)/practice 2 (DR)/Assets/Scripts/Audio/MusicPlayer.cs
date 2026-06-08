using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MusicPlayer : MonoBehaviour
{
    public GameObject objectMusic;
    private AudioSource audioSource;
    public Slider volumeSlider;

    float musicVolume = 0.2f;

    private void Start()
    {
        objectMusic = GameObject.FindWithTag("Music");
        audioSource = objectMusic.GetComponent<AudioSource>();

        musicVolume = PlayerPrefs.GetFloat("volume");
        audioSource.volume = musicVolume;
        volumeSlider.value = musicVolume;
    }

    void Update()
    {
        audioSource.volume = musicVolume;
        PlayerPrefs.SetFloat("volume", musicVolume);
    }

    public void VolumeUpdate(float volume)
    {
        musicVolume = volume;
    }
}
