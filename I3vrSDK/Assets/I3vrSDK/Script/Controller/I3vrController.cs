/*
 * Copyright (C) 2017 3ivr. All rights reserved.
 *
 * Author: Lucas(Wu Pengcheng)
 * Date  : 2017/06/19 08:08
 */

using UnityEngine;
using System.Collections;
using i3vr;

/// Represents the controller's current connection state.
/// All values and semantics below (except for Error) are
/// from I3VR API.
public enum I3vrConnectionState
{
    /// Indicates that an error has occurred.
    Error = -1,
    /// Indicates that the controller is disconnected.
    Disconnected = 0,
    /// Indicates that the device is scanning for controllers.
    Scanning = 1,
    /// Indicates that the device is connecting to a controller.
    Connecting = 2,
    /// Indicates that the device is connected to a controller.
    Connected = 3,
};

// Represents the API status of the current controller state.
public enum I3vrControllerApiStatus
{
    // A Unity-localized error occurred.
    Error = -1,
    // API is happy and healthy. This doesn't mean the controller itself
    // is connected, it just means that the underlying service is working
    // properly.
    Ok = 0,
    /// Any other status represents a permanent failure that requires
    /// external action to fix:
    /// API failed because this device does not support controllers (API is too
    /// low, or other required feature not present).
    Unsupported = 1,
    /// This app was not authorized to use the service (e.g., missing permissions,
    /// the app is blacklisted by the underlying service, etc).
    NotAuthorized = 2,
    /// The underlying VR service is not present.
    Unavailable = 3,
    /// The underlying VR service is too old, needs upgrade.
    ApiServiceObsolete = 4,
    /// The underlying VR service is too new, is incompatible with current client.
    ApiClientObsolete = 5,
    /// The underlying VR service is malfunctioning. Try again later.
    ApiMalfunction = 6,
};


/// Main entry point for the I3vr controller API.
///
/// To use this API, add this behavior to a GameObject in your scene, or use the
/// I3vrControllerMain prefab. There can only be one object with this behavior on your scene.
///
/// This is a singleton object.
///
/// To access the controller state, simply read the static properties of this class. For example,
/// to know the controller's current orientation, use I3vrController.Orientation.
public class I3vrController : MonoBehaviour
{
    private static I3vrController instance;
    private static IControllerProvider controllerProvider;

    private ControllerState controllerState = new ControllerState();
    private IEnumerator controllerUpdate;
    private WaitForEndOfFrame waitForEndOfFrame = new WaitForEndOfFrame();

    /// Event handler for receiving button, track pad, and IMU updates from the controller.
    public delegate void OnControllerUpdateEvent();
    public event OnControllerUpdateEvent OnControllerUpdate;

    public delegate void OnHeadsetRecenter();
    public event OnHeadsetRecenter HeadsetRecenter;

    /// Returns the arm model instance associated with the controller.
    public static I3vrArmModel ArmModel
    {
        get
        {
            return instance != null ? instance.GetComponent<I3vrArmModel>() : null;
        }
    }

    /// Returns the controller's current connection state.
    public static I3vrConnectionState ConnectionState
    {
        get
        {
            return instance != null ? instance.controllerState.connectionState : I3vrConnectionState.Error;
        }
    }

    /// Returns the API status of the current controller state.
    public static I3vrControllerApiStatus ApiStatus
    {
        get
        {
            return instance != null ? instance.controllerState.apiStatus : I3vrControllerApiStatus.Error;
        }
    }

    /// Returns the controller's current orientation in space, as a quaternion.
    /// The space in which the orientation is represented is the usual Unity space, with
    /// X pointing to the right, Y pointing up and Z pointing forward. Therefore, to make an
    /// object in your scene have the same orientation as the controller, simply assign this
    /// quaternion to the GameObject's transform.rotation.
    public static Quaternion Orientation
    {
        get
        {
            return instance != null ? instance.controllerState.orientation : Quaternion.identity;
        }
    }

    /// Returns the controller's gyroscope reading. The gyroscope indicates the angular
    /// about each of its local axes. The controller's axes are: X points to the right,
    /// Y points perpendicularly up from the controller's top surface and Z lies
    /// along the controller's body, pointing towards the front. The angular speed is given
    /// in radians per second, using the right-hand rule (positive means a right-hand rotation
    /// about the given axis).
    public static Vector3 Gyro
    {
        get
        {
            return instance != null ? instance.controllerState.gyro : Vector3.zero;
        }
    }

    /// Returns the controller's accelerometer reading. The accelerometer indicates the
    /// effect of acceleration and gravity in the direction of each of the controller's local
    /// axes. The controller's local axes are: X points to the right, Y points perpendicularly
    /// up from the controller's top surface and Z lies along the controller's body, pointing
    /// towards the front. The acceleration is measured in meters per second squared. Note that
    /// gravity is combined with acceleration, so when the controller is resting on a table top,
    /// it will measure an acceleration of 9.8 m/s^2 on the Y axis. The accelerometer reading
    /// will be zero on all three axes only if the controller is in free fall, or if the user
    /// is in a zero gravity environment like a space station.
    public static Vector3 Accel
    {
        get
        {
            return instance != null ? instance.controllerState.accel : Vector3.zero;
        }
    }

    /// If true, the user is currently touching the controller's touchpad.
    public static bool IsTouching
    {
        get
        {
            return instance != null ? instance.controllerState.isTouching : false;
        }
    }

    /// If true, the user just started touching the touchpad. This is an event flag (it is true
    /// for only one frame after the event happens, then reverts to false).
    public static bool TouchDown
    {
        get
        {
            return instance != null ? instance.controllerState.touchDown : false;
        }
    }

    /// If true, the user just stopped touching the touchpad. This is an event flag (it is true
    /// for only one frame after the event happens, then reverts to false).
    public static bool TouchUp
    {
        get
        {
            return instance != null ? instance.controllerState.touchUp : false;
        }
    }

    public static Vector2 TouchPos
    {
        get
        {
            return instance != null ? instance.controllerState.touchPos : Vector2.zero;
        }
    }

    /// If true, the user just completed the recenter gesture. The controller's orientation is
    /// now being reported in the new recentered coordinate system (the controller's orientation
    /// when recentering was completed was remapped to mean "forward"). This is an event flag
    /// (it is true for only one frame after the event happens, then reverts to false).
    /// The headset is recentered together with the controller.
    public static bool Recentered
    {
        get
        {
            return instance != null ? instance.controllerState.recentered : false;
        }
    }

    /// If true, the trigger button (touchpad button) is currently being pressed. This is not
    /// an event: it represents the button's state (it remains true while the button is being
    /// pressed).
    public static bool TriggerButton
    {
        get
        {
            return instance != null ? instance.controllerState.triggerButtonState : false;
        }
    }

    /// If true, the trigger button was just pressed. This is an event flag: it will be true for
    /// only one frame after the event happens.
    public static bool TriggerButtonDown
    {
        get
        {
            return instance != null ? instance.controllerState.triggerButtonDown : false;
        }
    }

    /// If true, the trigger button was just released. This is an event flag: it will be true for
    /// only one frame after the event happens.
    public static bool TriggerButtonUp
    {
        get
        {
            return instance != null ? instance.controllerState.triggerButtonUp : false;
        }
    }

    /// If true, the app button (touchpad button) is currently being pressed. This is not
    /// an event: it represents the button's state (it remains true while the button is being
    /// pressed).
    public static bool AppButton
    {
        get
        {
            return instance != null ? instance.controllerState.appButtonState : false;
        }
    }

    /// If true, the app button was just pressed. This is an event flag: it will be true for
    /// only one frame after the event happens.
    public static bool AppButtonDown
    {
        get
        {
            return instance != null ? instance.controllerState.appButtonDown : false;
        }
    }

    /// If true, the app button was just released. This is an event flag: it will be true for
    /// only one frame after the event happens.
    public static bool AppButtonUp
    {
        get
        {
            return instance != null ? instance.controllerState.appButtonUp : false;
        }
    }
    /// If true, the home button (touchpad button) is currently being pressed. This is not
    /// an event: it represents the button's state (it remains true while the button is being
    /// pressed).
    public static bool HomeButton
    {
        get
        {
            return instance != null ? instance.controllerState.homeButtonState : false;
        }
    }

    /// If true, the home button was just pressed. This is an event flag: it will be true for
    /// only one frame after the event happens.
    public static bool HomeButtonDown
    {
        get
        {
            return instance != null ? instance.controllerState.homeButtonDown : false;
        }
    }

    /// If true, the home button was just released. This is an event flag: it will be true for
    /// only one frame after the event happens.
    public static bool HomeButtonUp
    {
        get
        {
            return instance != null ? instance.controllerState.homeButtonUp : false;
        }
    }

    /// If true, the switch button (touchpad button) is currently being pressed. This is not
    /// an event: it represents the button's state (it remains true while the button is being
    /// pressed).
    public static bool SwitchButton
    {
        get
        {
            return instance != null ? instance.controllerState.switchButtonState : false;
        }
    }

    /// If true, the switch button was just pressed. This is an event flag: it will be true for
    /// only one frame after the event happens.
    public static bool SwitchButtonDown
    {
        get
        {
            return instance != null ? instance.controllerState.switchButtonDown : false;
        }
    }

    /// If true, the switch button was just released. This is an event flag: it will be true for
    /// only one frame after the event happens.
    public static bool SwitchButtonUp
    {
        get
        {
            return instance != null ? instance.controllerState.switchButtonUp : false;
        }
    }

    /// If true, the touch pad's left direction. This is an event flag: 
    /// it will be true for only one frame after the event happens.
    public static bool TouchGestureLeft
    {
        get
        {
            return instance != null ? instance.controllerState.touchGestureLeft : false;
        }
    }

    /// If true, the touch pad's right direction. This is an event flag: 
    /// it will be true for only one frame after the event happens.
    public static bool TouchGestureRight
    {
        get
        {
            return instance != null ? instance.controllerState.touchGestureRight : false;
        }
    }

    /// If true, the touch pad's up direction. This is an event flag: 
    /// it will be true for only one frame after the event happens.
    public static bool TouchGestureUp
    {
        get
        {
            return instance != null ? instance.controllerState.touchGestureUp : false;
        }
    }

    /// If true, the touch pad's down direction. This is an event flag: 
    /// it will be true for only one frame after the event happens.
    public static bool TouchGestureDown
    {
        get
        {
            return instance != null ? instance.controllerState.touchGestureDown : false;
        }
    }

    /// If State == I3vrConnectionState.Error, this contains details about the error.
    public static string ErrorDetails
    {
        get
        {
            if (instance != null)
            {
                return instance.controllerState.connectionState == I3vrConnectionState.Error ?
                    instance.controllerState.errorDetails : "";
            }
            else
            {
                return "I3vrController instance not found in scene. It may be missing, or it might "
                    + "not have initialized yet.";
            }
        }
    }


    void Awake()
    {
        if (instance != null)
        {
            Debug.LogError("More than one I3vrController instance was found in your scene. "
                + "Ensure that there is only one I3vrController.");
            this.enabled = false;
            return;
        }
        instance = this;
        if (controllerProvider == null)
        {
            controllerProvider = ControllerProviderFactory.CreateControllerProvider(this);
        }
        Screen.sleepTimeout = SleepTimeout.NeverSleep;
    }

    void OnDestroy()
    {
        instance = null;
    }
    
    private void UpdateController()
    {
        controllerProvider.ReadState(controllerState);
        if (controllerState.headsetRecenterRequested)
        {
            if (HeadsetRecenter!=null) {
                HeadsetRecenter();
            }         
            controllerState.headsetRecenterRequested = false;
        }
    }

    void OnApplicationPause(bool paused)
    {
        if (null == controllerProvider) return;
        if (paused)
        {
            controllerProvider.OnPause();
        }
        else
        {
            controllerProvider.OnResume();
        }
    }

    void OnEnable()
    {
        controllerUpdate = EndOfFrame();
        StartCoroutine(controllerUpdate);
    }

    void OnDisable()
    {
        StopCoroutine(controllerUpdate);
    }

    IEnumerator EndOfFrame()
    {
        while (true)
        {
            // This must be done at the end of the frame to ensure that all GameObjects had a chance
            // to read transient controller state (e.g. events, etc) for the current frame before
            // it gets reset.
            yield return waitForEndOfFrame;
            UpdateController();
            if (OnControllerUpdate != null)
            {
                OnControllerUpdate();
            }
        }
    }
}

