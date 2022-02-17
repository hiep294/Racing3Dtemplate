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


    DesiredTracker m_DesiredTracker = new DesiredTracker()
    {
        distanceTravelled = 0,

    };
    float maxAccelTime;  // time from 0 to MaxSpeed, will be consider as max time of accel
    float maxSpeed;
    float currentSpeed = 0;
    float desiredSpeed;





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
        public GameObject frontPoint; // is always at the position which is min distance to stop the car by car's brake
        public GameObject checkingPoint;
    }
    [System.Serializable]
    public struct DesiredTracker
    {
        // public float desiredSpeed;
        public float distanceTravelled;
        public float approachingCornerAngle;
    }
    float distanceTravelled;
    GameObject trackersContainer;
    Tracker myTracker;
    [SerializeField] float stepToCheckCornerAngle = 5f; // farTracker's aHeadDistance will devide it into parts by this step; => to find maxApproachingCornerAngle
    float maxBrakeTime; // time from MaxSpeed to 0, will be consider as max time of brake
    float minDistanceToStopCar;





    //NITRO
    [Header("NITRO")]
    [SerializeField] [Min(0)] float maxSpeedAdditiveModifier = 100f;
    [SerializeField] [Range(0, 100)] float maxSpeedPercentageModifier = 100f;

    [SerializeField] [Min(0)] float maxAccelTimeAdditiveModifier = -1f;
    [SerializeField] [Range(0, 100)] float maxAccelTimePercentageModifier = -10f;

    [SerializeField] [Min(0)] float maxBrakeTimeAdditiveModifier = 0.5f; // please set it similar to baseBrakeCompletelyTime or less time
    [SerializeField] [Range(0, 100)] float maxBrakeTimePercentageModifier = -10f; // please set it similar to baseBrakeCompletelyTime or less time

    [SerializeField] string inputNitro = "Jump"; // press Space to run nitro
    [SerializeField] float nitroDuration = 3f;
    [SerializeField] int numberOfTimesUsingNitro = 2;
    [SerializeField] float cleaningNitroDuration = 2;
    [SerializeField] float maxApproachingCornerAngle = 30;
    float nitroRemainingTime = -Mathf.Infinity;
    float preparingNitroRemaining = Mathf.Infinity;

    public float MaxSpeed { get => maxSpeed; set => maxSpeed = value; }
    public float MaxAccelTime { get => maxAccelTime; set => maxAccelTime = value; }
    public float MaxBrakeTime
    {
        get => maxBrakeTime;
        set => maxBrakeTime = value;
    }
    public float PreparingNitroRemaining { get => preparingNitroRemaining; set => preparingNitroRemaining = value; }
    public int NumberOfTimesUsingNitro { get => numberOfTimesUsingNitro; set => numberOfTimesUsingNitro = value; }

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
            frontPoint = GameObject.CreatePrimitive(PrimitiveType.Cylinder),
            checkingPoint = GameObject.CreatePrimitive(PrimitiveType.Cylinder),
            distanceTravelled = 0,
            initLocalScale = new Vector3(4, 4, 4)
        };

        myTracker.frontPoint.transform.SetParent(trackersContainer.transform);
        myTracker.frontPoint.transform.localScale = myTracker.initLocalScale;
        // Destroy the cylinder collider so it doesn'y cuase any issues with physics
        DestroyImmediate(myTracker.frontPoint.GetComponent<Collider>());
        // Disable the trackers mesh renderer so you can't see it in the game
        myTracker.frontPoint.GetComponent<MeshRenderer>().enabled = false;
        // Rotate and place the tracker

        // similar to tempPoint
        myTracker.checkingPoint.transform.SetParent(trackersContainer.transform);
        myTracker.checkingPoint.GetComponent<MeshRenderer>().enabled = false;
        DestroyImmediate(myTracker.checkingPoint.GetComponent<Collider>());

        myTracker.frontPoint.transform.SetPositionAndRotation(thePathCreator.path.GetPointAtDistance(myTracker.distanceTravelled), thePathCreator.path.GetRotationAtDistance(myTracker.distanceTravelled));

#if UNITY_EDITOR
        myTracker.checkingPoint.GetComponent<MeshRenderer>().enabled = true;
        myTracker.frontPoint.GetComponent<MeshRenderer>().enabled = true;
#endif

    }

    void Update()
    {
        if (m_DesiredTracker.approachingCornerAngle > maxApproachingCornerAngle && IsUsingNitro())
        {
            StopImmediatelyNitro(m_DesiredTracker); // this will make the maxBrakeTime change to savely handle at corner
        }

        UpdateMovement(desiredSpeed); // desiredSpeed with prev Time.deltaTime

        minDistanceToStopCar = FindMinDistanceToStopCar(MaxSpeed, MaxBrakeTime);
        // float minDistanceToStopCarNitro = FindMinDistanceToStopCar(CalcIntendedNitroMaxSpeed(), CalcIntendedNitroMaxBrakeTime());

#if UNITY_EDITOR
        UpdateTrackerFrontPointMovement();
#endif

        m_DesiredTracker = FindDesiredTracker(distanceTravelled, minDistanceToStopCar);
        // update desiredSpeed;
        desiredSpeed = FindDesiredSpeed(m_DesiredTracker.approachingCornerAngle, MaxSpeed);

        UpdateNitroManament(m_DesiredTracker);
    }





    #region NITRO
    bool IsUsingNitro() { return nitroRemainingTime > 0; }
    bool IsCompletedCleaningNitro() { return Mathf.Approximately(MaxSpeed, baseMaxSpeed); }
    bool IsCleaningNitro() { return !IsUsingNitro() && !IsCompletedCleaningNitro(); }
    public bool ShouldPrepareNitro()
    {
        return !IsUsingNitro() && IsCompletedCleaningNitro() && NumberOfTimesUsingNitro > 0 && PreparingNitroRemaining == Mathf.Infinity;
    }

    // if use nitro now, IntendedNitroMaxSpeed will be calc
    float CalcIntendedNitroMaxSpeed()
    {
        return Mathf.Max(minOfMaxSpeed, GetValueModifier(baseMaxSpeed, maxSpeedAdditiveModifier, maxSpeedPercentageModifier));
    }
    // if use nitro now, IntendedNitroMaxAccelTime will be calc
    float CalcIntendedNitroMaxAccelTime()
    {
        return Mathf.Max(minOfMaxAccelTime, GetValueModifier(baseMaxAccelTime, maxAccelTimeAdditiveModifier, maxAccelTimePercentageModifier));
    }
    // if use nitro now, IntendedNitroMaxBrakeTime will be calc
    float CalcIntendedNitroMaxBrakeTime()
    {
        return Mathf.Max(minOfMaxBrakeTime, GetValueModifier(baseMaxBrakeTime, maxBrakeTimeAdditiveModifier, maxBrakeTimePercentageModifier));
    }

    void UpdateNitroManament(DesiredTracker currentDesiredTracker)
    {
        nitroRemainingTime -= Time.deltaTime;
        bool isUsingNitro = IsUsingNitro();
        if (!isUsingNitro)
        {
            TurnOffNitro();
        }

        if (IsCleaningNitro())
        {
            CleanNitro();
        }

        if (CrossPlatformInputManager.GetButtonUp(inputNitro))
        {
            PrepareToUseNitro();
        }
        UseNitro(currentDesiredTracker); // this will change MaxBrakeTime
    }

    private void StopImmediatelyNitro(DesiredTracker currentDesiredTracker)
    {
        TurnOffNitro(); // this will change MaxBrakeTime
        CleanNitroCompletely();
        // setup brakeTime corresponding to maxBrakeTime_of Nitro
        // currentSpeed also need to change immediately
        float v = FindDesiredSpeed(currentDesiredTracker.approachingCornerAngle, MaxSpeed);
        float s = currentDesiredTracker.distanceTravelled - distanceTravelled;
        float a = (0 - baseMaxSpeed) / baseMaxBrakeTime;
        // v*v -vo * vo = 2as // https://vietjack.com/vat-ly-lop-10/xac-dinh-van-toc-gia-toc-quang-duong-trong-chuyen-dong-thang-bien-doi-deu.jsp
        float vo = Mathf.Sqrt(v * v - 2 * a * s);
        currentSpeed = vo;
        Debug.Log($"currentSpeed {currentSpeed} ; v {v}");
    }

    public void PrepareToUseNitro()
    {
        if (ShouldPrepareNitro())
        {
            Debug.Log("PreparingNitroRemaining");
            PreparingNitroRemaining = 0;
        }
    }
    void UseNitro(DesiredTracker currentDesiredTracker)
    {
        PreparingNitroRemaining -= Time.deltaTime * 2;

        if (PreparingNitroRemaining > 0) return;
        PreparingNitroRemaining = Mathf.Infinity;

        NumberOfTimesUsingNitro--;
        nitroRemainingTime = nitroDuration;
        MaxSpeed = CalcIntendedNitroMaxSpeed();
        MaxAccelTime = CalcIntendedNitroMaxAccelTime();
        MaxBrakeTime = CalcIntendedNitroMaxBrakeTime();
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
        if (MaxSpeed < baseMaxSpeed) { CleanNitroCompletely(); }
    }
    void CleanNitroCompletely()
    {
        MaxSpeed = baseMaxSpeed;
    }

    #endregion




    #region BASIC MOVEMENT

    // handle brake or accel based on DesiredSpeed
    // will not check distance to desiredPoint like in UpdateNitro, because the start velocity of the car is zero, so it will be ok
    void UpdateMovement(float desiredSpeed)
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

        // update movement
        distanceTravelled += currentSpeed * Time.deltaTime;
        transform.SetPositionAndRotation(thePathCreator.path.GetPointAtDistance(distanceTravelled), (thePathCreator.path.GetRotationAtDistance(distanceTravelled)));
    }

    // the car will brake
    float FindMinDistanceToStopCar(float paramMaxSpeed, float paramMaxBrakeTime)
    {
        //* update aHeadDistance of FarTracker, it will be = distance For Car To Stop completely
        float a = (0f - paramMaxSpeed) / paramMaxBrakeTime;
        float distanceForCarToStop = FindDistanceGoing(a, currentSpeed, 0); // + currentSpeed * Time.deltaTime to avoid something small,
        return Mathf.Max(minLookAhead, distanceForCarToStop);
    }

    float FindDistanceGoing(float accelOrBrake, float currentSpeed, float lastSpeed)
    {
        float t = (lastSpeed - currentSpeed) / accelOrBrake;
        return (float)(currentSpeed * t + 0.5 * accelOrBrake * t * t);
    }

    void UpdateTrackerFrontPointMovement()
    {
        float frontPointDistanceTravelled = minDistanceToStopCar + distanceTravelled;

        //* update position and rotation of farTracker;
        myTracker.frontPoint.transform.SetPositionAndRotation(thePathCreator.path.GetPointAtDistance(frontPointDistanceTravelled), thePathCreator.path.GetRotationAtDistance(frontPointDistanceTravelled));
    }

    /**
    * find desiredSpeed for current position of car
    */
    private DesiredTracker FindDesiredTracker(float startDistanceTravelled, float rangeAhead)
    {
        float approachingCornerAngle = 0;

        //* calc angle of points (between farTracker and this transform) and this transform
        float suitableDistanceCount = 0; //cache for appropriate approachingCornerAngle
        float distanceCount = -stepToCheckCornerAngle;
        while (distanceCount < rangeAhead + stepToCheckCornerAngle)
        {
            if (distanceCount > rangeAhead)
            {
                distanceCount = (Mathf.Ceil(rangeAhead * 10)) / 10; // to avoid looping forever
            }


            myTracker.checkingPoint.transform.SetPositionAndRotation(thePathCreator.path.GetPointAtDistance(startDistanceTravelled + distanceCount), thePathCreator.path.GetRotationAtDistance(startDistanceTravelled + distanceCount));
            float angleCornerTest = Vector3.Angle(myTracker.checkingPoint.transform.forward, transform.forward);

            if (approachingCornerAngle < angleCornerTest)
            {
                approachingCornerAngle = angleCornerTest;
                suitableDistanceCount = distanceCount;
            };

            distanceCount += stepToCheckCornerAngle;
        }

        /**
         * Handle Brake
         */
        // the car will brake according to the upcoming change in direction of the target. Useful for route-based AI, slowing for corners.
        // check out the angle of our target compared to the current direction of the car

        approachingCornerAngle = Mathf.Min(approachingCornerAngle, cautiousMaxAngle);
        // if it's different to our current angle, we need to be cautious (i.e. slow down) a certain amount

#if UNITY_EDITOR
        myTracker.checkingPoint.transform.SetPositionAndRotation(thePathCreator.path.GetPointAtDistance(startDistanceTravelled - stepToCheckCornerAngle), thePathCreator.path.GetRotationAtDistance(startDistanceTravelled - stepToCheckCornerAngle));
#endif

        return new DesiredTracker()
        {
            distanceTravelled = suitableDistanceCount + startDistanceTravelled,
            approachingCornerAngle = approachingCornerAngle
        };
    }

    float FindDesiredSpeed(float approachingCornerAngle, float paramMaxSpeed)
    {
        float desiredSpeed = 0;
        desiredSpeed = Mathf.Cos(Mathf.Deg2Rad * approachingCornerAngle) * paramMaxSpeed;
        desiredSpeed = Mathf.Max(desiredSpeed, minSpeedAtAnyCorner);
        return desiredSpeed;
    }

    #endregion







    float GetValueModifier(float currentValue, float additiveModifier, float percentageModifier)
    {
        return (currentValue + additiveModifier) * (1 + percentageModifier / 100);
    }
}
