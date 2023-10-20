using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;


namespace Foundry
{
    // Struct for serializing translations and rotations
    public struct NetworkedTR : INetworkInput
    {
        public Vector3 translation;
        public Quaternion rotation;
        public Vector3 velocity;
        public Vector3 angularVelocity;
        public static implicit operator TrackerPos(NetworkedTR v) => new TrackerPos { rotation = v.rotation, translation = v.translation };
        public static implicit operator PosRot(NetworkedTR v) => new PosRot { pos =v.translation, rot=v.rotation};
        public static implicit operator NetworkedTR(PosRot v) => new NetworkedTR { translation =v.pos, rotation=v.rot};
    }
    
    // Class for handling prediction, smoothing, and displaying of networked TRs
    [System.Serializable]
    public class NetworkTRPredictor
    {
        public float lerpSpeed = 8f;

        NetworkedTR lastTruth;
        float lastTruthUpdate;
        Vector3 deltaVelocity;
        Vector3 deltaAngularVelocity;

        NetworkedTR lastRender;
        float lastRenderTime = -1;

        // Call from fixed network update only if Runner.Tick == Runner.Simulation.LatestServerState.Tick, otherwise use predict
        public void Update(NetworkedTR latestTruth, float time)
        {
            float deltaTime = time - lastTruthUpdate;
            deltaVelocity = (latestTruth.velocity - lastTruth.velocity) / deltaTime;
            deltaAngularVelocity = (latestTruth.angularVelocity - lastTruth.angularVelocity) / deltaTime;
            lastTruth = latestTruth;
            lastTruthUpdate = time;
        }

        public void Predict(ref NetworkedTR current, float deltaTime)
        {
            current.translation += lastTruth.velocity * deltaTime;
            current.rotation = Quaternion.Euler(lastTruth.angularVelocity * deltaTime) * current.rotation;
            current.velocity += deltaVelocity * deltaTime;
            current.angularVelocity += deltaAngularVelocity * deltaTime;
        }

        // Returns a tracker pos that's lerped to match the NetworkTR, with a small amount of prediction
        public TrackerPos Render(in NetworkedTR current, float time)
        {
            float deltaTime = time - lastRenderTime;
            //Teleport if render delta is too big
            if (deltaTime > 0.5f)
            {
                lastRender = new NetworkedTR
                {
                    translation = current.translation,
                    rotation = current.rotation,
                    velocity = current.velocity,
                    angularVelocity = current.angularVelocity
                };
                return lastRender;
            }
            else
            {
                Vector3 newVelocity = Vector3.Lerp(lastRender.velocity, (current.translation - lastRender.translation) / deltaTime, 0.9f);
                Vector3 newAngularVelocity = Vector3.Lerp(lastRender.angularVelocity, (current.angularVelocity - lastRender.angularVelocity) / deltaTime, 0.9f);
                lastRender.translation += lastRender.velocity * deltaTime;
                lastRender.rotation = Quaternion.Euler(lastRender.angularVelocity * deltaTime) * lastRender.rotation;
                lastRender.velocity = newVelocity;
                lastRender.angularVelocity = newAngularVelocity;
            }


            lastRender.translation = Vector3.Lerp(lastRender.translation, current.translation, lerpSpeed * Time.deltaTime);
            lastRender.rotation = Quaternion.Slerp(lastRender.rotation, current.rotation, lerpSpeed * Time.deltaTime);
            lastRenderTime = time;
            return lastRender;
        }

        //Lerps render pos without prediction
        public TrackerPos Render(in NetworkedTR current)
        {
            lastRender.translation = Vector3.Lerp(lastRender.translation, current.translation, lerpSpeed * Time.deltaTime);
            lastRender.rotation = Quaternion.Slerp(lastRender.rotation, current.rotation, lerpSpeed * Time.deltaTime);
            return lastRender;
        }

        public Vector3 RenderVelocity()
        {
            return lastRender.velocity;
        }

        public void Teleport(NetworkedTR newTruth)
        {
            lastTruth = newTruth;
            lastRenderTime = -1;
        }
    }
}

