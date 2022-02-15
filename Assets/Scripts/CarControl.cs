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
    [SerializeField] float baseMaxSpeed = 100;
    [Tooltip("Min between this and its farTracker")] [SerializeField] float minLookAhead = 5f;
    [Tooltip("Time from 0 to maxSpeed")] [SerializeField] [Min(0)] float baseMaxSpeedTime = 5f; //note*: time from maxSpeed to 0: will be handle by the brake, at cornors 


    //HANDLE BRAKE
    [Header("Handle brake")]
    [SerializeField] float minSpeedAtAnyCorner = 10f;
    readonly float cautiousMaxAngle = 90f;                  // angle of approaching corner to treat as warranting maximum caution

    [Tooltip("Time when car get from maxSpeed to 0")]
    [SerializeField]
    [Min(0)] float baseBrakeCompletelyTime = 1f;

    public enum TrackerType
    {
        Near, Far
    }
    public class Tracker
    {
        public float distanceTravelled;
        public Vector3 initLocalScale;
        public GameObject theGameObject;
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



    //NITRO
    [Header("NITRO")]
    [SerializeField] float maxSpeedAdditiveModifier = 50f;
    [SerializeField] [Range(-100, 100)] float maxSpeedPercentageModifier = 10f;
    [SerializeField] float maxSpeedTimeAdditiveModifier = -1f;
    [SerializeField] [Range(-100, 100)] float maxSpeedTimePercentageModifier = -50f;
    [SerializeField] float brakeCompletelyTimeAdditiveModifier = 1f;
    [SerializeField] [Range(-100, 100)] float brakeCompletelyTimePercentageModifier = 20f;
    [SerializeField] string inputNitro = "Jump";
    [SerializeField] float duration = 3f;

    float currentSpeed = 0;
    float desiredSpeed = 0;
    float maxSpeed;
    float maxSpeedTime;
    float brakeCompletelyTime;
    float approachingCornerAngle;

    public float MaxSpeed { get => maxSpeed; set => maxSpeed = value; }
    public float MaxSpeedTime { get => maxSpeedTime; set => maxSpeedTime = value; }
    public float BrakeCompletelyTime { get => brakeCompletelyTime; set => brakeCompletelyTime = value; }

    void Awake()
    {
        trackersContainer = GameObject.FindWithTag("Trackers");
    }

    void Start()
    {
        StartTrackers();
        MaxSpeed = baseMaxSpeed;
        MaxSpeedTime = baseMaxSpeedTime;
        BrakeCompletelyTime = baseBrakeCompletelyTime;
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

        UpdateCurrentSpeed();

        UpdateMovement();
        UpdateFarTrackerMovement();

    }

    #region NITRO
    public void UpdateNitroManament()
    {
        if (CrossPlatformInputManager.GetButtonUp(inputNitro))
        {
            UseNitro();
        }
    }

    private void UseNitro()
    {
        Debug.Log("UseNitro");
        MaxSpeed = GetValueModifier(MaxSpeed, maxSpeedAdditiveModifier, maxSpeedPercentageModifier);
        MaxSpeedTime = GetValueModifier(MaxSpeedTime, maxSpeedTimeAdditiveModifier, maxSpeedTimePercentageModifier);
        BrakeCompletelyTime = GetValueModifier(BrakeCompletelyTime, brakeCompletelyTimeAdditiveModifier, brakeCompletelyTimePercentageModifier);
    }

    #endregion




    #region BASIC MOVEMENT
    // this will compare with the speed of prev frame
    private void UpdateNearTrackerMovement()
    {
        Tracker nearTracker = myTrackers[TrackerType.Near];
        nearTracker.distanceTravelled += currentSpeed * Time.deltaTime; // currentSpeed is the same ad prev frame, but Time.deltaTime is different
        nearTracker.theGameObject.transform.SetPositionAndRotation(thePathCreator.path.GetPointAtDistance(nearTracker.distanceTravelled), (thePathCreator.path.GetRotationAtDistance(nearTracker.distanceTravelled)));
    }

    // this will compare with currentSpeed of this frame, because 
    void UpdateFarTrackerMovement()
    {
        Tracker farTracker = myTrackers[TrackerType.Far];
        // update aHeadDistance of FarTracker, it will be = distance For Car To Stop completely
        float a = (0f - MaxSpeed) / BrakeCompletelyTime;
        float t = (0f - currentSpeed) / a;
        float distanceForCarToStop = currentSpeed * t + 0.5f * a * t * t;
        farTracker.aHeadDistance = Mathf.Max(minLookAhead, distanceForCarToStop);

        // stop farTracker in need, to avoid it is too far from the car
        if (farTracker.distanceTravelled - distanceTravelled >= farTracker.aHeadDistance)
        {
            return;
        }

        // moving farTracker
        float farTrackerSpeed = desiredSpeed;
        if (Mathf.Approximately(desiredSpeed, currentSpeed))
        {
            farTrackerSpeed = currentSpeed * 2 + 50; // to ensure tracker will go faster;
        }

        farTracker.distanceTravelled += farTrackerSpeed * Time.deltaTime;

        if (farTracker.distanceTravelled - distanceTravelled > farTracker.aHeadDistance)
        {
            farTracker.distanceTravelled = farTracker.aHeadDistance + distanceTravelled;
        }

        farTracker.theGameObject.transform.SetPositionAndRotation(thePathCreator.path.GetPointAtDistance(farTracker.distanceTravelled), thePathCreator.path.GetRotationAtDistance(farTracker.distanceTravelled));
    }

    void UpdateCurrentSpeed()
    {
        /**
         * Handle Brake
         */
        // the car will brake according to the upcoming change in direction of the target. Useful for route-based AI, slowing for corners.
        // check out the angle of our target compared to the current direction of the car
        Tracker farTracker = myTrackers[TrackerType.Far];
        Tracker nearTracker = myTrackers[TrackerType.Near];

        farTracker.approachingCornerAngleForCar = Vector3.Angle(farTracker.theGameObject.transform.forward, transform.forward);
        nearTracker.approachingCornerAngleForCar = Vector3.Angle(nearTracker.theGameObject.transform.forward, transform.forward);

        approachingCornerAngle = Mathf.Max(farTracker.approachingCornerAngleForCar, nearTracker.approachingCornerAngleForCar);
        approachingCornerAngle = Mathf.Min(approachingCornerAngle, cautiousMaxAngle);
        // if it's different to our current angle, we need to be cautious (i.e. slow down) a certain amount
        desiredSpeed = Mathf.Cos(Mathf.Deg2Rad * approachingCornerAngle) * MaxSpeed;
        float desiredSpeedApplied = Mathf.Max(desiredSpeed, minSpeedAtAnyCorner);

        float upComingCurrentSpeed = currentSpeed;
        if (desiredSpeedApplied > currentSpeed)
        { // accel
            upComingCurrentSpeed += Time.deltaTime * MaxSpeed / MaxSpeedTime;
            upComingCurrentSpeed = Mathf.Min(upComingCurrentSpeed, desiredSpeedApplied);
        }
        else if (desiredSpeedApplied < currentSpeed)
        { // bake
            upComingCurrentSpeed -= Time.deltaTime * MaxSpeed / BrakeCompletelyTime;
            upComingCurrentSpeed = Mathf.Max(upComingCurrentSpeed, desiredSpeedApplied);
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
