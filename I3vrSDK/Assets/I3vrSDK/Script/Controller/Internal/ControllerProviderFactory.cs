/*
 * Copyright (C) 2017 3ivr. All rights reserved.
 *
 * Author: Lucas(Wu Pengcheng)
 * Date  : 2017/06/19 08:08
 */

using UnityEngine;

namespace i3vr
{
    static class ControllerProviderFactory
    {
        static internal IControllerProvider CreateControllerProvider(ControllerType controllerNumb)
        {

#if UNITY_EDITOR
            return new OtherServiceProvider();
#elif UNITY_ANDROID
            if (controllerNumb== ControllerType.LeftAndRight) {
                return new AndroidDoubleServiceProvider();
            }
            return new AndroidServiceProvider();          
#elif UNITY_IPHONE
            return new IosServiceProvider();
#elif UNITY_WIN
            return new WinServiceProvider();
#endif
        }
    }
}

