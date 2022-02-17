using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BtnNitro : MonoBehaviour
{
    CarControl playerCarControl;
    Button myBtn;

    // Start is called before the first frame update
    void Awake()
    {
        playerCarControl = GameObject.FindWithTag("Player").GetComponent<CarControl>();
        myBtn = GetComponent<Button>();
    }

    // Update is called once per frame
    private void Update()
    {
        myBtn.interactable = playerCarControl.ShouldPrepareNitro();
    }

    // trigger by event
    public void OnClick()
    {
        playerCarControl.PrepareToUseNitro();
    }
}
