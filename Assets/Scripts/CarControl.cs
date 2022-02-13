using PathCreation;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CarControl : MonoBehaviour
{
    [SerializeField] PathCreator thePathCreator;
    [SerializeField] float maxSpeed = 100;
    [Tooltip("Distance between this and its myTracker")] [SerializeField] float lookAhead = 40f;
    [Tooltip("Time when car get from 0 to maxSpeed")] [SerializeField] [Min(0)] float timeToGetMaxSpeed = 5f; //note*: time from maxSpeed to 0: will be handle by the brake, at cornors 

    [Header("Handle brake")]
    [SerializeField] [Range(0, 1)] float cautiousSpeedFactor = 0.2f;               // percentage of max speed to use when being maximally cautious
    [SerializeField] [Range(0, 180)] float cautiousMaxAngle = 180f;                  // angle of approaching corner to treat as warranting maximum caution
    [Tooltip("Time when car get from maxSpeed to 0")] [SerializeField] [Min(0)] float timeToBrakeCompletely = 2f; //note*: time from maxSpeed to 0: will be handle by the brake, at cornors 

    float distanceTravelled;
    GameObject trackers;
    GameObject myTracker; // to check whether to brake or not

    float currentSpeed = 0;

    void Awake()
    {
        trackers = GameObject.FindWithTag("Trackers");
    }

    void Start()
    {
        StartTracker();
    }

    void Update()
    {
        // Call the ProcessTracker method to move the tracker
        UpdateTrackerMovement();

        UpdateCurrentSpeed();

        UpdateMovement();
    }

    void StartTracker()
    {
        myTracker = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        myTracker.transform.SetParent(trackers.transform);
        myTracker.transform.localScale = new Vector3(4, 4, 4);
        // Destroy the cylinder collider so it doesn'y cuase any issues with physics
        DestroyImmediate(myTracker.GetComponent<Collider>());
        // Disable the trackers mesh renderer so you can't see it in the game
        //tracker.GetComponent<MeshRenderer>().enabled = false;
        // Rotate and place the tracker
        myTracker.transform.SetPositionAndRotation(this.transform.position, this.transform.rotation);
    }

    // this will not use desiredSpeed
    void UpdateTrackerMovement()
    {
        float distanceTravelledOfTracker = distanceTravelled + lookAhead;
        myTracker.transform.SetPositionAndRotation(thePathCreator.path.GetPointAtDistance(distanceTravelledOfTracker), (thePathCreator.path.GetRotationAtDistance(distanceTravelledOfTracker)));
    }

    void UpdateCurrentSpeed()
    {
        /**
         * Handle Brake
         */
        // the car will brake according to the upcoming change in direction of the target. Useful for route-based AI, slowing for corners.
        // check out the angle of our target compared to the current direction of the car
        float approachingCornerAngle = Vector3.Angle(myTracker.transform.forward, transform.forward);
        // tested: Debug.Log($"myTracker.transform.forward {myTracker.transform.forward}; transform.forward {transform.forward}; approachingCornerAngle {approachingCornerAngle}");
        // this tested passed, the Rotation has set for both myTracker and transform.rotation

        // if it's different to our current angle, we need to be cautious (i.e. slow down) a certain amount
        float cautiousnessRequired = Mathf.InverseLerp(0, cautiousMaxAngle, approachingCornerAngle);
        float desiredSpeed = Mathf.Lerp(maxSpeed, maxSpeed * cautiousSpeedFactor, cautiousnessRequired);

        float upComingCurrentSpeed = currentSpeed;
        if (desiredSpeed > currentSpeed)
        { // accel
            upComingCurrentSpeed += Time.deltaTime * maxSpeed / timeToGetMaxSpeed;
            upComingCurrentSpeed = Mathf.Min(upComingCurrentSpeed, desiredSpeed);
        }
        else if (desiredSpeed < currentSpeed)
        {//bake
            upComingCurrentSpeed -= Time.deltaTime * maxSpeed / timeToBrakeCompletely;
            upComingCurrentSpeed = Mathf.Max(upComingCurrentSpeed, desiredSpeed);
        }
        currentSpeed = upComingCurrentSpeed;
    }

    void UpdateMovement()
    {
        distanceTravelled += currentSpeed * Time.deltaTime;
        transform.SetPositionAndRotation(thePathCreator.path.GetPointAtDistance(distanceTravelled), (thePathCreator.path.GetRotationAtDistance(distanceTravelled)));
    }



}
