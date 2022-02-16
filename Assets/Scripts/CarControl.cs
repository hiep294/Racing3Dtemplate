using PathCreation;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityStandardAssets.CrossPlatformInput;

public class CarControl : MonoBehaviour
{
    [Header("Basic information")]
    [SerializeField] PathCreator thePathCreator;
    [SerializeField] float baseMaxSpeed = 200;
    [Tooltip("MaxSpeed could be changed by Nitro")] [SerializeField] float minOfMaxSpeed = 50f;
    [Tooltip("Min between this and its farTracker")] [SerializeField] float minLookAhead = 5f;
    [Tooltip("Time from 0 to maxSpeed")] [SerializeField] [Min(0)] float baseMaxAccelTime = 5f;
    [Tooltip("MaxAccelTime could be changed by Nitro")] [SerializeField] float minOfMaxAccelTime = 0.5f;

    float currentSpeed = 0;
    float desiredSpeed = 0;
    float maxSpeed;
    float maxAccelTime;  // time from 0 to MaxSpeed, will be consider as max time of accel






    //HANDLE BRAKE
    [Header("Handle brake")]
    [SerializeField] float minSpeedAtAnyCorner = 20f;
    readonly float cautiousMaxAngle = 90f;                  // angle of approaching corner to treat as warranting maximum caution

    [Tooltip("Time when car get from maxSpeed to 0")]
    [SerializeField]
    [Min(0)] float baseMaxBrakeTime = 1f;
    [Tooltip("MaxBrakeTime could be changed by Nitro")] [SerializeField] float minOfMaxBrakeTime = 0.5f;

    struct Tracker
    {
        public float distanceTravelled;
        public Vector3 initLocalScale;
        public GameObject theGameObject;
        public GameObject checkingPoint;
        public float aHeadDistance; // how max far between the car and this tracker
        public float approachingCornerAngleForCar;
    }
    float distanceTravelled;
    GameObject trackersContainer;
    Tracker myTracker;
    [SerializeField] float stepToCheckCornerAngle = 5f; // farTracker's aHeadDistance will devide it into parts by this step; => to find maxApproachingCornerAngle
    float maxBrakeTime; // time from MaxSpeed to 0, will be consider as max time of brake





    //NITRO
    [Header("NITRO")]
    [SerializeField] float maxSpeedAdditiveModifier = 100f;
    [SerializeField] [Range(-100, 100)] float maxSpeedPercentageModifier = 100f;

    [SerializeField] float maxAccelTimeAdditiveModifier = -1f;
    [SerializeField] [Range(-100, 100)] float maxAccelTimePercentageModifier = -10f;

    [SerializeField] float maxBrakeTimeAdditiveModifier = 0.5f; // please set it similar to baseBrakeCompletelyTime or less time
    [SerializeField] [Range(-100, 100)] float maxBrakeTimePercentageModifier = -10f; // please set it similar to baseBrakeCompletelyTime or less time

    [SerializeField] string inputNitro = "Jump"; // press Space to run nitro
    [SerializeField] float nitroDuration = 3f;
    [SerializeField] int numberOfTimesUsingNitro = 2;
    [SerializeField] float cleaningNitroDuration = 2;
    float nitroRemainingTime = -Mathf.Infinity;




    public float MaxSpeed { get => maxSpeed; set => maxSpeed = value; }
    public float MaxAccelTime { get => maxAccelTime; set => maxAccelTime = value; }
    public float MaxBrakeTime { get => maxBrakeTime; set => maxBrakeTime = value; }

    void Awake()
    {
        trackersContainer = GameObject.FindWithTag("Trackers");
    }

    void Start()
    {
        StartTracker();
        MaxSpeed = baseMaxSpeed;
        MaxAccelTime = baseMaxAccelTime;
        MaxBrakeTime = baseMaxBrakeTime;
    }

    void StartTracker()
    {
        myTracker = new Tracker()
        {
            theGameObject = GameObject.CreatePrimitive(PrimitiveType.Cylinder),
            checkingPoint = GameObject.CreatePrimitive(PrimitiveType.Cylinder),
            distanceTravelled = 0,
            initLocalScale = new Vector3(4, 4, 4),
            aHeadDistance = minLookAhead,
            approachingCornerAngleForCar = 0
        };

        myTracker.theGameObject.transform.SetParent(trackersContainer.transform);
        myTracker.theGameObject.transform.localScale = myTracker.initLocalScale;
        // Destroy the cylinder collider so it doesn'y cuase any issues with physics
        DestroyImmediate(myTracker.theGameObject.GetComponent<Collider>());
        // Disable the trackers mesh renderer so you can't see it in the game
        //tracker.GetComponent<MeshRenderer>().enabled = false;
        // Rotate and place the tracker

        // similar to tempPoint
        myTracker.checkingPoint.transform.SetParent(trackersContainer.transform);
        myTracker.checkingPoint.GetComponent<MeshRenderer>().enabled = false;
        DestroyImmediate(myTracker.checkingPoint.GetComponent<Collider>());

        myTracker.theGameObject.transform.SetPositionAndRotation(thePathCreator.path.GetPointAtDistance(myTracker.distanceTravelled), thePathCreator.path.GetRotationAtDistance(myTracker.distanceTravelled));
    }

    void Update()
    {
        UpdateNitroManament();

        float minDistanceToStopCar = FindMinDistanceToStopCar();

#if UNITY_EDITOR
        UpdateTrackerMovement(minDistanceToStopCar);
#endif

        UpdateDesiredSpeed(minDistanceToStopCar);

        // handle brake or accel based on DesiredSpeed
        UpdateCurrentSpeed();

        UpdateMovement();

    }



    #region NITRO

    bool IsUsingNitro() { return nitroRemainingTime > 0; }
    bool IsCompletedCleaningNitro() { return MaxSpeed == baseMaxSpeed; }
    bool IsCleaningNitro() { return !IsUsingNitro() && !IsCompletedCleaningNitro(); }

    void UpdateNitroManament()
    {
        nitroRemainingTime -= Time.deltaTime;
        if (!IsUsingNitro())
        {
            TurnOffNitro();
        }

        if (IsCleaningNitro())
        {
            CleanNitro();
        }

        if (CrossPlatformInputManager.GetButtonUp(inputNitro) && !IsUsingNitro() && numberOfTimesUsingNitro > 0)
        {
            UseNitro();
        }
    }

    void UseNitro()
    {
        numberOfTimesUsingNitro--;
        nitroRemainingTime = nitroDuration;
        MaxSpeed = Mathf.Max(minOfMaxSpeed, GetValueModifier(baseMaxSpeed, maxSpeedAdditiveModifier, maxSpeedPercentageModifier));
        MaxAccelTime = Mathf.Max(minOfMaxAccelTime, GetValueModifier(MaxAccelTime, maxAccelTimeAdditiveModifier, maxAccelTimePercentageModifier));
        MaxBrakeTime = Mathf.Max(minOfMaxBrakeTime, GetValueModifier(MaxBrakeTime, maxBrakeTimeAdditiveModifier, maxBrakeTimePercentageModifier));
    }
    void TurnOffNitro()
    {
        nitroRemainingTime = -Mathf.Infinity;
        MaxAccelTime = baseMaxAccelTime;
        MaxBrakeTime = baseMaxBrakeTime;
    }
    void CleanNitro()
    {
        float maxSpeedNitro = Mathf.Max(minOfMaxSpeed, GetValueModifier(baseMaxSpeed, maxSpeedAdditiveModifier, maxSpeedPercentageModifier));
        MaxSpeed += (baseMaxSpeed - maxSpeedNitro) * Time.deltaTime / cleaningNitroDuration;
        if (MaxSpeed < baseMaxSpeed) { MaxSpeed = baseMaxSpeed; }
    }
    #endregion




    #region BASIC MOVEMENT

    float FindMinDistanceToStopCar()
    {
        //* update aHeadDistance of FarTracker, it will be = distance For Car To Stop completely
        float a = (0f - MaxSpeed) / MaxBrakeTime;
        float t = (0f - currentSpeed) / a;
        float distanceForCarToStop = currentSpeed * t + 0.5f * a * t * t + currentSpeed * Time.deltaTime; // + currentSpeed * Time.deltaTime to avoid something small,
        return Mathf.Max(minLookAhead, distanceForCarToStop);
    }

    void UpdateTrackerMovement(float distanceForCarToStop)
    {
        myTracker.distanceTravelled = myTracker.aHeadDistance + distanceTravelled;

        //* update position and rotation of farTracker;
        myTracker.theGameObject.transform.SetPositionAndRotation(thePathCreator.path.GetPointAtDistance(myTracker.distanceTravelled), thePathCreator.path.GetRotationAtDistance(myTracker.distanceTravelled));
    }

    private void UpdateDesiredSpeed(float distanceForCarToStop)
    {
        float approachingCornerAngle = 0;

        //* calc angle of points (between farTracker and this transform) and this transform
        float distanceCount = 0;
        while (distanceCount < distanceForCarToStop + stepToCheckCornerAngle)
        {
            if (distanceCount > distanceForCarToStop)
            {
                // but distanceCount will never > distanceForCarToStop + stepToCheckCornerAngle
                distanceCount = distanceForCarToStop;
                // make sure to check points with stepToCheckCornerAngle and the point in distanceForCarToStop
            }

            myTracker.checkingPoint.transform.SetPositionAndRotation(thePathCreator.path.GetPointAtDistance(distanceTravelled + distanceCount), thePathCreator.path.GetRotationAtDistance(distanceTravelled + distanceCount));
            float angleCornerTest = Vector3.Angle(myTracker.checkingPoint.transform.forward, transform.forward);

            if (approachingCornerAngle < angleCornerTest) approachingCornerAngle = angleCornerTest;

            distanceCount += stepToCheckCornerAngle;
        }
        //myTracker.tempPoint.transform.SetPositionAndRotation(thePathCreator.path.GetPointAtDistance(distanceTravelled), thePathCreator.path.GetRotationAtDistance(distanceTravelled));

        /**
         * Handle Brake
         */
        // the car will brake according to the upcoming change in direction of the target. Useful for route-based AI, slowing for corners.
        // check out the angle of our target compared to the current direction of the car

        approachingCornerAngle = Mathf.Min(approachingCornerAngle, cautiousMaxAngle);
        // if it's different to our current angle, we need to be cautious (i.e. slow down) a certain amount
        desiredSpeed = Mathf.Cos(Mathf.Deg2Rad * approachingCornerAngle) * MaxSpeed;
        desiredSpeed = Mathf.Max(desiredSpeed, minSpeedAtAnyCorner);
    }

    void UpdateCurrentSpeed()
    {
        float upComingCurrentSpeed = currentSpeed;
        if (desiredSpeed > currentSpeed)
        { // accel
            upComingCurrentSpeed += Time.deltaTime * MaxSpeed / MaxAccelTime;
            upComingCurrentSpeed = Mathf.Min(upComingCurrentSpeed, desiredSpeed);
        }
        else if (desiredSpeed < currentSpeed)
        { // bake
            upComingCurrentSpeed -= Time.deltaTime * MaxSpeed / MaxBrakeTime;
            upComingCurrentSpeed = Mathf.Max(upComingCurrentSpeed, desiredSpeed);
        }
        currentSpeed = upComingCurrentSpeed;
    }

    void UpdateMovement()
    {
        distanceTravelled += currentSpeed * Time.deltaTime;
        transform.SetPositionAndRotation(thePathCreator.path.GetPointAtDistance(distanceTravelled), (thePathCreator.path.GetRotationAtDistance(distanceTravelled)));
    }

    #endregion







    float GetValueModifier(float currentValue, float additiveModifier, float percentageModifier)
    {
        return (currentValue + additiveModifier) * (1 + percentageModifier / 100);
    }
}
