using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RumbleSound : MonoBehaviour
{
    AudioSource myRumbleSoundPlayer;
    WheelCustomController[] wheelCustomControllers;
    // Start is called before the first frame update
    void Awake()
    {
        myRumbleSoundPlayer = GetComponent<AudioSource>();
        wheelCustomControllers = FindObjectsOfType<WheelCustomController>();
    }

    // Update is called once per frame
    private void Update()
    {
        foreach (var wheelCustomController in wheelCustomControllers)
        {
            if (wheelCustomController.IsHittingRumble)
            {
                myRumbleSoundPlayer.Play();
                return;
            }
        }
        myRumbleSoundPlayer.Stop();
    }
}
