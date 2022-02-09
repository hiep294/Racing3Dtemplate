using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// to create thud sound
public class ThudCube : MonoBehaviour
{
    AudioSource myRumbleSoundPlayer; // no loop sound
    bool IsPlaying = false;

    void Awake()
    {
        myRumbleSoundPlayer = GetComponent<AudioSource>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Barrier")) // if not has tag registered, it will throw error
        {
            if (IsPlaying == false)
            {
                myRumbleSoundPlayer.Play();
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject.CompareTag("Barrier")) // if not has tag registered, it will throw error
        {
            if (IsPlaying == true)
            {
                IsPlaying = false;
            }
        }
    }
}
