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
    [Tooltip("Min between this and its farTracker")] [SerializeField] float minLookAhead = 5f;
    [Tooltip("Time from 0 to maxSpeed")] [SerializeField] [Min(0)] float baseMaxAccelTime = 5f;
    float currentSpeed = 0;
    float desiredSpeed = 0;
    float maxSpeed;
    float maxAccelTime;






    //HANDLE BRAKE
    [Header("Handle brake")]
    [SerializeField] float minSpeedAtAnyCorner = 20f;
    readonly float cautiousMaxAngle = 90f;                  // angle of approaching corner to treat as warranting maximum caution

    [Tooltip("Time when car get from maxSpeed to 0")]
    [SerializeField]
    [Min(0)] float baseMaxBrakeTime = 1f;

    public enum TrackerType
    {
        Near, Far
    }
    class Tracker
    {
        public float distanceTravelled;
        public Vector3 initLocalScale;
        public GameObject theGameObject;
        public GameObject tempPoint;
        public float aHeadDistance; // how max far between the car and this tracker
        public float approachingCornerAngleForCar;
        public Tracker(float distanceTravelled, Vector3 initLocalScale, float aHeadDistance)
        {
            this.distanceTravelled = distanceTravelled;
            this.initLocalScale = initLocalScale;
            this.aHeadDistance = aHeadDistance;
            approachingCornerAngleForCar = 0;
        }
    }
    float distanceTravelled;
    GameObject trackersContainer;
    Dictionary<TrackerType, Tracker> myTrackers;
    [SerializeField] float stepToCheckCornerAngle = 5f; // farTracker's aHeadDistance will devide it into parts by this step; => to find maxApproachingCornerAngle
    float brakeCompletelyTime;
    float approachingCornerAngle;





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
    public float MaxBrakeTime { get => brakeCompletelyTime; set => brakeCompletelyTime = value; }

    void Awake()
    {
        trackersContainer = GameObject.FindWithTag("Trackers");
    }

    void Start()
    {
        StartTrackers();
        MaxSpeed = baseMaxSpeed;
        MaxAccelTime = baseMaxAccelTime;
        MaxBrakeTime = baseMaxBrakeTime;
    }

    void StartTrackers()
    {
        myTrackers = new Dictionary<TrackerType, Tracker> {
            { TrackerType.Near, new Tracker(0, new Vector3(1, 1, 1), 0) },
            { TrackerType.Far, new Tracker(0, new Vector3(4, 4, 4), minLookAhead) }
        };

        foreach (var pair in myTrackers)
        {
            Tracker tracker = pair.Value;
            tracker.tempPoint = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            tracker.tempPoint.transform.SetParent(trackersContainer.transform);
            tracker.tempPoint.GetComponent<MeshRenderer>().enabled = false;

            tracker.theGameObject = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            tracker.theGameObject.transform.SetParent(trackersContainer.transform);
            tracker.theGameObject.transform.localScale = tracker.initLocalScale;
            // Destroy the cylinder collider so it doesn'y cuase any issues with physics
            DestroyImmediate(tracker.theGameObject.GetComponent<Collider>());
            // Disable the trackers mesh renderer so you can't see it in the game
            //tracker.GetComponent<MeshRenderer>().enabled = false;
            // Rotate and place the tracker
            tracker.theGameObject.transform.SetPositionAndRotation(thePathCreator.path.GetPointAtDistance(tracker.distanceTravelled), thePathCreator.path.GetRotationAtDistance(tracker.distanceTravelled));
        }
    }

    void Update()
    {
        UpdateNitroManament();

        UpdateNearTrackerMovement();
        UpdateFarTrackerMovement();

        UpdateCurrentSpeed();

        UpdateMovement();

    }

    #region NITRO

    bool IsUsingNitro() { return nitroRemainingTime > 0; }
    bool IsCompletedCleaningNitro() { return MaxSpeed == baseMaxSpeed; }
    bool IsCleaningNitro() { return !IsUsingNitro() && !IsCompletedCleaningNitro(); }

    void UpdateNitroManament()
    {
        if (CrossPlatformInputManager.GetButtonUp(inputNitro) && !IsUsingNitro() && numberOfTimesUsingNitro > 0)
        {
            UseNitro();
        }

        nitroRemainingTime -= Time.deltaTime;
        if (!IsUsingNitro())
        {
            TurnOffNitro();
        }

        if (IsCleaningNitro())
        {
            CleanNitro();
        }
    }

    void UseNitro()
    {
        numberOfTimesUsingNitro--;
        nitroRemainingTime = nitroDuration;
        MaxSpeed = Mathf.Max(GetValueModifier(baseMaxSpeed, maxSpeedAdditiveModifier, maxSpeedPercentageModifier), 0);
        MaxAccelTime = Mathf.Max(GetValueModifier(MaxAccelTime, maxAccelTimeAdditiveModifier, maxAccelTimePercentageModifier), 0);
        MaxBrakeTime = Mathf.Max(0, GetValueModifier(MaxBrakeTime, maxBrakeTimeAdditiveModifier, maxBrakeTimePercentageModifier));
    }
    void TurnOffNitro()
    {
        nitroRemainingTime = -Mathf.Infinity;
        MaxAccelTime = baseMaxAccelTime;
        MaxBrakeTime = baseMaxBrakeTime;
    }
    void CleanNitro()
    {
        float maxSpeedNitro = Mathf.Max(GetValueModifier(baseMaxSpeed, maxSpeedAdditiveModifier, maxSpeedPercentageModifier), 0);
        MaxSpeed += (baseMaxSpeed - maxSpeedNitro) * Time.deltaTime / cleaningNitroDuration;
        if (MaxSpeed < baseMaxSpeed) { MaxSpeed = baseMaxSpeed; }
    }
    #endregion




    #region BASIC MOVEMENT
    // this will compare with the speed of prev frame
    void UpdateNearTrackerMovement()
    {
        Tracker nearTracker = myTrackers[TrackerType.Near];
        nearTracker.distanceTravelled += currentSpeed * Time.deltaTime; // currentSpeed is the same ad prev frame, but Time.deltaTime is different
        nearTracker.theGameObject.transform.SetPositionAndRotation(thePathCreator.path.GetPointAtDistance(nearTracker.distanceTravelled), (thePathCreator.path.GetRotationAtDistance(nearTracker.distanceTravelled)));

        nearTracker.approachingCornerAngleForCar = Vector3.Angle(nearTracker.theGameObject.transform.forward, transform.forward);

        if (approachingCornerAngle < nearTracker.approachingCornerAngleForCar)
        {
            approachingCornerAngle = nearTracker.approachingCornerAngleForCar;
        }
    }

    // this will compare with currentSpeed of this frame, because 
    void UpdateFarTrackerMovement()
    {
        Tracker farTracker = myTrackers[TrackerType.Far];
        // update aHeadDistance of FarTracker, it will be = distance For Car To Stop completely
        float a = (0f - MaxSpeed) / MaxBrakeTime;
        float t = (0f - currentSpeed) / a;
        float distanceForCarToStop = currentSpeed * t + 0.5f * a * t * t;
        farTracker.aHeadDistance = Mathf.Max(minLookAhead, distanceForCarToStop + currentSpeed * Time.deltaTime); // + currentSpeed * Time.deltaTime to avoid something small,

        farTracker.distanceTravelled = farTracker.aHeadDistance + distanceTravelled;

        // update position and rotation of farTracker;
        farTracker.theGameObject.transform.SetPositionAndRotation(thePathCreator.path.GetPointAtDistance(farTracker.distanceTravelled), thePathCreator.path.GetRotationAtDistance(farTracker.distanceTravelled));
        // cal angle of farTracker and this transform
        farTracker.approachingCornerAngleForCar = Vector3.Angle(farTracker.theGameObject.transform.forward, transform.forward);
        if (approachingCornerAngle < farTracker.approachingCornerAngleForCar)
        {
            approachingCornerAngle = farTracker.approachingCornerAngleForCar;
        }

        // cal angle of points (between farTracker and this transform) and this transform
        float distanceCount = stepToCheckCornerAngle;
        while (distanceCount < farTracker.aHeadDistance)
        {
            farTracker.tempPoint.transform.SetPositionAndRotation(thePathCreator.path.GetPointAtDistance(distanceTravelled + distanceCount), thePathCreator.path.GetRotationAtDistance(distanceTravelled + distanceCount));
            float angleCornerTest = Vector3.Angle(farTracker.tempPoint.transform.forward, transform.forward);
            if (approachingCornerAngle < angleCornerTest) approachingCornerAngle = angleCornerTest;
            distanceCount += stepToCheckCornerAngle;
        }
    }

    void UpdateCurrentSpeed()
    {
        /**
         * Handle Brake
         */
        // the car will brake according to the upcoming change in direction of the target. Useful for route-based AI, slowing for corners.
        // check out the angle of our target compared to the current direction of the car

        approachingCornerAngle = Mathf.Min(approachingCornerAngle, cautiousMaxAngle);
        // if it's different to our current angle, we need to be cautious (i.e. slow down) a certain amount
        desiredSpeed = Mathf.Cos(Mathf.Deg2Rad * approachingCornerAngle) * MaxSpeed;
        float desiredSpeedApplied = Mathf.Max(desiredSpeed, minSpeedAtAnyCorner);

        float upComingCurrentSpeed = currentSpeed;
        if (desiredSpeedApplied > currentSpeed)
        { // accel
            upComingCurrentSpeed += Time.deltaTime * MaxSpeed / MaxAccelTime;
            upComingCurrentSpeed = Mathf.Min(upComingCurrentSpeed, desiredSpeedApplied);
        }
        else if (desiredSpeedApplied < currentSpeed)
        { // bake
            upComingCurrentSpeed -= Time.deltaTime * MaxSpeed / MaxBrakeTime;
            upComingCurrentSpeed = Mathf.Max(upComingCurrentSpeed, desiredSpeedApplied);
        }
        currentSpeed = upComingCurrentSpeed;
    }

    void UpdateMovement()
    {
        distanceTravelled += currentSpeed * Time.deltaTime;
        transform.SetPositionAndRotation(thePathCreator.path.GetPointAtDistance(distanceTravelled), (thePathCreator.path.GetRotationAtDistance(distanceTravelled)));
        approachingCornerAngle = 0;
    }
    #endregion



    float GetValueModifier(float currentValue, float additiveModifier, float percentageModifier)
    {
        return (currentValue + additiveModifier) * (1 + percentageModifier / 100);
    }
}
