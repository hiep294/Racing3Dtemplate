using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Lap : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        Debug.Log("asdasd");
        if (other.gameObject.CompareTag("Player"))
        {
            SaveScript.LapNumber++;
        }
    }
}
