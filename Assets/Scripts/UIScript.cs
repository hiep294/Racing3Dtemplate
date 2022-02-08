using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityStandardAssets.Vehicles.Car;
using TMPro;

public class UIScript : MonoBehaviour
{
    public Image SpeedRing;
    public TextMeshProUGUI LapNumberText;
    public int TotalLaps = 3;
    public TextMeshProUGUI SpeedText;
    public TextMeshProUGUI GearText; //example: speed (0-90) => gear 1; speed (90-120) gear 2; speed 120-180 is gear 3; speed 180-230 is gear 4
    CarController playerCarController;

    void Awake()
    {
        playerCarController = GameObject.FindGameObjectWithTag("Player").GetComponent<CarController>();
    }

    void Start()
    {
        SpeedRing.fillAmount = 0;
        SpeedText.text = "0";
        GearText.text = "1";
    }

    // Update is called once per frame
    void LateUpdate()
    {
        float speed = playerCarController.CurrentSpeed;
        float maxSpeed = playerCarController.MaxSpeed;
        SpeedRing.fillAmount = speed / maxSpeed;
        SpeedText.text = $"{speed:0}";
        GearText.text = $"{playerCarController.GetGearNum() + 1}";
        LapNumberText.text = $"{SaveScript.LapNumber}/{TotalLaps}";
    }
}
