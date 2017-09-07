/*
 * Copyright (C) 2017 3ivr. All rights reserved.
 *
 * Author: Lucas(Wu Pengcheng)
 * Date  : 2017/06/19 08:08
 */

using UnityEngine;
using UnityEngine.UI;

namespace i3vr
{
    public class DisplayTooltips : MonoBehaviour
    {
        public bool isDisplayUI;
        public float minAngles_x;
        public float maxAngles_x;
        public bool isRightSource;

        private I3vrController controller;
        private GameObject ButtonTooltips;
        private Image TouchPadPos;
        private Vector3 StartPoint;
        private Vector3 TouchPosPoint;

        private void Awake()
        {
            ButtonTooltips = transform.FindChild("Tooltip").FindChild("ButtonTooltip").gameObject;
            TouchPadPos = transform.FindChild("Tooltip").FindChild("TouchPoint").GetComponent<Image>();
            ButtonTooltips.SetActive(false);
            StartPoint = TouchPadPos.rectTransform.localPosition;
        }
        private void Start()
        {
            controller = I3vrControllerManager.RightController;
            if (!isRightSource)
            {
                controller = I3vrControllerManager.LeftController;
            }
        }

        // Update is called once per frame
        void Update()
        {
            if (isDisplayUI)
            {
                if (transform.localRotation.eulerAngles.x < maxAngles_x && transform.localRotation.eulerAngles.x > minAngles_x)
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
}
