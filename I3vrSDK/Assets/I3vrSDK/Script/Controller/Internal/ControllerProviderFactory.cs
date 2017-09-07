/*
 * Copyright (C) 2017 3ivr. All rights reserved.
 *
 * Author: Lucas(Wu Pengcheng)
 * Date  : 2017/06/19 08:08
 */

namespace i3vr
{
    static class ControllerProviderFactory
    {
        static internal IControllerProvider CreateControllerProvider()
        {
#if UNITY_EDITOR
            return new OtherServiceProvider();
#elif UNITY_ANDROID
            return new AndroidServiceControllerProvider();
#elif UNITY_IPHONE
            return new IosServiceProvider();
#elif UNITY_WIN
            return new WinServiceProvider();
#endif
        }
    }
}

