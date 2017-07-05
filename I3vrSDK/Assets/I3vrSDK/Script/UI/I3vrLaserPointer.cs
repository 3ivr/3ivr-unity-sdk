/*
 * Copyright (C) 2017 3ivr. All rights reserved.
 *
 * Author: Lucas(Wu Pengcheng)
 * Date  : 2017/07/04 17:05
 */

using UnityEngine;

/// This laser pointer visual should be attached to the controller object.
/// The laser visual is important to help users locate their cursor
/// when its not directly in their field of view.
[RequireComponent(typeof(LineRenderer))]
public class I3vrLaserPointer : MonoBehaviour
{
    private I3vrLaserPointerImpl laserPointerImpl;

    /// Color of the laser pointer including alpha transparency
    public Color laserColor = new Color(1.0f, 1.0f, 1.0f, 0.25f);

    /// Maximum distance of the pointer (meters).
    [Range(0.0f, 10.0f)]
    public float maxLaserDistance = 0.75f;

    /// Maximum distance of the reticle (meters).
    [Range(0.4f, 10.0f)]
    public float maxReticleDistance = 2.5f;

    public GameObject reticle;

    /// Sorting order to use for the reticle's renderer.
    /// Range values come from https://docs.unity3d.com/ScriptReference/Renderer-sortingOrder.html.
    [Range(-32767, 32767)]
    public int reticleSortingOrder = 32767;

    void Awake()
    {
        laserPointerImpl = new I3vrLaserPointerImpl();
        laserPointerImpl.LaserLineRenderer = gameObject.GetComponent<LineRenderer>();

        if (reticle != null)
        {
            Renderer reticleRenderer = reticle.GetComponent<Renderer>();
            reticleRenderer.sortingOrder = reticleSortingOrder;
        }
    }

    void Start()
    {
        laserPointerImpl.OnStart();
        laserPointerImpl.MainCamera = Camera.main;
        UpdateLaserPointerProperties();
    }

    void LateUpdate()
    {
        UpdateLaserPointerProperties();
        laserPointerImpl.OnUpdate();
    }

    public void SetAsMainPointer()
    {
        I3vrPointerManager.Pointer = laserPointerImpl;
    }

    public Vector3 LineStartPoint
    {
        get
        {
            return laserPointerImpl != null ? laserPointerImpl.PointerTransform.position : Vector3.zero;
        }
    }

    public Vector3 LineEndPoint
    {
        get { return laserPointerImpl != null ? laserPointerImpl.LineEndPoint : Vector3.zero; }
    }

    public LineRenderer LineRenderer
    {
        get { return laserPointerImpl != null ? laserPointerImpl.LaserLineRenderer : null; }
    }

    private void UpdateLaserPointerProperties()
    {
        if (laserPointerImpl == null)
        {
            return;
        }
        laserPointerImpl.LaserColor = laserColor;
        laserPointerImpl.Reticle = reticle;
        laserPointerImpl.MaxLaserDistance = maxLaserDistance;
        laserPointerImpl.MaxReticleDistance = maxReticleDistance;
        laserPointerImpl.PointerTransform = transform;
    }
}
