using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;
using Fusion.Sockets;
using System;

namespace Foundry
{
    public enum PlayerControlMode
    {
        Auto = 0,
        Desktop = 1,
        XR = 2
    }

    public enum FoundryNetworkButton
    {
        leftGrab = 0,
        rightGrab = 1
    }

    public struct NetPosRot : INetworkStruct
    {
        public Vector3 pos;
        public Quaternion rot;
        public static implicit operator PosRot(NetPosRot v) => new PosRot
        {
            pos = v.pos,
            rot = v.rot
        };
        public static implicit operator NetPosRot(PosRot v) => new NetPosRot
        {
            pos = v.pos,
            rot = v.rot
        };
    }

    public struct NetworkedCommonInput : INetworkInput
    {
        public NetworkButtons buttons;
        public Vector3 movement;
    }

    public struct NetworkedDesktopInput : INetworkInput
    {
        public NetworkedCommonInput input;
        public NetworkedTR head;
        public NetworkedTR reachPos;
    }

    public struct Networked3PInput : INetworkInput
    {
        public NetworkedCommonInput input;

        public NetworkedTR head;
        public NetworkedTR leftHand;
        public NetworkedTR rightHand;
    }

    public struct Networked6PInput : INetworkInput
    {
        public NetworkedCommonInput input;

        public NetworkedTR head;
        public NetworkedTR leftHand;
        public NetworkedTR rightHand;
        public NetworkedTR waist;
        public NetworkedTR leftFoot;
        public NetworkedTR rightFoot;
    }
}
