using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LapCollider : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Player"))
        {
            other.GetComponent<SaveScript>().OnPlayerTriggerLapCollider();
        }
    }
}
