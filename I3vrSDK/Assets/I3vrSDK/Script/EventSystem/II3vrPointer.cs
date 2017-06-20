/*
 * Copyright (C) 2017 3ivr. All rights reserved.
 *
 * Author: Lucas(Wu Pengcheng)
 * Date  : 2017/06/19 08:08
 */

using UnityEngine;

/// This script provides an interface for pointer based input used with
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
/// This class is expected to be inherited by pointers doing 1 of 2 things:
/// 1. Responding to movement of the users head (Cardboard gaze-based-pointer).
/// 2. Responding to the movement of the i3vr controller.
public interface II3vrPointer
{

    /// This is used by I3vrBasePointerRaycaster to determine if the
    /// enterRadius or the exitRadius should be used for the raycast.
    /// It is set by I3vrPointerInputModule and doesn't need to be controlled manually.
    bool ShouldUseExitRadiusForRaycast
    {
        get;
        set;
    }

    /// This is called when the 'BaseInputModule' system should be enabled.
    void OnInputModuleEnabled();

    /// This is called when the 'BaseInputModule' system should be disabled.
    void OnInputModuleDisabled();

    /// Called when the pointer is facing a valid GameObject. This can be a 3D
    /// or UI element.
    ///
    /// The targetObject is the object the user is pointing at.
    /// The intersectionPosition is where the ray intersected with the targetObject.
    /// The intersectionRay is the ray that was cast to determine the intersection.
    void OnPointerEnter(GameObject targetObject, Vector3 intersectionPosition,
       Ray intersectionRay, bool isInteractive);

    /// Called every frame the user is still pointing at a valid GameObject. This
    /// can be a 3D or UI element.
    ///
    /// The targetObject is the object the user is pointing at.
    /// The intersectionPosition is where the ray intersected with the targetObject.
    /// The intersectionRay is the ray that was cast to determine the intersection.
    void OnPointerHover(GameObject targetObject, Vector3 intersectionPosition,
        Ray intersectionRay, bool isInteractive);

    /// Called when the pointer no longer faces an object previously
    /// intersected with a ray projected from the camera.
    /// This is also called just before **OnInputModuleDisabled** and may have have any of
    /// the values set as **null**.
    void OnPointerExit(GameObject targetObject);

    /// Called when a click is initiated.
    void OnPointerClickDown();

    /// Called when click is finished.
    void OnPointerClickUp();

    /// Returns the max distance this pointer will be rendered at from the camera.
    /// This is used by I3vrBasePointerRaycaster to calculate the ray when using
    /// the default "Camera" RaycastMode. See I3vrBasePointerRaycaster.cs for details.
    float GetMaxPointerDistance();

    /// Returns the transform that represents this pointer.
    /// It is used by I3vrBasePointerRaycaster as the origin of the ray.
    Transform GetPointerTransform();

    /// Return the radius of the pointer. It is used by I3vrPointerPhysicsRaycaster
    /// and I3vrGaze when searching for valid pointer targets. If a radius is 0, then
    /// a ray is used to find a valid pointer target. Otherwise it will use a SphereCast.
    /// The *enterRadius* is used for finding new targets while the *exitRadius*
    /// is used to see if you are still nearby the object currently pointed at
    /// to avoid a flickering effect when just at the border of the intersection.
    ///
    /// NOTE: This is only works with I3vrPointerPhysicsRaycaster. To use it with uGUI,
    /// add 3D colliders to your canvas elements.
    void GetPointerRadius(out float enterRadius, out float exitRadius);
}
