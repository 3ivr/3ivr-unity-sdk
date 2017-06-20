/*
* Copyright (C) 2017 3ivr. All rights reserved.
*
* Author: Lucas(Wu Pengcheng)
* Date  : 2017/06/19 08:08
*/

inline float4 GvrUnityObjectToClipPos(in float3 pos) {
#if defined(UNITY_5_4_OR_NEWER)
    return UnityObjectToClipPos(pos);
#else

#if defined(UNITY_SINGLE_PASS_STEREO) || defined(UNITY_USE_CONCATENATED_MATRICES)
    // More efficient than computing M*VP matrix product
    return mul(UNITY_MATRIX_VP, mul(unity_ObjectToWorld, float4(pos, 1.0)));
#else
    return mul(UNITY_MATRIX_MVP, float4(pos, 1.0));
#endif  // defined(UNITY_SINGLE_PASS_STEREO) || defined(UNITY_USE_CONCATENATED_MATRICES)

#endif  // defined(UNITY_5_4_OR_NEWER)
}
