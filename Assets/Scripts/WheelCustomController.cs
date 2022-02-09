using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityStandardAssets.Vehicles.Car;

public class WheelCustomController : MonoBehaviour
{
    WheelCollider myWheelCollider;
    bool isHittingRumble = false;

    [System.Serializable]
    public struct StiffnessesStruct
    {
        public string tag;
        public float forwardStiffness;
        public float sidewaysStiffness;
    }

    [SerializeField] StiffnessesStruct[] stiffnesses;

    Dictionary<string, StiffnessesStruct> lookUpStiffnesses = new Dictionary<string, StiffnessesStruct>();

    public StiffnessesStruct[] Stiffnesses { get => stiffnesses; }
    public Dictionary<string, StiffnessesStruct> LookUpStiffnesses { get => lookUpStiffnesses; }
    public bool IsHittingRumble { get => isHittingRumble; set => isHittingRumble = value; }

    // Start is called before the first frame update
    private void Awake()
    {
        myWheelCollider = GetComponent<WheelCollider>();
        foreach (var stiffness in stiffnesses)
        {
            lookUpStiffnesses[stiffness.tag] = stiffness;
        }
    }

    private void FixedUpdate()
    {
        WheelHit wheelHit;
        myWheelCollider.GetGroundHit(out wheelHit);
        OnWheelHitChange(wheelHit);
    }

    private void OnWheelHitChange(WheelHit wheelHit)
    {
        if (wheelHit.collider != null && lookUpStiffnesses.ContainsKey(wheelHit.collider.tag))
        {
            float forwardStiffness = lookUpStiffnesses[wheelHit.collider.tag].forwardStiffness;
            float sidewayStiffness = lookUpStiffnesses[wheelHit.collider.tag].sidewaysStiffness;

            WheelFrictionCurve fFriction = myWheelCollider.forwardFriction;
            fFriction.stiffness = forwardStiffness;
            myWheelCollider.forwardFriction = fFriction;

            WheelFrictionCurve sFriction = myWheelCollider.sidewaysFriction;
            sFriction.stiffness = sidewayStiffness;
            myWheelCollider.sidewaysFriction = sFriction;
        }


        if (wheelHit.collider != null && wheelHit.collider.CompareTag("RumbleStrip"))
        {
            IsHittingRumble = true;
        }
        else
        {
            IsHittingRumble = false;
        }
    }
}
