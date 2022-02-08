using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityStandardAssets.Vehicles.Car;
using TMPro;
using System;

public class UIScript : MonoBehaviour
{
    [SerializeField] Image SpeedRing;
    [SerializeField] TextMeshProUGUI LapTimeText;
    [SerializeField] TextMeshProUGUI LapNumberText;
    [SerializeField] int TotalLaps = 3;
    [SerializeField] TextMeshProUGUI SpeedText;
    [SerializeField] TextMeshProUGUI GearText; //example: speed (0-90) => gear 1; speed (90-120) gear 2; speed 120-180 is gear 3; speed 180-230 is gear 4
    [SerializeField] TextMeshProUGUI RaceTimeText;
    [SerializeField] TextMeshProUGUI BestLapTimeText;
    [SerializeField] TextMeshProUGUI CheckpointText;

    CarController playerCarController;
    SaveScript playerSaveScript;

    void Awake()
    {
        playerCarController = GameObject.FindGameObjectWithTag("Player").GetComponent<CarController>();
        playerSaveScript = playerCarController.GetComponent<SaveScript>();
    }

    void Start()
    {
        SpeedRing.fillAmount = 0;
        SpeedText.text = "0";
        GearText.text = "1";
        StartCoroutine(MakeCheckpointShow(0, 0));
    }

    // Update is called once per frame
    void LateUpdate()
    {
        float speed = playerCarController.CurrentSpeed;
        float maxSpeed = playerCarController.MaxSpeed;
        SpeedRing.fillAmount = speed / maxSpeed;
        SpeedText.text = $"{speed:0}";
        GearText.text = $"{playerCarController.GetGearNum() + 1}";
        LapNumberText.text = $"{playerSaveScript.LapNumber}/{TotalLaps}";

        var LapTime = TimeSpan.FromSeconds(playerSaveScript.LapTime);
        //LapTimeText.text = $"{LapTime / 60:00}:{LapTime % 60:00}";
        LapTimeText.text = $"{LapTime:mm}:{LapTime:ss}";

        var RaceTime = TimeSpan.FromSeconds(playerSaveScript.RaceTime);
        RaceTimeText.text = $"{RaceTime:mm}:{RaceTime:ss}";

        var BestLapTime = TimeSpan.FromSeconds(playerSaveScript.BestLapTime);
        BestLapTimeText.text = $"{BestLapTime:mm}:{BestLapTime:ss}";

        foreach (var checkpoint in playerSaveScript.Checkpoints)
        {
            if (checkpoint.CheckPointPass)
            {
                checkpoint.CheckPointPass = false;
                StartCoroutine(MakeCheckpointDisplay(checkpoint));
            }
        }

    }

    private IEnumerator MakeCheckpointDisplay(CheckpointCollider checkpoint)
    {
        float playerImprovement = checkpoint.CheckImprovement();
        if (playerImprovement > 0)
        {
            CheckpointText.color = Color.green;
            CheckpointText.text = $"+{playerImprovement:0.00}";
        }
        if (playerImprovement < 0)
        {
            CheckpointText.color = Color.red;
            CheckpointText.text = $"{playerImprovement:0.00}";
        }
        if (playerImprovement != 0)
        {
            yield return MakeCheckpointShow(0.5f, 1);
            yield return new WaitForSeconds(3);
            yield return MakeCheckpointShow(1, 0);
        }
    }

    /**
     * targetAlphaOfColor: 0 or 1
     */
    private IEnumerator MakeCheckpointShow(float time, float targetAlphaOfColor)
    {
        while (!Mathf.Approximately(CheckpointText.color.a, targetAlphaOfColor))
        {
            var r = CheckpointText.color.r;
            var g = CheckpointText.color.g;
            var b = CheckpointText.color.b;
            var a = CheckpointText.color.a;
            a = Mathf.MoveTowards(a, targetAlphaOfColor, Time.deltaTime * (1 - 0) / time); // 1: max of a, 0: min of a
            CheckpointText.color = new Color(r, g, b, a);
            yield return null;
        }

        //tu gia tri a => gia tri b
        //ton t time;
        //1 lan loop ton deltaTime
        //n: so lan loop
        //=> t = n * deltaTime

        //z: ty le thay doi: maxDelta
        //z* n = b - a
        //z = (b - a) / n
        // = (b - a) * deltaTime / t
    }
}
