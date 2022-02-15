using PathCreation;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CarControl : MonoBehaviour
{
    [SerializeField] PathCreator thePathCreator;
    [SerializeField] float maxSpeed = 100;
    [Tooltip("Min between this and its farTracker")] [SerializeField] float minLookAhead = 5f;
    [Tooltip("Time when car get from 0 to maxSpeed")] [SerializeField] [Min(0)] float timeToGetMaxSpeed = 5f; //note*: time from maxSpeed to 0: will be handle by the brake, at cornors 

    [Header("Handle brake")]
    [SerializeField] float minSpeedAtAnyCorner = 10f;
    readonly float cautiousMaxAngle = 90f;                  // angle of approaching corner to treat as warranting maximum caution

    [Tooltip("Time when car get from maxSpeed to 0")]
    [SerializeField]
    [Min(0)] float timeToBrakeCompletely = 1f; //note*: time from maxSpeed to 0: will be handle by the brake, at cornors 

    public enum TrackerType
    {
        Near, Far
    }
    [System.Serializable]
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
    //GameObject myFarTracker; // to check whether to brake or not
    //GameObject myCloseTracker; // to check whether to brake or not

    float currentSpeed = 0;

    void Awake()
    {
        trackersContainer = GameObject.FindWithTag("Trackers");
    }

    void Start()
    {
        StartTrackers();
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
        UpdateNearTrackerMovement();
        UpdateFarTrackerMovement();

        UpdateCurrentSpeed();

        UpdateMovement();
    }

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
        float a = (0f - maxSpeed) / timeToBrakeCompletely;
        float t = (0f - currentSpeed) / a;
        float distanceForCarToStop = currentSpeed * t + 0.5f * a * t * t;
        farTracker.aHeadDistance = Mathf.Max(minLookAhead, distanceForCarToStop);


        if (farTracker.distanceTravelled - distanceTravelled > farTracker.aHeadDistance)
        {
            return;
        }

        float farTrackerSpeed = currentSpeed + 15f;
        if (farTracker.distanceTravelled - distanceTravelled > farTracker.aHeadDistance - minLookAhead)
        {
            farTrackerSpeed = currentSpeed + 5f;
        }
        farTracker.distanceTravelled += farTrackerSpeed * Time.deltaTime;
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

        float approachingCornerAngle = Mathf.Max(farTracker.approachingCornerAngleForCar, nearTracker.approachingCornerAngleForCar);
        approachingCornerAngle = Mathf.Min(approachingCornerAngle, cautiousMaxAngle);
        // if it's different to our current angle, we need to be cautious (i.e. slow down) a certain amount
        float desiredSpeed = Mathf.Cos(Mathf.Deg2Rad * approachingCornerAngle) * maxSpeed;
        desiredSpeed = Mathf.Max(desiredSpeed, minSpeedAtAnyCorner);

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
