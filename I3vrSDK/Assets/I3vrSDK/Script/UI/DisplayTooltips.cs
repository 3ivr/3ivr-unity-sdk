/*
 * Copyright (C) 2017 3ivr. All rights reserved.
 *
 * Author: Lucas(Wu Pengcheng)
 * Date  : 2017/06/19 08:08
 */

using UnityEngine;
using System.Collections;
using System;
using UnityEngine.UI;

public class DisplayTooltips : MonoBehaviour
{
    public bool IsDisplayUI;
    public float MinAngles_x;
    public float MaxAngles_x;
    GameObject ButtonTooltips;

    Image TouchPadPos;
    Vector3 StartPoint;
    Vector3 TouchPosPoint;

    private void Awake()
    {
        ButtonTooltips = transform.FindChild("Tooltip").FindChild("ButtonTooltip").gameObject;
        TouchPadPos = transform.FindChild("Tooltip").FindChild("TouchPoint").GetComponent<Image>();
        ButtonTooltips.SetActive(false);
        StartPoint = TouchPadPos.rectTransform.localPosition;
    }

    // Update is called once per frame
    void Update()
    {
        if (IsDisplayUI)
        {
            if (transform.localRotation.eulerAngles.x < MaxAngles_x && transform.localRotation.eulerAngles.x > MinAngles_x)
            {
                ButtonTooltips.SetActive(true);
            }
            else ButtonTooltips.SetActive(false);
        }
        if (I3vrController.IsTouching)
        {
            TouchPadPos.gameObject.SetActive(true);
            TouchPosPoint.Set(I3vrController.TouchPos.x, -I3vrController.TouchPos.y, 0);
            TouchPadPos.rectTransform.localPosition = StartPoint + TouchPosPoint * 108;
        }
        else TouchPadPos.gameObject.SetActive(false);

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Application.Quit();
        }
    }
}
