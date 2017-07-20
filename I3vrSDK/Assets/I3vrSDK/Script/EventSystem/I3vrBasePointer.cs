/*
 * Copyright (C) 2017 3ivr. All rights reserved.
 *
 * Author: Lucas(Wu Pengcheng)
 * Date  : 2017/07/04 17:05
 */

using UnityEngine;
using UnityEngine.EventSystems;

/// This abstract class should be implemented for pointer based input, and used with
/// the I3vrPointerInputModule script.
///
/// It provides methods called on pointer interaction with in-game objects and UI,
/// trigger events, and 'BaseInputModule' class state changes.
///
/// To have the methods called, an instance of this (implemented) class must be
/// registered with the **I3vrPointerManager** script on 'OnEnable' by calling
/// I3vrPointerManager.OnPointerCreated.
/// A registered instance should also un-register itself at 'OnDisable' calls
/// by setting the **I3vrPointerManager.Pointer** static property
/// to null.
///
/// This abstract class should be implemented by pointers doing 1 of 2 things:
/// 1. Responding to movement of the users head (Cardboard gaze-based-pointer).
/// 2. Responding to the movement of the I3vr controller (I3vr 3D pointer).
public abstract class I3vrBasePointer
{
    /// Convenience function to access what the pointer is currently hitting.
    public RaycastResult CurrentRaycastResult
    {
        get
        {
            I3vrPointerInputModule inputModule = I3vrPointerInputModule.FindInputModule();
            if (inputModule == null)
            {
                return new RaycastResult();
            }

            if (inputModule.Impl == null)
            {
                return new RaycastResult();
            }

            if (inputModule.Impl.CurrentEventData == null)
            {
                return new RaycastResult();
            }

            return inputModule.Impl.CurrentEventData.pointerCurrentRaycast;
        }
    }

    /// This is used by I3vrBasePointerRaycaster to determine if the
    /// enterRadius or the exitRadius should be used for the raycast.
    /// It is set by I3vrPointerInputModule and doesn't need to be controlled manually.
    public bool ShouldUseExitRadiusForRaycast { get; set; }

    /// Returns the transform that represents this pointer.
    /// It is used by I3vrBasePointerRaycaster as the origin of the ray.
    public virtual Transform PointerTransform { get; set; }

    /// Returns the point that represents the reticle position
    /// It is used by the keyboard as the end of the ray.
    public abstract Vector3 LineEndPoint { get; }

    /// Returns the max distance this pointer will be rendered at from the camera.
    /// This is used by I3vrBasePointerRaycaster to calculate the ray when using
    /// the default "Camera" RaycastMode. See I3vrBasePointerRaycaster.cs for details.
    public abstract float MaxPointerDistance { get; }

    public virtual bool TriggerDown
    {
        get
        {
            bool isTriggerDown = Input.GetMouseButtonDown(0);
            return isTriggerDown || I3vrControllerManager.I3vrRightController.TriggerButtonDown;
        }
    }

    /// If true, the trigger is currently being pressed. This is not
    /// an event: it represents the trigger's state (it remains true while the trigger is being
    /// pressed).
    /// Defaults to I3vrController.ClickButton, can be overridden to change the trigger.
    public virtual bool Triggering
    {
        get
        {
            bool isTriggering = Input.GetMouseButton(0);
            return isTriggering || I3vrControllerManager.I3vrRightController.TriggerButton;
        }
    }

    public virtual void OnStart()
    {
        I3vrPointerManager.OnPointerCreated(this);
    }

    /// This is called when the 'BaseInputModule' system should be enabled.
    public abstract void OnInputModuleEnabled();

    /// This is called when the 'BaseInputModule' system should be disabled.
    public abstract void OnInputModuleDisabled();

    /// Called when the pointer is facing a valid GameObject. This can be a 3D
    /// or UI element.
    ///
    /// **raycastResult** is the hit detection result for the object being pointed at.
    /// **ray** is the ray that was cast to determine the raycastResult.
    /// **isInteractive** is true if the object being pointed at is interactive.
    public abstract void OnPointerEnter(RaycastResult rayastResult, Ray ray,
      bool isInteractive);

    /// Called every frame the user is still pointing at a valid GameObject. This
    /// can be a 3D or UI element.
    ///
    /// **raycastResult** is the hit detection result for the object being pointed at.
    /// **ray** is the ray that was cast to determine the raycastResult.
    /// **isInteractive** is true if the object being pointed at is interactive.
    public abstract void OnPointerHover(RaycastResult rayastResult, Ray ray,
      bool isInteractive);

    /// Called when the pointer no longer faces an object previously
    /// intersected with a ray projected from the camera.
    /// This is also called just before **OnInputModuleDisabled**
    /// previousObject will be null in this case.
    ///
    /// **previousObject** is the object that was being pointed at the previous frame.
    public abstract void OnPointerExit(GameObject previousObject);

    /// Called when a click is initiated.
    public abstract void OnPointerClickDown();

    /// Called when click is finished.
    public abstract void OnPointerClickUp();

    /// Return the radius of the pointer. It is used by I3vrPointerPhysicsRaycaster when
    /// searching for valid pointer targets. If a radius is 0, then a ray is used to find
    /// a valid pointer target. Otherwise it will use a SphereCast.
    /// The *enterRadius* is used for finding new targets while the *exitRadius*
    /// is used to see if you are still nearby the object currently pointed at
    /// to avoid a flickering effect when just at the border of the intersection.
    ///
    /// NOTE: This is only works with I3vrPointerPhysicsRaycaster. To use it with uGUI,
    /// add 3D colliders to your canvas elements.
    public abstract void GetPointerRadius(out float enterRadius, out float exitRadius);
}
