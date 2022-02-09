using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// to compare whether the Player is good at this point or not
public class CheckpointCollider : MonoBehaviour
{
    float lastCheckPointTime; // = this.thisCheckPointTime when player triggers the lap
    float thisCheckPointTime; // = GameTime of player when player triggers this checkpoint
    bool checkPointPass = false;

    public float LastCheckPointTime { get => lastCheckPointTime; set => lastCheckPointTime = value; }
    public float ThisCheckPointTime { get => thisCheckPointTime; set => thisCheckPointTime = value; }
    public bool CheckPointPass { get => checkPointPass; set => checkPointPass = value; }

    public float CheckImprovement()
    {
        if (lastCheckPointTime == 0) return 0;
        float rs = lastCheckPointTime - thisCheckPointTime;
        return rs;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Player"))
        {
            var playerSaveScript = other.GetComponent<SaveScript>();
            ThisCheckPointTime = playerSaveScript.LapTime;
            CheckPointPass = true;
        }
    }
}
