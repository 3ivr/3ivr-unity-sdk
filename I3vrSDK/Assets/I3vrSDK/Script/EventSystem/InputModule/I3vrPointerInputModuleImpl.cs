/*
 * Copyright (C) 2017 3ivr. All rights reserved.
 *
 * Author: Lucas(Wu Pengcheng)
 * Date  : 2017/07/04 17:05
 */

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.VR;

namespace i3vr
{
    /// Implementation of _I3vrPointerInputModule_
    public class I3vrPointerInputModuleImpl
    {
        /// Interface for controlling the actual InputModule.
        public II3vrInputModuleController ModuleController { get; set; }

        /// Interface for executing events.
        public II3vrEventExecutor EventExecutor { get; set; }

        /// Determines whether pointer input is active in VR Mode only (`true`), or all of the
        /// time (`false`).  Set to false if you plan to use direct screen taps or other
        /// input when not in VR Mode.
        public bool VrModeOnly { get; set; }

        /// The I3vrPointerScrollInput used to route Scroll Events through _EventSystem_
        public I3vrPointerScrollInput ScrollInput { get; set; }

        /// The I3vrBasePointer which will be responding to pointer events.
        public I3vrBasePointer Pointer { get; set; }

        /// PointerEventData from the most recent frame.
        public PointerEventData CurrentEventData { get; private set; }

        private Vector2 lastPose;
        private bool isPointerHovering = false;

        // Active state
        private bool isActive = false;

        public bool ShouldActivateModule()
        {
            bool isVrModeEnabled = !VrModeOnly;
            isVrModeEnabled |= VRSettings.enabled;
            bool activeState = ModuleController.ShouldActivate() && isVrModeEnabled;

            if (activeState != isActive)
            {
                isActive = activeState;
                // Activate pointer
                if (Pointer != null)
                {
                    if (isActive)
                    {
                        Pointer.OnInputModuleEnabled();
                    }
                }
            }
            return activeState;
        }

        public void DeactivateModule()
        {
            DisablePointer();
            ModuleController.Deactivate();
            if (CurrentEventData != null)
            {
                HandlePendingClick();
                HandlePointerExitAndEnter(CurrentEventData, null);
                CurrentEventData = null;
            }
            ModuleController.eventSystem.SetSelectedGameObject(null, ModuleController.GetBaseEventData());
        }

        public bool IsPointerOverGameObject(int pointerId)
        {
            return CurrentEventData != null && CurrentEventData.pointerEnter != null;
        }

        public void Process()
        {
            if (Pointer == null)
            {
                return;
            }

            // Save the previous Game Object
            GameObject previousObject = GetCurrentGameObject();

            CastRay();
            UpdateCurrentObject(previousObject);
            UpdatePointer(previousObject);

            // True during the frame that the trigger has been pressed.
            bool triggerDown = false;
            // True if the trigger is held down.
            bool triggering = false;

            if (IsPointerActiveAndAvailable())
            {
                triggerDown = Pointer.TriggerDown;
                triggering = Pointer.Triggering;
            }

            bool handlePendingClickRequired = !triggering;

            // Handle input
            if (!triggerDown && triggering)
            {
                HandleDrag();
            }
            else if (triggerDown && !CurrentEventData.eligibleForClick)
            {
                // New trigger action.
                HandleTriggerDown();
            }
            else if (handlePendingClickRequired)
            {
                // Check if there is a pending click to handle.
                HandlePendingClick();
            }

            ScrollInput.HandleScroll(GetCurrentGameObject(), CurrentEventData, IsPointerActiveAndAvailable());
        }

        private void CastRay()
        {
            if (Pointer == null || Pointer.PointerTransform == null)
            {
                return;
            }
            Vector2 currentPose = I3vrMathHelpers.NormalizedCartesianToSpherical(Pointer.PointerTransform.forward);

            if (CurrentEventData == null)
            {
                CurrentEventData = new PointerEventData(ModuleController.eventSystem);
                lastPose = currentPose;
            }

            // Store the previous raycast result.
            RaycastResult previousRaycastResult = CurrentEventData.pointerCurrentRaycast;

            // The initial cast must use the enter radius.
            if (Pointer != null)
            {
                Pointer.ShouldUseExitRadiusForRaycast = false;
            }

            // Cast a ray into the scene
            CurrentEventData.Reset();
            // Set the position to the center of the camera.
            // This is only necessary if using the built-in Unity raycasters.
            RaycastResult raycastResult;
            CurrentEventData.position = I3vrMathHelpers.GetViewportCenter();
            bool isPointerActiveAndAvailable = IsPointerActiveAndAvailable();
            if (isPointerActiveAndAvailable)
            {
                ModuleController.eventSystem.RaycastAll(CurrentEventData, ModuleController.RaycastResultCache);
                raycastResult = ModuleController.FindFirstRaycast(ModuleController.RaycastResultCache);
            }
            else
            {
                raycastResult = new RaycastResult();
                raycastResult.Clear();
            }

            // If we were already pointing at an object we must check that object against the exit radius
            // to make sure we are no longer pointing at it to prevent flicker.
            if (previousRaycastResult.gameObject != null
                && raycastResult.gameObject != previousRaycastResult.gameObject
                && isPointerActiveAndAvailable)
            {
                if (Pointer != null)
                {
                    Pointer.ShouldUseExitRadiusForRaycast = true;
                }
                ModuleController.RaycastResultCache.Clear();
                ModuleController.eventSystem.RaycastAll(CurrentEventData, ModuleController.RaycastResultCache);
                RaycastResult firstResult = ModuleController.FindFirstRaycast(ModuleController.RaycastResultCache);
                if (firstResult.gameObject == previousRaycastResult.gameObject)
                {
                    raycastResult = firstResult;
                }
            }

            if (raycastResult.gameObject != null && raycastResult.worldPosition == Vector3.zero)
            {
                raycastResult.worldPosition =
                  I3vrMathHelpers.GetIntersectionPosition(CurrentEventData.enterEventCamera, raycastResult);
            }

            CurrentEventData.pointerCurrentRaycast = raycastResult;

            // Find the real screen position associated with the raycast
            // Based on the results of the hit and the state of the pointerData.
            if (raycastResult.gameObject != null)
            {
                CurrentEventData.position = raycastResult.screenPosition;
            }
            else
            {
                Transform pointerTransform = Pointer.PointerTransform;
                float maxPointerDistance = Pointer.MaxPointerDistance;
                Vector3 pointerPos = pointerTransform.position + (pointerTransform.forward * maxPointerDistance);
                if (CurrentEventData.pressEventCamera != null)
                {
                    CurrentEventData.position = CurrentEventData.pressEventCamera.WorldToScreenPoint(pointerPos);
                }
                else if (I3vrControllerManager.MainCamera != null)
                {
                    CurrentEventData.position = I3vrControllerManager.MainCamera.WorldToScreenPoint(pointerPos);
                }
            }

            ModuleController.RaycastResultCache.Clear();
            CurrentEventData.delta = currentPose - lastPose;
            lastPose = currentPose;

            // Check to make sure the Raycaster being used is a I3vrRaycaster.
            if (raycastResult.module != null
                && !(raycastResult.module is I3vrPointerGraphicRaycaster)
                && !(raycastResult.module is I3vrPointerPhysicsRaycaster))
            {
                Debug.LogWarning("Using Raycaster (Raycaster: " + raycastResult.module.GetType() +
                  ", Object: " + raycastResult.module.name + "). It is recommended to use " +
                  "I3vrPointerPhysicsRaycaster or I3vrPointerGrahpicRaycaster with I3vrPointerInputModule.");
            }
        }

        private void UpdateCurrentObject(GameObject previousObject)
        {
            if (Pointer == null || CurrentEventData == null)
            {
                return;
            }
            // Send enter events and update the highlight.
            GameObject currentObject = GetCurrentGameObject(); // Get the pointer target
            HandlePointerExitAndEnter(CurrentEventData, currentObject);

            // Update the current selection, or clear if it is no longer the current object.
            var selected = EventExecutor.GetEventHandler<ISelectHandler>(currentObject);
            if (selected == ModuleController.eventSystem.currentSelectedGameObject)
            {
                EventExecutor.Execute(ModuleController.eventSystem.currentSelectedGameObject, ModuleController.GetBaseEventData(),
                  ExecuteEvents.updateSelectedHandler);
            }
            else
            {
                ModuleController.eventSystem.SetSelectedGameObject(null, CurrentEventData);
            }

            // Execute hover event.
            if (currentObject != null && currentObject == previousObject)
            {
                EventExecutor.ExecuteHierarchy(currentObject, CurrentEventData, I3vrExecuteEventsExtension.pointerHoverHandler);
            }
        }

        private void UpdatePointer(GameObject previousObject)
        {
            if (Pointer == null || CurrentEventData == null)
            {
                return;
            }

            GameObject currentObject = GetCurrentGameObject(); // Get the pointer target

            bool isInteractive = CurrentEventData.pointerPress != null ||
                                 EventExecutor.GetEventHandler<IPointerClickHandler>(currentObject) != null ||
                                 EventExecutor.GetEventHandler<IDragHandler>(currentObject) != null;

            if (isPointerHovering && currentObject != null && currentObject == previousObject)
            {
                Pointer.OnPointerHover(CurrentEventData.pointerCurrentRaycast, GetLastRay(), isInteractive);
            }
            else
            {
                // If the object's don't match or the hovering object has been destroyed
                // then the pointer has exited.
                if (previousObject != null || (currentObject == null && isPointerHovering))
                {
                    Pointer.OnPointerExit(previousObject);
                    isPointerHovering = false;
                }

                if (currentObject != null)
                {
                    Pointer.OnPointerEnter(CurrentEventData.pointerCurrentRaycast, GetLastRay(), isInteractive);
                    isPointerHovering = true;
                }
            }
        }

        private static bool ShouldStartDrag(Vector2 pressPos, Vector2 currentPos, float threshold, bool useDragThreshold)
        {
            if (!useDragThreshold)
                return true;

            return (pressPos - currentPos).sqrMagnitude >= threshold * threshold;
        }

        private void HandleDrag()
        {
            bool moving = CurrentEventData.IsPointerMoving();
            bool shouldStartDrag = ShouldStartDrag(CurrentEventData.pressPosition,
                                     CurrentEventData.position,
                                     ModuleController.eventSystem.pixelDragThreshold,
                                     CurrentEventData.useDragThreshold);

            if (moving && shouldStartDrag && CurrentEventData.pointerDrag != null && !CurrentEventData.dragging)
            {
                EventExecutor.Execute(CurrentEventData.pointerDrag, CurrentEventData,
                  ExecuteEvents.beginDragHandler);
                CurrentEventData.dragging = true;
            }

            // Drag notification
            if (CurrentEventData.dragging && moving && CurrentEventData.pointerDrag != null)
            {
                // Before doing drag we should cancel any pointer down state
                // And clear selection!
                if (CurrentEventData.pointerPress != CurrentEventData.pointerDrag)
                {
                    EventExecutor.Execute(CurrentEventData.pointerPress, CurrentEventData, ExecuteEvents.pointerUpHandler);

                    CurrentEventData.eligibleForClick = false;
                    CurrentEventData.pointerPress = null;
                    CurrentEventData.rawPointerPress = null;
                }

                EventExecutor.Execute(CurrentEventData.pointerDrag, CurrentEventData, ExecuteEvents.dragHandler);
            }
        }

        private void HandlePendingClick()
        {
            if (CurrentEventData == null || (!CurrentEventData.eligibleForClick && !CurrentEventData.dragging))
            {
                return;
            }

            if (Pointer != null)
            {
                Pointer.OnPointerClickUp();
            }

            var go = CurrentEventData.pointerCurrentRaycast.gameObject;

            // Send pointer up and click events.
            EventExecutor.Execute(CurrentEventData.pointerPress, CurrentEventData, ExecuteEvents.pointerUpHandler);

            GameObject pointerClickHandler = EventExecutor.GetEventHandler<IPointerClickHandler>(go);
            if (CurrentEventData.pointerPress == pointerClickHandler && CurrentEventData.eligibleForClick)
            {
                EventExecutor.Execute(CurrentEventData.pointerPress, CurrentEventData, ExecuteEvents.pointerClickHandler);
            }

            if (CurrentEventData.pointerDrag != null && CurrentEventData.dragging)
            {
                EventExecutor.ExecuteHierarchy(go, CurrentEventData, ExecuteEvents.dropHandler);
                EventExecutor.Execute(CurrentEventData.pointerDrag, CurrentEventData, ExecuteEvents.endDragHandler);
            }

            // Clear the click state.
            CurrentEventData.pointerPress = null;
            CurrentEventData.rawPointerPress = null;
            CurrentEventData.eligibleForClick = false;
            CurrentEventData.clickCount = 0;
            CurrentEventData.clickTime = 0;
            CurrentEventData.pointerDrag = null;
            CurrentEventData.dragging = false;
        }

        private void HandleTriggerDown()
        {
            var go = CurrentEventData.pointerCurrentRaycast.gameObject;

            // Send pointer down event.
            CurrentEventData.pressPosition = CurrentEventData.position;
            CurrentEventData.pointerPressRaycast = CurrentEventData.pointerCurrentRaycast;
            CurrentEventData.pointerPress =
              EventExecutor.ExecuteHierarchy(go, CurrentEventData, ExecuteEvents.pointerDownHandler) ??
              EventExecutor.GetEventHandler<IPointerClickHandler>(go);

            // Save the pending click state.
            CurrentEventData.rawPointerPress = go;
            CurrentEventData.eligibleForClick = true;
            CurrentEventData.delta = Vector2.zero;
            CurrentEventData.dragging = false;
            CurrentEventData.useDragThreshold = true;
            CurrentEventData.clickCount = 1;
            CurrentEventData.clickTime = Time.unscaledTime;

            // Save the drag handler as well
            CurrentEventData.pointerDrag = EventExecutor.GetEventHandler<IDragHandler>(go);
            if (CurrentEventData.pointerDrag != null)
            {
                EventExecutor.Execute(CurrentEventData.pointerDrag, CurrentEventData, ExecuteEvents.initializePotentialDrag);
            }

            if (Pointer != null)
            {
                Pointer.OnPointerClickDown();
            }
        }

        private GameObject GetCurrentGameObject()
        {
            if (CurrentEventData != null)
            {
                return CurrentEventData.pointerCurrentRaycast.gameObject;
            }

            return null;
        }

        // Modified version of BaseInputModule.HandlePointerExitAndEnter that calls EventExecutor instead of
        // UnityEngine.EventSystems.ExecuteEvents.
        private void HandlePointerExitAndEnter(PointerEventData currentPointerData, GameObject newEnterTarget)
        {
            // If we have no target or pointerEnter has been deleted then
            // just send exit events to anything we are tracking.
            // Afterwards, exit.
            if (newEnterTarget == null || currentPointerData.pointerEnter == null)
            {
                for (var i = 0; i < currentPointerData.hovered.Count; ++i)
                {
                    EventExecutor.Execute(currentPointerData.hovered[i], currentPointerData, ExecuteEvents.pointerExitHandler);
                }

                currentPointerData.hovered.Clear();

                if (newEnterTarget == null)
                {
                    currentPointerData.pointerEnter = newEnterTarget;
                    return;
                }
            }

            // If we have not changed hover target.
            if (newEnterTarget && currentPointerData.pointerEnter == newEnterTarget)
            {
                return;
            }

            GameObject commonRoot = ModuleController.FindCommonRoot(currentPointerData.pointerEnter, newEnterTarget);

            // We already an entered object from last time.
            if (currentPointerData.pointerEnter != null)
            {
                // Send exit handler call to all elements in the chain
                // until we reach the new target, or null!
                Transform t = currentPointerData.pointerEnter.transform;

                while (t != null)
                {
                    // If we reach the common root break out!
                    if (commonRoot != null && commonRoot.transform == t)
                        break;

                    EventExecutor.Execute(t.gameObject, currentPointerData, ExecuteEvents.pointerExitHandler);
                    currentPointerData.hovered.Remove(t.gameObject);
                    t = t.parent;
                }
            }

            // Now issue the enter call up to but not including the common root.
            currentPointerData.pointerEnter = newEnterTarget;
            if (newEnterTarget != null)
            {
                Transform t = newEnterTarget.transform;

                while (t != null && t.gameObject != commonRoot)
                {
                    EventExecutor.Execute(t.gameObject, currentPointerData, ExecuteEvents.pointerEnterHandler);
                    currentPointerData.hovered.Add(t.gameObject);
                    t = t.parent;
                }
            }
        }

        private Ray GetLastRay()
        {
            if (CurrentEventData != null)
            {
                I3vrBasePointerRaycaster raycaster = CurrentEventData.pointerCurrentRaycast.module as I3vrBasePointerRaycaster;
                if (raycaster != null)
                {
                    return raycaster.GetLastRay();
                }
                else if (CurrentEventData.enterEventCamera != null)
                {
                    Camera cam = CurrentEventData.enterEventCamera;
                    return new Ray(cam.transform.position, cam.transform.forward);
                }
            }

            return new Ray();
        }

        private void DisablePointer()
        {
            if (Pointer == null)
            {
                return;
            }

            GameObject currentGameObject = GetCurrentGameObject();
            if (currentGameObject)
            {
                Pointer.OnPointerExit(currentGameObject);
            }

            Pointer.OnInputModuleDisabled();
        }

        private bool IsPointerActiveAndAvailable()
        {
            if (Pointer == null)
            {
                return false;
            }

            Transform pointerTransform = Pointer.PointerTransform;
            if (pointerTransform == null)
            {
                return false;
            }

            return pointerTransform.gameObject.activeInHierarchy;
        }
    }
}
