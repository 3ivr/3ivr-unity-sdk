/*
 * Copyright (C) 2017 3ivr. All rights reserved.
 *
 * Author: Lucas(Wu Pengcheng)
 * Date  : 2017/06/19 08:08
 */

using UnityEngine;
using System.Collections;
using UnityEngine.UI;
namespace i3vr
{
    public class ShowData : MonoBehaviour
    {
        private Image Touch, Home, App, Switch, Trigger, LeftGesture, RightGesture, UpGesture, DownGesture;
        private Text Rotation_Pitch, Rotation_Yaw, Rotation_Roll, Gyro_Pitch, Gyro_Yaw, Gyro_Roll, Accele_Pitch, Accele_Yaw, Accele_Roll, Touch_Pitch,
        Touch_Yaw, ConnectionStatus, Frame;
        private Color Green = Color.green;
        private Color Red = Color.red;
        private Transform ButtonState, RawData;
        private Vector2 TouchOrigin;
        private float TouchPointMoveScope = 243;
        private Vector2 TouchPosV2 = Vector2.zero;
        private Vector3 RawOriV3 = Vector3.zero;
        private WaitForSeconds waitForSeconds = new WaitForSeconds(0.1f);
        private float CurrentTime;
        private I3vrController controller;
        private long RightPreviouslyFrame;
        private long RightCurrenFrame;
        private long LefrPreviouslyFrame;
        private long LeftCurrenFrame;

        public DataSource ControllerDataSource = DataSource.Right;
        // Use this for initialization
        void Start()
        {
            controller = I3vrControllerManager.I3vrRightController;
            if (ControllerDataSource == DataSource.Left)
            {
                controller = I3vrControllerManager.I3vrLeftController;
            }

            ButtonState = transform.FindChild("ButtonState");
            RawData = transform.FindChild("RawData");

            Touch = ButtonState.FindChild("Front").FindChild("Touch").GetComponent<Image>();
            Home = ButtonState.FindChild("Front").FindChild("Home").GetComponent<Image>();
            App = ButtonState.FindChild("Front").FindChild("App").GetComponent<Image>();
            Switch = ButtonState.FindChild("Front").FindChild("Switch").GetComponent<Image>();
            Trigger = ButtonState.FindChild("Side").FindChild("Trigger").GetComponent<Image>();
            LeftGesture = ButtonState.FindChild("Front").FindChild("LeftGesture").GetComponent<Image>();
            RightGesture = ButtonState.FindChild("Front").FindChild("RightGesture").GetComponent<Image>();
            UpGesture = ButtonState.FindChild("Front").FindChild("UpGesture").GetComponent<Image>();
            DownGesture = ButtonState.FindChild("Front").FindChild("DownGesture").GetComponent<Image>();

            Rotation_Pitch = RawData.FindChild("Rotation").FindChild("Pitch").FindChild("Input").GetComponent<Text>();
            Rotation_Yaw = RawData.FindChild("Rotation").FindChild("Yaw").FindChild("Input").GetComponent<Text>();
            Rotation_Roll = RawData.FindChild("Rotation").FindChild("Roll").FindChild("Input").GetComponent<Text>();

            Gyro_Pitch = RawData.FindChild("Gyro").FindChild("Pitch").FindChild("Input").GetComponent<Text>();
            Gyro_Yaw = RawData.FindChild("Gyro").FindChild("Yaw").FindChild("Input").GetComponent<Text>();
            Gyro_Roll = RawData.FindChild("Gyro").FindChild("Roll").FindChild("Input").GetComponent<Text>();

            Accele_Pitch = RawData.FindChild("Accele").FindChild("Pitch").FindChild("Input").GetComponent<Text>();
            Accele_Yaw = RawData.FindChild("Accele").FindChild("Yaw").FindChild("Input").GetComponent<Text>();
            Accele_Roll = RawData.FindChild("Accele").FindChild("Roll").FindChild("Input").GetComponent<Text>();

            Touch_Pitch = RawData.FindChild("Touch").FindChild("Pitch").FindChild("Input").GetComponent<Text>();
            Touch_Yaw = RawData.FindChild("Touch").FindChild("Yaw").FindChild("Input").GetComponent<Text>();

            ConnectionStatus = RawData.FindChild("ConnectionStatus").GetComponent<Text>();
            TouchOrigin = Touch.rectTransform.localPosition;

            Frame = transform.FindChild("Frame").FindChild("Input").GetComponent<Text>();
        }

        // Update is called once per frame
        void Update()
        {
            UpdateState();
        }

        void UpdateState()
        {
            ShowFrame();

            Quaternion RawOriQua = controller.Orientation;
            RawOriV3 = RawOriQua.eulerAngles;
            Rotation_Pitch.text = RawOriV3.x.ToString("f2");
            Rotation_Yaw.text = RawOriV3.y.ToString("f2");
            Rotation_Roll.text = RawOriV3.z.ToString("f2");

            Gyro_Pitch.text = controller.Gyro.x.ToString("f2");
            Gyro_Yaw.text = controller.Gyro.y.ToString("f2");
            Gyro_Roll.text = controller.Gyro.z.ToString("f2");

            Accele_Pitch.text = controller.Accel.x.ToString("f2");
            Accele_Yaw.text = controller.Accel.y.ToString("f2");
            Accele_Roll.text = controller.Accel.z.ToString("f2");

            Touch_Pitch.text = controller.TouchPos.x.ToString("f2");
            Touch_Yaw.text = controller.TouchPos.y.ToString("f2");
            TouchPosV2.Set(controller.TouchPos.x, -controller.TouchPos.y);

            if (controller.IsTouching)
            {
                Touch.gameObject.SetActive(true);
                Touch.color = Green;
            }
            if (!controller.IsTouching)
            {
                Touch.gameObject.SetActive(false);
            }

            if (controller.AppButtonDown)
            {
                App.color = Green;
            }
            if (controller.AppButtonUp)
            {
                App.color = Red;
            }

            if (controller.TriggerButtonDown)
            {
                Trigger.color = Green;
            }
            if (controller.TriggerButtonUp)
            {
                Trigger.color = Red;
            }

            if (controller.HomeButtonDown)
            {
                Home.color = Green;
            }
            if (controller.HomeButtonUp)
            {
                Home.color = Red;
            }

            if (controller.SwitchButtonDown)
            {
                Switch.color = Green;
            }
            if (controller.SwitchButtonUp)
            {
                Switch.color = Red;
            }

            if (controller.TouchGestureLeft)
            {
                LeftGesture.gameObject.SetActive(true);
                StartCoroutine(Conceal(LeftGesture.gameObject));
            }
            if (controller.TouchGestureRight)
            {
                RightGesture.gameObject.SetActive(true);
                StartCoroutine(Conceal(RightGesture.gameObject));
            }
            if (controller.TouchGestureUp)
            {
                UpGesture.gameObject.SetActive(true);
                StartCoroutine(Conceal(UpGesture.gameObject));
            }
            if (controller.TouchGestureDown)
            {
                DownGesture.gameObject.SetActive(true);
                StartCoroutine(Conceal(DownGesture.gameObject));
            }

            Touch.rectTransform.localPosition = TouchOrigin + TouchPosV2 * TouchPointMoveScope;

            ConnectionStatus.text = controller.ConnectionState.ToString();
        }

        void ShowFrame()
        {
            CurrentTime += Time.deltaTime;
            if (CurrentTime > 1)
            {
                if (I3vrControllerManager.I3vrControllerNumb == ControllerType.LeftAndRight)
                {
                    if (ControllerDataSource == DataSource.Right)
                    {
                        RightPreviouslyFrame = RightCurrenFrame;
                        RightCurrenFrame = AndroidDoubleServiceProvider.rightFrame;
                        Frame.text = (RightCurrenFrame - RightPreviouslyFrame).ToString();
                    }
                    else
                    {
                        LefrPreviouslyFrame = LeftCurrenFrame;
                        LeftCurrenFrame = AndroidDoubleServiceProvider.leftFrame;
                        Frame.text = (LeftCurrenFrame - LefrPreviouslyFrame).ToString();
                    }
                }
                else
                {
                    RightPreviouslyFrame = RightCurrenFrame;
                    RightCurrenFrame = AndroidServiceProvider.rightFrame;
                    Frame.text = (RightCurrenFrame - RightPreviouslyFrame).ToString();
                }
                CurrentTime = 0;
            }
        }

        IEnumerator Conceal(GameObject obj)
        {
            yield return waitForSeconds;
            obj.gameObject.SetActive(false);
        }
    }

}
