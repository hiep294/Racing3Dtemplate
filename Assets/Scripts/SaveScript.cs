using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityStandardAssets.Vehicles.Car;

public class SaveScript : MonoBehaviour
{
    CarController playerCarController; //has boxCollider is trigger, but just to setup player to over to lap
    public static int LapNumber = 0; // when player go to start point, => LapNumber changes
    public static bool LapChange = false;
    public static float LapTime;
    public static float RaceTime;
    public static float LastLapTime;
    public static float BestLapTime;

    void Awake()
    {
        playerCarController = GameObject.FindGameObjectWithTag("Player").GetComponent<CarController>();
    }

    private void Update()
    {
        if (LapChange)
        {
            LapChange = false;
            LastLapTime = LapTime;
            // reset LapTime
            LapTime = 0;

            // calculate BestLap
            if (LapNumber == 2 || (LapNumber > 2 && LastLapTime < BestLapTime))
            {
                BestLapTime = LastLapTime;
            }
        }

        if (LapNumber >= 1)
        {
            LapTime += Time.deltaTime;
            RaceTime += Time.deltaTime;
        }
    }
}
