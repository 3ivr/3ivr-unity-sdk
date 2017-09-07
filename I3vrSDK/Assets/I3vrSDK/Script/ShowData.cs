/*
 * Copyright (C) 2017 3ivr. All rights reserved.
 *
 * Author: Lucas(Wu Pengcheng)
 * Date  : 2017/06/19 08:08
 */
//#if UNITY_ANDROID&&!UNITY_EDITOR
using UnityEngine;
using System.Collections;
using UnityEngine.UI;

namespace i3vr
{
    public class ShowData : MonoBehaviour
    {
        private Image touch, _return, app, home, trigger, leftGesture, rightGesture, upGesture, downGesture;
        private Text rotation_Pitch, rotation_Yaw, rotation_Roll, gyro_Pitch, gyro_Yaw, gyro_Roll, accele_Pitch, accele_Yaw, accele_Roll, touch_Pitch,
        touch_Yaw, connectionStatus, frame, deviceName, macAddress, manufacturerName, modelNumber, serialNumber, hardwareRevision, firmwareRevision, softwareRevision;
        private Color green = Color.green;
        private Color red = Color.red;
        private Transform buttonState, rawData, deviceInfo;
        private Vector2 touchOrigin;
        private float touchPointMoveScope = 243;
        private Vector2 touchPosV2 = Vector2.zero;
        private Vector3 rawOriV3 = Vector3.zero;
        private WaitForSeconds waitForSeconds = new WaitForSeconds(0.1f);
        private float currentTime;
        private I3vrController controller;
        private long rightPreviouslyFrame;
        private long rightCurrenFrame;
        private long lefrPreviouslyFrame;
        private long leftCurrenFrame;

        public bool isRightSource;

        // Use this for initialization
        void Start()
        {
            controller = I3vrControllerManager.RightController;
            if (!isRightSource) {
                controller = I3vrControllerManager.LeftController;
            }

            buttonState = transform.FindChild("ButtonState");
            rawData = transform.FindChild("RawData");
            deviceInfo = rawData.FindChild("DeviceInfo");

            deviceName = deviceInfo.FindChild("DeviceName").FindChild("Input").GetComponent<Text>();
            macAddress = deviceInfo.FindChild("MacAddress").FindChild("Input").GetComponent<Text>();
            manufacturerName = deviceInfo.FindChild("ManufacturerName").FindChild("Input").GetComponent<Text>();
            modelNumber = deviceInfo.FindChild("ModelNumber").FindChild("Input").GetComponent<Text>();
            serialNumber = deviceInfo.FindChild("SerialNumber").FindChild("Input").GetComponent<Text>();
            hardwareRevision = deviceInfo.FindChild("HardwareRevision").FindChild("Input").GetComponent<Text>();
            firmwareRevision = deviceInfo.FindChild("FirmwareRevision").FindChild("Input").GetComponent<Text>();
            softwareRevision = deviceInfo.FindChild("SoftwareRevision").FindChild("Input").GetComponent<Text>();

            touch = buttonState.FindChild("Front").FindChild("Touch").GetComponent<Image>();
            _return = buttonState.FindChild("Front").FindChild("Return").GetComponent<Image>();
            app = buttonState.FindChild("Front").FindChild("App").GetComponent<Image>();
            home = buttonState.FindChild("Front").FindChild("Home").GetComponent<Image>();
            trigger = buttonState.FindChild("Side").FindChild("Trigger").GetComponent<Image>();
            leftGesture = buttonState.FindChild("Front").FindChild("LeftGesture").GetComponent<Image>();
            rightGesture = buttonState.FindChild("Front").FindChild("RightGesture").GetComponent<Image>();
            upGesture = buttonState.FindChild("Front").FindChild("UpGesture").GetComponent<Image>();
            downGesture = buttonState.FindChild("Front").FindChild("DownGesture").GetComponent<Image>();

            rotation_Pitch = rawData.FindChild("Rotation").FindChild("Pitch").FindChild("Input").GetComponent<Text>();
            rotation_Yaw = rawData.FindChild("Rotation").FindChild("Yaw").FindChild("Input").GetComponent<Text>();
            rotation_Roll = rawData.FindChild("Rotation").FindChild("Roll").FindChild("Input").GetComponent<Text>();

            gyro_Pitch = rawData.FindChild("Gyro").FindChild("Pitch").FindChild("Input").GetComponent<Text>();
            gyro_Yaw = rawData.FindChild("Gyro").FindChild("Yaw").FindChild("Input").GetComponent<Text>();
            gyro_Roll = rawData.FindChild("Gyro").FindChild("Roll").FindChild("Input").GetComponent<Text>();

            accele_Pitch = rawData.FindChild("Accele").FindChild("Pitch").FindChild("Input").GetComponent<Text>();
            accele_Yaw = rawData.FindChild("Accele").FindChild("Yaw").FindChild("Input").GetComponent<Text>();
            accele_Roll = rawData.FindChild("Accele").FindChild("Roll").FindChild("Input").GetComponent<Text>();

            touch_Pitch = rawData.FindChild("Touch").FindChild("Pitch").FindChild("Input").GetComponent<Text>();
            touch_Yaw = rawData.FindChild("Touch").FindChild("Yaw").FindChild("Input").GetComponent<Text>();

            connectionStatus = rawData.FindChild("ConnectionStatus").GetComponent<Text>();
            touchOrigin = touch.rectTransform.localPosition;

            frame = transform.FindChild("Frame").FindChild("Input").GetComponent<Text>();
        }

        // Update is called once per frame
        void Update()
        {
            UpdateState();
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                Application.Quit();
            }
        }

        void UpdateState()
        {
            ShowFrame();

            Quaternion RawOriQua = controller.Orientation;
            rawOriV3 = RawOriQua.eulerAngles;
            rotation_Pitch.text = rawOriV3.x.ToString("f2");
            rotation_Yaw.text = rawOriV3.y.ToString("f2");
            rotation_Roll.text = rawOriV3.z.ToString("f2");

            gyro_Pitch.text = controller.Gyro.x.ToString("f2");
            gyro_Yaw.text = controller.Gyro.y.ToString("f2");
            gyro_Roll.text = controller.Gyro.z.ToString("f2");

            accele_Pitch.text = controller.Accel.x.ToString("f2");
            accele_Yaw.text = controller.Accel.y.ToString("f2");
            accele_Roll.text = controller.Accel.z.ToString("f2");

            touch_Pitch.text = controller.TouchPos.x.ToString("f2");
            touch_Yaw.text = controller.TouchPos.y.ToString("f2");
            touchPosV2.Set(controller.TouchPos.x, -controller.TouchPos.y);

            if (controller.IsTouching)
            {
                touch.gameObject.SetActive(true);
                touch.color = green;
            }
            if (!controller.IsTouching)
            {
                touch.gameObject.SetActive(false);
            }

            if (controller.AppButtonDown)
            {
                app.color = green;
            }
            if (controller.AppButtonUp)
            {
                app.color = red;
            }

            if (controller.TriggerButtonDown)
            {
                trigger.color = green;
            }
            if (controller.TriggerButtonUp)
            {
                trigger.color = red;
            }

            if (controller.ReturnButtonDown)
            {
                _return.color = green;
            }
            if (controller.ReturnButtonUp)
            {
                _return.color = red;
            }

            if (controller.HomeButtonDown)
            {
                home.color = green;
            }
            if (controller.HomeButtonUp)
            {
                home.color = red;
            }

            if (controller.TouchGestureLeft)
            {
                leftGesture.gameObject.SetActive(true);
                StartCoroutine(Conceal(leftGesture.gameObject));
            }
            if (controller.TouchGestureRight)
            {
                rightGesture.gameObject.SetActive(true);
                StartCoroutine(Conceal(rightGesture.gameObject));
            }
            if (controller.TouchGestureUp)
            {
                upGesture.gameObject.SetActive(true);
                StartCoroutine(Conceal(upGesture.gameObject));
            }
            if (controller.TouchGestureDown)
            {
                downGesture.gameObject.SetActive(true);
                StartCoroutine(Conceal(downGesture.gameObject));
            }

            touch.rectTransform.localPosition = touchOrigin + touchPosV2 * touchPointMoveScope;

            connectionStatus.text = controller.ConnectionState.ToString();

            ShowInfo();

            //Debug.Log(AndroidServiceControllerProvider.GetInterval(0));
        }

        void ShowFrame()
        {
            currentTime += Time.deltaTime;
            if (currentTime > 1)
            {
                if (isRightSource)
                {
                    rightPreviouslyFrame = rightCurrenFrame;
                    rightCurrenFrame = AndroidServiceControllerProvider.GetFrameNumber(0);
                    frame.text = (rightCurrenFrame - rightPreviouslyFrame).ToString();
                }
                else
                {
                    lefrPreviouslyFrame = leftCurrenFrame;
                    leftCurrenFrame = AndroidServiceControllerProvider.GetFrameNumber(1);
                    frame.text = (leftCurrenFrame - lefrPreviouslyFrame).ToString();
                }
                currentTime = 0;
            }
        }

        IEnumerator Conceal(GameObject obj)
        {
            yield return waitForSeconds;
            obj.gameObject.SetActive(false);
        }

        void ShowInfo()
        {
            if (controller.ConnectionState == I3vrConnectionState.Connected)
            {
                if (isRightSource)
                {
                    SetDoubleDeviceIndex(0);
                }
                else
                    SetDoubleDeviceIndex(1);
            }
        }

        void SetDoubleDeviceIndex(int index)
        {
            deviceName.text = AndroidServiceControllerProvider.GetDeviceName();
            macAddress.text = AndroidServiceControllerProvider.GetMacAddress(index);
            manufacturerName.text = AndroidServiceControllerProvider.GetManufacturerName(index);
            modelNumber.text = AndroidServiceControllerProvider.GetModelNumber(index);
            serialNumber.text = AndroidServiceControllerProvider.GetSerialNumber(index);
            hardwareRevision.text = AndroidServiceControllerProvider.GetHardwareRevision(index);
            firmwareRevision.text = AndroidServiceControllerProvider.GetFirmwareRevision(index);
            softwareRevision.text = AndroidServiceControllerProvider.GetSoftwareRevision(index);
        }
    }
}
//#endif