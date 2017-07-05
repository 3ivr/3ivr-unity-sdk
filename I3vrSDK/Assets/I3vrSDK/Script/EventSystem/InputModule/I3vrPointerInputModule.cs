/*
 * Copyright (C) 2017 3ivr. All rights reserved.
 *
 * Author: Lucas(Wu Pengcheng)
 * Date  : 2017/07/04 17:05
 */

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

/// This script provides an implemention of Unity's `BaseInputModule` class, so
/// that Canvas-based (_uGUI_) UI elements and 3D scene objects can be
/// interacted with in a I3vr Application.
///
/// This script is intended for use with either a
/// 3D Pointer with the I3vr Controller (Recommended for I3vr),
/// or a Gaze-based-Pointer (Recommended for Cardboard).
///
/// To use, attach to the scene's **EventSystem** object.  Be sure to move it above the
/// other modules, such as _TouchInputModule_ and _StandaloneInputModule_, in order
/// for the Pointer to take priority in the event system.
///
/// If you are using a **Canvas**, set the _Render Mode_ to **World Space**,
/// and add the _I3vrPointerGraphicRaycaster_ script to the object.
///
/// If you'd like pointers to work with 3D scene objects, add a _I3vrPointerPhysicsRaycaster_ to the main camera,
/// and add a component that implements one of the _Event_ interfaces (_EventTrigger_ will work nicely) to
/// an object with a collider.
///
/// I3vrPointerInputModule emits the following events: _Enter_, _Exit_, _Down_, _Up_, _Click_, _Select_,
/// _Deselect_, _UpdateSelected_, and _I3vrPointerHover_.  Scroll, move, and submit/cancel events are not emitted.
///
/// To use a 3D Pointer with the I3vr Controller:
///   - Add the prefab I3vrSDK/Prefabs/UI/I3vrControllerPointer to your scene.
///   - Set the parent of I3vrControllerPointer to the same parent as the main camera
///     (With a local position of 0,0,0).
///
/// To use a Gaze-based-pointer:
///   - Add the prefab I3vrSDK/Prefabs/UI/I3vrReticlePointer to your scene.
///   - Set the parent of I3vrReticlePointer to the main camera.
///
public class I3vrPointerInputModule : BaseInputModule, II3vrInputModuleController
{
    /// Determines whether Pointer input is active in VR Mode only (`true`), or all of the
    /// time (`false`).  Set to false if you plan to use direct screen taps or other
    /// input when not in VR Mode.
    [Tooltip("Whether Pointer input is active in VR Mode only (true), or all the time (false).")]
    public bool vrModeOnly = false;

    [Tooltip("Manages scroll events for the input module.")]
    public I3vrPointerScrollInput scrollInput = new I3vrPointerScrollInput();

    public I3vrPointerInputModuleImpl Impl { get; private set; }

    public I3vrEventExecutor EventExecutor { get; private set; }

    public new EventSystem eventSystem
    {
        get
        {
            return base.eventSystem;
        }
    }

    public List<RaycastResult> RaycastResultCache
    {
        get
        {
            return m_RaycastResultCache;
        }
    }

    /// Helper function to find the Event Executor that is part of
    /// the input module if one exists in the scene.
    public static I3vrEventExecutor FindEventExecutor()
    {
        I3vrPointerInputModule i3vrInputModule = FindInputModule();
        if (i3vrInputModule == null)
        {
            return null;
        }

        return i3vrInputModule.EventExecutor;
    }

    /// Helper function to find the input module if one exists in the
    /// scene and it is the active module.
    public static I3vrPointerInputModule FindInputModule()
    {
        if (EventSystem.current == null)
        {
            return null;
        }

        BaseInputModule inputModule = EventSystem.current.currentInputModule;
        if (inputModule == null)
        {
            return null;
        }

        I3vrPointerInputModule i3vrInputModule = inputModule as I3vrPointerInputModule;
        return i3vrInputModule;
    }

    public override bool ShouldActivateModule()
    {
        return Impl.ShouldActivateModule();
    }

    public override void DeactivateModule()
    {
        Impl.DeactivateModule();
    }

    public override bool IsPointerOverGameObject(int pointerId)
    {
        return Impl.IsPointerOverGameObject(pointerId);
    }

    public override void Process()
    {
        UpdateImplProperties();
        Impl.Process();
    }

    protected override void Awake()
    {
        base.Awake();
        Impl = new I3vrPointerInputModuleImpl();
        EventExecutor = new I3vrEventExecutor();
        UpdateImplProperties();
    }

    public bool ShouldActivate()
    {
        return base.ShouldActivateModule();
    }

    public void Deactivate()
    {
        base.DeactivateModule();
    }

    public new GameObject FindCommonRoot(GameObject g1, GameObject g2)
    {
        return BaseInputModule.FindCommonRoot(g1, g2);
    }

    public new BaseEventData GetBaseEventData()
    {
        return base.GetBaseEventData();
    }

    public new RaycastResult FindFirstRaycast(List<RaycastResult> candidates)
    {
        return BaseInputModule.FindFirstRaycast(candidates);
    }

    private void UpdateImplProperties()
    {
        if (Impl == null)
        {
            return;
        }

        Impl.ScrollInput = scrollInput;
        Impl.VrModeOnly = vrModeOnly;
        Impl.Pointer = I3vrPointerManager.Pointer;
        Impl.ModuleController = this;
        Impl.EventExecutor = EventExecutor;
    }
}
