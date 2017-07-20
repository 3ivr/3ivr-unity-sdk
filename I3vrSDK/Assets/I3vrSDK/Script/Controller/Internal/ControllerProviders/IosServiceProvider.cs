/*
 * Copyright (C) 2017 3ivr. All rights reserved.
 *
 * Author: Lucas(Wu Pengcheng)
 * Date  : 2017/06/19 08:08
 */

using UnityEngine;
using System.Collections;
using i3vr;
using System;

class IosServiceProvider : IControllerProvider
{
    public void LeftReadState(ControllerState outState)
    {
        //throw new NotImplementedException();
    }

    public void OnPause()
    {

    }

    public void OnResume()
    {

    }

    public void ReadState(ControllerState outState)
    {
        outState.connectionState = I3vrConnectionState.Error;
    }

    public void RightReadState(ControllerState outState)
    {
        //throw new NotImplementedException();
    }
}
