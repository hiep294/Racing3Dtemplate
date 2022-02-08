using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityStandardAssets.Vehicles.Car;

public class SaveScript : MonoBehaviour
{
    CarController playerCarController; //has boxCollider is trigger, but just to setup player to over to lap
    public static int LapNumber = 0; // when player go to start point, => LapNumber changes
    public static bool LapChange = false;


    void Awake()
    {
        playerCarController = GameObject.FindGameObjectWithTag("Player").GetComponent<CarController>();
    }
}
