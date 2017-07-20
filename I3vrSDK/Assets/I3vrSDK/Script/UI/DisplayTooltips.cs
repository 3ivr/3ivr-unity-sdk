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
    private I3vrController controller;
    private GameObject ButtonTooltips;
    private Image TouchPadPos;
    private Vector3 StartPoint;
    private Vector3 TouchPosPoint;

    public bool IsDisplayUI;
    public float MinAngles_x;
    public float MaxAngles_x;
    public DataSource ControllerDataSource = DataSource.Right;

    private void Awake()
    {
        ButtonTooltips = transform.FindChild("Tooltip").FindChild("ButtonTooltip").gameObject;
        TouchPadPos = transform.FindChild("Tooltip").FindChild("TouchPoint").GetComponent<Image>();
        ButtonTooltips.SetActive(false);
        StartPoint = TouchPadPos.rectTransform.localPosition;
    }
    private void Start()
    {
        controller = I3vrControllerManager.I3vrRightController;
        if (ControllerDataSource == DataSource.Left)
        {
            controller = I3vrControllerManager.I3vrLeftController;
        }
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
        if (controller.IsTouching)
        {
            TouchPadPos.gameObject.SetActive(true);
            TouchPosPoint.Set(controller.TouchPos.x, -controller.TouchPos.y, 0);
            TouchPadPos.rectTransform.localPosition = StartPoint + TouchPosPoint * 108;
        }
        else TouchPadPos.gameObject.SetActive(false);
    }
}
