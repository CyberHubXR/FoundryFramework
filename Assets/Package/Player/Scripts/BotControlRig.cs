using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Foundry
{
    public class BotControlRig : MonoBehaviour, IPlayerControlRig
    {
        public Transform[] trackers;
        public Vector3 movementRange = new Vector3(1, 1, 1);

        public Transform TrackerTransform(TrackerType type)
        {
            var tracker = trackers[(int) type];
            return tracker;
        }

        public TrackingMode GetTrackingMode()
        {
            return TrackingMode.ThreePoint;
        }

        public void UpdateTrackers(TrackerPos[] trackers)
        {
            for (int i = 0; i < trackers.Length; i++)  {
                var time = Time.time + i * (1f / trackers.Length) * 2 * Mathf.PI;
                var offset = new Vector3(
                    Mathf.Sin(time) * movementRange.x,
                    Mathf.Cos(time) * movementRange.y,
                    Mathf.Sin(time) * movementRange.z
                );
                var rotation = Quaternion.Euler(
                    Mathf.Sin(time) * 180,
                    Mathf.Cos(time) * 180,
                    Mathf.Sin(time) * 180
                );
                trackers[i].translation = this.trackers[i].position + offset;
                trackers[i].rotation = this.trackers[i].rotation * rotation;
                trackers[i].enabled = true;
            }
        }
    }
}
