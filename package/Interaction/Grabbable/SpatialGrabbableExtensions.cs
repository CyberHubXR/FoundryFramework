using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

namespace Foundry
{
    public static class SpatialGrabbableExtensions
    {


        /// <summary>Returns true if there is a grabbable or link, out null if there is none</summary>
        public static bool HasGrabbable(this SpatialHand hand, GameObject obj, out SpatialGrabbable grabbable) {
            return HasGrabbable(obj, out grabbable);
        }

        /// <summary>Returns true if there is a grabbable or link, out null if there is none</summary>
        public static bool HasGrabbable(this GameObject obj, out SpatialGrabbable grabbable) {
            if(obj == null) {
                grabbable = null;
                return false;
            }

            if(obj.CanGetComponent(out grabbable)) {
                return true;
            }

            SpatialGrabbableChild grabChild;
            if(obj.CanGetComponent(out grabChild)) {
                grabbable = grabChild.grabParent;
                return true;
            }

            grabbable = null;
            return false;
        }


        /// <summary>Autohand extension method, used so I can use TryGetComponent for newer versions and GetComponent for older versions</summary>
        public static bool CanGetComponent<T>(this GameObject componentClass, out T component) {
#if UNITY_2019_1 || UNITY_2018 || UNITY_2017
       var tempComponent = componentClass.GetComponent<T>();
        if(tempComponent != null){
            component = tempComponent;
            return true;
        }
        else {
            component = tempComponent;
            return false;
        }
#else
            var value = componentClass.TryGetComponent(out component);
            return value;
#endif
        }

        /// <summary>Autohand extension method, used so I can use TryGetComponent for newer versions and GetComponent for older versions</summary>
        public static bool CanGetComponent<T>(this Transform componentClass, out T component) {
#if UNITY_2019_1 || UNITY_2018 || UNITY_2017
       var tempComponent = componentClass.GetComponent<T>();
        if(tempComponent != null){
            component = tempComponent;
            return true;
        }
        else {
            component = tempComponent;
            return false;
        }
#else
            var value = componentClass.TryGetComponent(out component);
            return value;
#endif
        }
    }
}
