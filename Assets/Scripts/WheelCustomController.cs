using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityStandardAssets.Vehicles.Car;

public class WheelCustomController : MonoBehaviour
{
    WheelCollider myWheelC;
    [SerializeField] int indexOfWheel;
    [SerializeField] CarController carController;
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
        myWheelC = GetComponent<WheelCollider>();
        carController.OnWheelHitChange += OnWheelHitChange;
        foreach (var stiffness in stiffnesses)
        {
            lookUpStiffnesses[stiffness.tag] = stiffness;
        }
    }

    private void OnWheelHitChange(WheelHit wheelHit, int index)
    {
        if (index != indexOfWheel) return;

        if (wheelHit.collider != null && lookUpStiffnesses.ContainsKey(wheelHit.collider.tag))
        {
            float forwardStiffness = lookUpStiffnesses[wheelHit.collider.tag].forwardStiffness;
            float sidewayStiffness = lookUpStiffnesses[wheelHit.collider.tag].sidewaysStiffness;

            WheelFrictionCurve fFriction = myWheelC.forwardFriction;
            fFriction.stiffness = forwardStiffness;
            myWheelC.forwardFriction = fFriction;

            WheelFrictionCurve sFriction = myWheelC.sidewaysFriction;
            sFriction.stiffness = sidewayStiffness;
            myWheelC.sidewaysFriction = sFriction;
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
