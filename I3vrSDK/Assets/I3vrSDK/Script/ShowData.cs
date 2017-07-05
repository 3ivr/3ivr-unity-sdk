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
        Image Touch, Home, App, Switch, Trigger, LeftGesture, RightGesture, UpGesture, DownGesture;
        Text Rotation_Pitch, Rotation_Yaw, Rotation_Roll, Gyro_Pitch, Gyro_Yaw, Gyro_Roll, Accele_Pitch, Accele_Yaw, Accele_Roll, Touch_Pitch,
        Touch_Yaw, ConnectionStatus;
        Color Green = Color.green;
        Color Red = Color.red;
        Transform ButtonState, RawData;
        Vector2 TouchOrigin;
        float TouchPointMoveScope = 243;
        Vector2 TouchPosV2 = Vector2.zero;
        Vector3 RawOriV3 = Vector3.zero;
        WaitForSeconds waitForSeconds = new WaitForSeconds(0.1f);

        // Use this for initialization
        void Start()
        {
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

            ConnectionStatus= RawData.FindChild("ConnectionStatus").GetComponent<Text>();
            TouchOrigin = Touch.rectTransform.localPosition;
        }

        private void OnApplicationQuit()
        {
            AndroidServiceProvider.BleRelease();
        }

        // Update is called once per frame
        void Update()
        {
            UpdateState();
        }

        void UpdateState()
        {
            Quaternion RawOriQua = I3vrController.Orientation;
            RawOriV3 = RawOriQua.eulerAngles;
            Rotation_Pitch.text = RawOriV3.x.ToString("f2");
            Rotation_Yaw.text = RawOriV3.y.ToString("f2");
            Rotation_Roll.text = RawOriV3.z.ToString("f2");

            Gyro_Pitch.text = I3vrController.Gyro.x.ToString("f2");
            Gyro_Yaw.text = I3vrController.Gyro.y.ToString("f2");
            Gyro_Roll.text = I3vrController.Gyro.z.ToString("f2");

            Accele_Pitch.text = I3vrController.Accel.x.ToString("f2");
            Accele_Yaw.text = I3vrController.Accel.y.ToString("f2");
            Accele_Roll.text = I3vrController.Accel.z.ToString("f2");

            Touch_Pitch.text = I3vrController.TouchPos.x.ToString("f2");
            Touch_Yaw.text = I3vrController.TouchPos.y.ToString("f2");
            TouchPosV2.Set(I3vrController.TouchPos.x, -I3vrController.TouchPos.y);

            if (I3vrController.TouchDown)
            {
                Touch.gameObject.SetActive(true);
                Touch.color = Green;
            }
            if (I3vrController.TouchUp)
            {
                Touch.gameObject.SetActive(false);
            }

            if (I3vrController.AppButtonDown)
            {
                App.color = Green;
            }
            if (I3vrController.AppButtonUp)
            {
                App.color = Red;
            }

            if (I3vrController.TriggerButtonDown)
            {
                Trigger.color = Green;
            }
            if (I3vrController.TriggerButtonUp)
            {
                Trigger.color = Red;
            }

            if (I3vrController.HomeButtonDown)
            {
                Home.color = Green;
            }
            if (I3vrController.HomeButtonUp)
            {
                Home.color = Red;
            }

            if (I3vrController.SwitchButtonDown)
            {
                Switch.color = Green;
            }
            if (I3vrController.SwitchButtonUp)
            {
                Switch.color = Red;
            }

            if (I3vrController.TouchGestureLeft)
            {
                LeftGesture.gameObject.SetActive(true);
                StartCoroutine(Conceal(LeftGesture.gameObject));
            }
            if (I3vrController.TouchGestureRight)
            {
                RightGesture.gameObject.SetActive(true);
                StartCoroutine(Conceal(RightGesture.gameObject));
            }
            if (I3vrController.TouchGestureUp)
            {
                UpGesture.gameObject.SetActive(true);
                StartCoroutine(Conceal(UpGesture.gameObject));
            }
            if (I3vrController.TouchGestureDown)
            {
                DownGesture.gameObject.SetActive(true);
                StartCoroutine(Conceal(DownGesture.gameObject));
            }
           
            Touch.rectTransform.localPosition = TouchOrigin + TouchPosV2 * TouchPointMoveScope;

            ConnectionStatus.text = I3vrController.ConnectionState.ToString();

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                Application.Quit();
            }
        }

        IEnumerator Conceal(GameObject obj)
        {
            yield return waitForSeconds;
            obj.gameObject.SetActive(false);
        }
    }
}
