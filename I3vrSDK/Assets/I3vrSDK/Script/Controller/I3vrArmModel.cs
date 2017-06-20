/*
 * Copyright (C) 2017 3ivr. All rights reserved.
 *
 * Author: Lucas(Wu Pengcheng)
 * Date  : 2017/06/19 08:08
 */

using UnityEngine;
using i3vr;

/// The I3vrArmModel is a standard interface to interact with a scene with the controller.
/// It is responsible for:
/// -  Determining the orientation and location of the controller.
/// -  Predict the location of the shoulder, elbow, wrist, and pointer.
///
/// There should only be one instance in the scene, and it should be attached
/// to the I3vrController.
[RequireComponent(typeof(I3vrController))]
public class I3vrArmModel : MonoBehaviour
{
    private static I3vrArmModel instance = null;

    /// Initial relative location of the shoulder (meters).
    private static readonly Vector3 DEFAULT_SHOULDER_RIGHT = new Vector3(0.19f, -0.19f, -0.03f);

    /// The range of movement from the elbow position due to accelerometer (meters).
    private static readonly Vector3 ELBOW_MIN_RANGE = new Vector3(-0.05f, -0.1f, 0.0f);
    private static readonly Vector3 ELBOW_MAX_RANGE = new Vector3(0.05f, 0.1f, 0.2f);

    /// Offset of the laser pointer origin relative to the wrist (meters)
    private static readonly Vector3 POINTER_OFFSET = new Vector3(0.0f, -0.009f, 0.099f);

    /// Rest position parameters for arm model (meters).
    private static readonly Vector3 ELBOW_POSITION = new Vector3(0.195f, -0.5f, -0.075f);
    private static readonly Vector3 WRIST_POSITION = new Vector3(0.0f, 0.0f, 0.25f);
    private static readonly Vector3 ARM_EXTENSION_OFFSET = new Vector3(-0.13f, 0.14f, 0.08f);

    /// Strength of the acceleration filter (unitless).
    private const float GRAVITY_CALIB_STRENGTH = 0.999f;

    /// Strength of the velocity suppression (unitless).
    private const float VELOCITY_FILTER_SUPPRESS = 0.99f;

    /// Strength of the velocity suppression during low acceleration (unitless).
    private const float LOW_ACCEL_VELOCITY_SUPPRESS = 0.9f;

    /// Strength of the acceleration suppression during low velocity (unitless).
    private const float LOW_VELOCITY_ACCEL_SUPPRESS = 0.5f;

    /// The minimum allowable accelerometer reading before zeroing (m/s^2).
    private const float MIN_ACCEL = 1.0f;

    /// The expected force of gravity (m/s^2).
    private const float GRAVITY_FORCE = 9.807f;

    /// Amount of normalized alpha transparency to change per second.
    private const float DELTA_ALPHA = 4.0f;

    /// Angle ranges the for arm extension offset to start and end (degrees).
    private const float MIN_EXTENSION_ANGLE = 7.0f;
    private const float MAX_EXTENSION_ANGLE = 60.0f;

    /// Increases elbow bending as the controller moves up (unitless).
    private const float EXTENSION_WEIGHT = 0.4f;

    /// Offset of the elbow due to the accelerometer
    private Vector3 elbowOffset;

    /// Forward direction of the arm model.
    private Vector3 torsoDirection;

    /// Filtered velocity of the controller.
    private Vector3 filteredVelocity;

    /// Filtered acceleration of the controller.
    private Vector3 filteredAccel;

    /// Used to calibrate the ambient gravitational force.
    private Vector3 zeroAccel;

    /// Indicates if this is the first frame to receive new IMU measurements.
    private bool firstUpdate;

    /// Multiplier for handedness such that 1 = Right, 0 = Center, -1 = left.
    private Vector3 handedMultiplier;


    /// Use the I3vrController singleton to obtain a singleton for this class.
    public static I3vrArmModel Instance
    {
        get
        {
            if (instance == null)
            {
                instance = I3vrController.ArmModel;
            }
            return instance != null && instance.isActiveAndEnabled ? instance : null;
        }
    }

    /// Represents when gaze-following behavior should occur.
    public enum GazeBehavior
    {
        Never,        /// The shoulder will never follow the gaze.
        DuringMotion, /// The shoulder will follow the gaze during controller motion.
        Always        /// The shoulder will always follow the gaze.
    }

    /// Height of the elbow  (m).
    [Range(0.0f, 0.2f)]
    public float addedElbowHeight = 0.0f;

    /// Depth of the elbow  (m).
    [Range(0.0f, 0.2f)]
    public float addedElbowDepth = 0.0f;

    /// Downward tilt of the laser pointer relative to the controller (degrees).
    [Range(0.0f, 30.0f)]
    public float pointerTiltAngle = 15.0f;

    /// Controller distance from the face after which the alpha value decreases (meters).
    [Range(0.0f, 0.4f)]
    public float fadeDistanceFromFace = 0.32f;

    /// Determines if the shoulder should follow the gaze
    public GazeBehavior followGaze = GazeBehavior.Never;

    /// Determines if the accelerometer should be used.
    public bool useAccelerometer = false;

    /// Vector to represent the pointer's location.
    /// NOTE: This is in meatspace coordinates.
    public Vector3 pointerPosition { get; private set; }

    /// Quaternion to represent the pointer's rotation.
    /// NOTE: This is in meatspace coordinates.
    public Quaternion pointerRotation { get; private set; }

    /// Vector to represent the wrist's location.
    /// NOTE: This is in meatspace coordinates.
    public Vector3 wristPosition { get; private set; }

    /// Quaternion to represent the wrist's rotation.
    /// NOTE: This is in meatspace coordinates.
    public Quaternion wristRotation { get; private set; }

    /// Vector to represent the elbow's location.
    /// NOTE: This is in meatspace coordinates.
    public Vector3 elbowPosition { get; private set; }

    /// Quaternion to represent the elbow's rotation.
    /// NOTE: This is in meatspace coordinates.
    public Quaternion elbowRotation { get; private set; }

    /// Vector to represent the shoulder's location.
    /// NOTE: This is in meatspace coordinates.
    public Vector3 shoulderPosition { get; private set; }

    /// Vector to represent the shoulder's location.
    /// NOTE: This is in meatspace coordinates.
    public Quaternion shoulderRotation { get; private set; }

    /// The suggested rendering alpha value of the controller.
    /// This is to prevent the controller from intersecting face.
    public float alphaValue { get; private set; }

    void Start()
    {
        // Obtain the I3vr controller from the scene.
        I3vrController controller = GetComponent<I3vrController>();

        UpdateHandedness();

        // Register the controller update listener.
        controller.OnControllerUpdate += OnControllerUpdate;
        // Reset other relevant state.
        firstUpdate = true;
        elbowOffset = Vector3.zero;
        alphaValue = 1.0f;
        zeroAccel.Set(0, GRAVITY_FORCE, 0);
    }

    void OnDestroy()
    {
        // Unregister the controller update listener.
        I3vrController controller = GetComponent<I3vrController>();
        controller.OnControllerUpdate -= OnControllerUpdate;

        // Reset the singleton instance.
        instance = null;
    }

    void OnControllerUpdate()
    {
        UpdateHandedness();
        UpdateTorsoDirection();
        if (I3vrController.ConnectionState == I3vrConnectionState.Connected)
        {
            UpdateFromController();
        }
        else
        {
            ResetState();
        }
        if (useAccelerometer)
        {
            UpdateVelocity();
            TransformElbow();
        }
        else
        {
            elbowOffset = Vector3.zero;
        }
        ApplyArmModel();
        UpdateTransparency();
        UpdatePointer();
    }

    private void UpdateHandedness()
    {
        // Update user handedness if the setting has changed
        I3vrSettings.UserPrefsHandedness handedness = I3vrSettings.Handedness;

        // Determine handedness multiplier.
        handedMultiplier.Set(0, 1, 1);
        if (handedness == I3vrSettings.UserPrefsHandedness.Right)
        {
            handedMultiplier.x = 1.0f;
        }
        else if (handedness == I3vrSettings.UserPrefsHandedness.Left)
        {
            handedMultiplier.x = -1.0f;
        }

        // Place the shoulder in anatomical positions based on the height and handedness.
        shoulderRotation = Quaternion.identity;
        shoulderPosition = Vector3.Scale(DEFAULT_SHOULDER_RIGHT, handedMultiplier);
    }

    private Vector3 GetHeadOrientation()
    {
        return Camera.main.transform.rotation * Vector3.forward;
    }

    private void UpdateTorsoDirection()
    {
        // Ignore updates here if requested.
        if (followGaze == GazeBehavior.Never)
        {
            return;
        }

        // Determine the gaze direction horizontally.
        Vector3 gazeDirection = GetHeadOrientation();
        gazeDirection.y = 0.0f;
        gazeDirection.Normalize();

        // Use the gaze direction to update the forward direction.
        if (followGaze == GazeBehavior.Always)
        {
            torsoDirection = gazeDirection;
        }
        else if (followGaze == GazeBehavior.DuringMotion)
        {
            float angularVelocity = I3vrController.Gyro.magnitude;
            float gazeFilterStrength = Mathf.Clamp((angularVelocity - 0.2f) / 45.0f, 0.0f, 0.1f);
            torsoDirection = Vector3.Slerp(torsoDirection, gazeDirection, gazeFilterStrength);
        }

        // Rotate the fixed joints.
        Quaternion gazeRotation = Quaternion.FromToRotation(Vector3.forward, torsoDirection);
        shoulderRotation = gazeRotation;
        shoulderPosition = gazeRotation * shoulderPosition;
    }

    private void UpdateFromController()
    {
        // Get the orientation-adjusted acceleration.
        Vector3 accel = I3vrController.Orientation * I3vrController.Accel;
        accel *= GRAVITY_FORCE;

        // Very slowly calibrate gravity force out of acceleration.
        zeroAccel = zeroAccel * GRAVITY_CALIB_STRENGTH + accel * (1.0f - GRAVITY_CALIB_STRENGTH);
        filteredAccel = accel - zeroAccel;

        // If no tracking history, reset the velocity.
        if (firstUpdate)
        {
            filteredVelocity = Vector3.zero;
            firstUpdate = false;
        }

        // IMPORTANT: The accelerometer is not reliable at these low magnitudes
        // so ignore it to prevent drift.
        if (filteredAccel.magnitude < MIN_ACCEL)
        {
            // Suppress the acceleration.
            filteredAccel = Vector3.zero;
            filteredVelocity *= LOW_ACCEL_VELOCITY_SUPPRESS;
        }
        else
        {
            // If the velocity is decreasing, prevent snap-back by reducing deceleration.
            Vector3 newVelocity = filteredVelocity + filteredAccel * Time.deltaTime;
            if (newVelocity.sqrMagnitude < filteredVelocity.sqrMagnitude)
            {
                filteredAccel *= LOW_VELOCITY_ACCEL_SUPPRESS;
            }
        }
    }

    private void UpdateVelocity()
    {
        // Update the filtered velocity.
        filteredVelocity += filteredAccel * Time.deltaTime;
        filteredVelocity *= VELOCITY_FILTER_SUPPRESS;
    }

    private void ResetState()
    {
        // We've lost contact, quickly reset the state.
        filteredVelocity *= 0.5f;
        filteredAccel *= 0.5f;
        firstUpdate = true;
    }

    private void TransformElbow()
    {
        // Apply the filtered velocity to update the elbow offset position.
        if (useAccelerometer)
        {
            elbowOffset += filteredVelocity * Time.deltaTime;
            elbowOffset.x = Mathf.Clamp(elbowOffset.x, ELBOW_MIN_RANGE.x, ELBOW_MAX_RANGE.x);
            elbowOffset.y = Mathf.Clamp(elbowOffset.y, ELBOW_MIN_RANGE.y, ELBOW_MAX_RANGE.y);
            elbowOffset.z = Mathf.Clamp(elbowOffset.z, ELBOW_MIN_RANGE.z, ELBOW_MAX_RANGE.z);
        }
    }

    private void ApplyArmModel()
    {
        // Find the controller's orientation relative to the player
        Quaternion controllerOrientation = I3vrController.Orientation;
        controllerOrientation = Quaternion.Inverse(shoulderRotation) * controllerOrientation;

        // Get the relative positions of the joints
        elbowPosition = ELBOW_POSITION + new Vector3(0.0f, addedElbowHeight, addedElbowDepth);
        elbowPosition = Vector3.Scale(elbowPosition, handedMultiplier) + elbowOffset;
        wristPosition = Vector3.Scale(WRIST_POSITION, handedMultiplier);
        Vector3 armExtensionOffset = Vector3.Scale(ARM_EXTENSION_OFFSET, handedMultiplier);

        // Extract just the x rotation angle
        Vector3 controllerForward = controllerOrientation * Vector3.forward;
        float xAngle = 90.0f - Vector3.Angle(controllerForward, Vector3.up);

        // Remove the z rotation from the controller
        Quaternion xyRotation = Quaternion.FromToRotation(Vector3.forward, controllerForward);

        // Offset the elbow by the extension
        float normalizedAngle = (xAngle - MIN_EXTENSION_ANGLE) / (MAX_EXTENSION_ANGLE - MIN_EXTENSION_ANGLE);
        float extensionRatio = Mathf.Clamp(normalizedAngle, 0.0f, 1.0f);
        if (!useAccelerometer)
        {
            elbowPosition += armExtensionOffset * extensionRatio;
        }

        // Calculate the lerp interpolation factor
        float totalAngle = Quaternion.Angle(xyRotation, Quaternion.identity);
        float lerpSuppresion = 1.0f - Mathf.Pow(totalAngle / 180.0f, 6);
        float lerpValue = lerpSuppresion * (0.4f + 0.6f * extensionRatio * EXTENSION_WEIGHT);

        // Apply the absolute rotations to the joints
        Quaternion lerpRotation = Quaternion.Lerp(Quaternion.identity, xyRotation, lerpValue);
        elbowRotation = shoulderRotation * Quaternion.Inverse(lerpRotation) * controllerOrientation;
        wristRotation = shoulderRotation * controllerOrientation;

        // Determine the relative positions
        elbowPosition = shoulderRotation * elbowPosition;
        wristPosition = elbowPosition + elbowRotation * wristPosition;
    }

    private void UpdateTransparency()
    {
        // Determine how vertical the controller is pointing.
        float distToFace = Vector3.Distance(wristPosition, Vector3.zero);
        if (distToFace < fadeDistanceFromFace)
        {
            alphaValue = Mathf.Max(0.0f, alphaValue - DELTA_ALPHA * Time.deltaTime);
        }
        else
        {
            alphaValue = Mathf.Min(1.0f, alphaValue + DELTA_ALPHA * Time.deltaTime);
        }
    }

    private void UpdatePointer()
    {
        // Determine the direction of the ray.
        pointerPosition = wristPosition + wristRotation * POINTER_OFFSET;
        pointerRotation = wristRotation * Quaternion.AngleAxis(pointerTiltAngle, Vector3.right);
    }
}
