using System;
using UnityEngine;
using UnityStandardAssets.CrossPlatformInput;

namespace UnityStandardAssets.Vehicles.Car
{
    [RequireComponent(typeof(CarController))]
    public class CarUserControl : MonoBehaviour
    {
        private CarController m_Car; // the car controller we want to use

        private void Awake()
        {
            // get the car controller
            m_Car = GetComponent<CarController>();
        }

        private void FixedUpdate()
        {
            // pass the input to the car!
            float h = CrossPlatformInputManager.GetAxis("Horizontal");
            float v = CrossPlatformInputManager.GetAxis("Vertical");

            bool isSpeedingUp = v > 0;

            if (isSpeedingUp)
            {
                m_Car.M_SteerHelper = m_Car.AccelerationSteerHelper;
                m_Car.M_MaximumSteerAngle = m_Car.AccelerationMaximumSteerAngle;
            }
            else
            {
                m_Car.M_SteerHelper = m_Car.DecelerationSteerHelper;
                m_Car.M_MaximumSteerAngle = m_Car.DecelerationMaximumSteerAngle;
            }

            if (m_Car.GetGearNum() == 0)
            {
                m_Car.M_MaximumSteerAngle = m_Car.MaximumSteerAngleInLowSpeed;
            }


#if !MOBILE_INPUT
            float handbrake = CrossPlatformInputManager.GetAxis("Jump");
            m_Car.Move(h, v, v, handbrake);
#else
            m_Car.Move(h, v, v, 0f);
#endif
        }
    }
}
