using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityStandardAssets.Vehicles.Car;

public class SaveScript : MonoBehaviour
{
    CarController myCarController; //has boxCollider is trigger, but just to setup player to over to lap
    int lapNumber = 0; // when player go to start point, => LapNumber changes
    bool lapChange = false;
    float lapTime;
    float raceTime;
    float lastLapTime;
    float bestLapTime;

    [SerializeField] CheckpointCollider[] checkpoints;

    public int LapNumber { get => lapNumber; set => lapNumber = value; }
    public bool LapChange { get => lapChange; set => lapChange = value; }
    public float LapTime { get => lapTime; set => lapTime = value; }
    public float RaceTime { get => raceTime; set => raceTime = value; }
    public float LastLapTime { get => lastLapTime; set => lastLapTime = value; }
    public float BestLapTime { get => bestLapTime; set => bestLapTime = value; }
    public CheckpointCollider[] Checkpoints { get => checkpoints; }

    void Awake()
    {
        myCarController = GetComponent<CarController>();
    }

    private void Update()
    {
        if (LapNumber >= 1)
        {
            LapTime += Time.deltaTime;
            RaceTime += Time.deltaTime;
        }
    }

    public void OnPlayerTriggerLapCollider()
    {
        LapNumber++;
        LastLapTime = LapTime;
        LapTime = 0;
        foreach (var checkpoint in Checkpoints)
        {
            checkpoint.CheckPointPass = false;
            checkpoint.LastCheckPointTime = checkpoint.ThisCheckPointTime;
        }
        CalculateBestLap();
    }

    void CalculateBestLap()
    {
        if (LapNumber == 2 || (LapNumber > 2 && LastLapTime < BestLapTime))
        {
            BestLapTime = LastLapTime;
        }
    }

}
