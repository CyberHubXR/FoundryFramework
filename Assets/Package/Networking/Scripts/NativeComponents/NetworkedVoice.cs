using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Foundry;
using Foundry.Core.Serialization;
using Foundry.Networking;
using UnityEngine;
using UnityEngine.Audio;
using Random = UnityEngine.Random;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Foundry
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(AudioSource))]
    public class NetworkedVoice : NetworkComponent
    {
        [Tooltip(
            "The sample rate of the audio. Higher values will increase quality but also increase bandwidth usage.")]
        public NetworkProperty<int> sampleRate = new(44100);

        [Tooltip(
            "The time in seconds a receiver will buffer audio for before playing it. Higher values will reduce audio artifacts but increase latency.")]
        public float bufferTime = 0.4f;

        [Tooltip(
            "The name of the microphone device to use for recording. If left blank the default microphone will be used. ONLY USE THIS FOR TESTING, as it will not work on other clients.")]
        [SerializeField]
        internal string microphoneDevice;

        [Tooltip(
            "If true, the audio will be mirrored back to the sender. This is useful for testing but should be disabled in production.")]
        [SerializeField]
        internal bool debugMirror;

        internal AudioClip audioBuffer;
        internal AudioSource audioOutput;

        internal bool isRecording;
        public bool IsRecording => isRecording;

        internal int lastReadPos;
        internal int writePos;
        internal UInt64 packetIndex;

        internal Coroutine recordingCoroutine;

        internal int lastRealSamplesPerSecond;
        internal int realSamplesPerSecond;


        public override void RegisterProperties(List<INetworkProperty> props, List<INetworkEvent> events)
        {
            props.Add(sampleRate);
        }

        private void OnValidate()
        {
            bufferTime = Mathf.Max(bufferTime, 0.1f);
        }

        public override void OnConnected()
        {
            audioOutput = GetComponent<AudioSource>();
            if (IsOwner)
                recordingCoroutine = StartCoroutine(SendVoice());
            if (!IsOwner || debugMirror)
            {
                audioOutput.loop = true;
                CreateBuffer();
                var player = GetComponentInParent<Player>();
                if (player.playerId.Value == UInt64.MaxValue)
                    player.playerId.OnValueChanged += id => NetworkManager.instance.RegisterVoiceChatListener(id, IngestVoice);
                else
                    NetworkManager.instance.RegisterVoiceChatListener(player.playerId.Value, IngestVoice);
                sampleRate.OnChanged += CreateBuffer;
                StartCoroutine(MaintainBufferLength());
            }
            StartCoroutine(UpdateSamplesLastSecond());
        }

        private void CreateBuffer()
        {
            audioBuffer = AudioClip.Create("Voice", Mathf.CeilToInt(sampleRate.Value * (bufferTime + 2)), 1,
                sampleRate.Value, false);
            audioOutput.clip = audioBuffer;
            if (audioBuffer.frequency != sampleRate.Value)
                Debug.LogError(
                    "Audio buffer sample rate does not match the desired sample rate. This will cause audio artifacts.");
        }

        /// <summary>
        /// Set which microphone to use for recording, if not set it will use the default microphone.
        /// </summary>
        /// <param name="deviceName">Value from Microphone.devices</param>
        public void SetMicrophone(string deviceName)
        {
            if (recordingCoroutine != null)
            {
                StopCoroutine(recordingCoroutine);
                Microphone.End(microphoneDevice);
            }

            microphoneDevice = deviceName;
            recordingCoroutine = StartCoroutine(SendVoice());
        }

        readonly float[] transportBuffer = new float[1024 / sizeof(short)];
        private IEnumerator SendVoice()
        {
            Debug.Log("Starting recording");
            var readBuffer = Microphone.Start(microphoneDevice, true, 5, sampleRate.Value);
            yield return new WaitUntil(() => Microphone.GetPosition(microphoneDevice) > 0);
            isRecording = true;

            if (readBuffer.frequency != sampleRate.Value)
            {
                sampleRate.Value = readBuffer.frequency;
                Debug.LogWarning("Microphone sample rate does not match desired sample rate. Setting to " +
                                 readBuffer.frequency);
            }

            yield return null;
            while (Microphone.IsRecording(microphoneDevice))
            {
                int readPos = Microphone.GetPosition(microphoneDevice);
                int readLength = readPos - lastReadPos;
                if (readLength < 0)
                    readLength = readPos + (readBuffer.samples - lastReadPos);
                
                while (readLength > transportBuffer.Length)
                {
                    readBuffer.GetData(transportBuffer, lastReadPos);
                    
                    byte[] quantized = new byte[1024];
                    for (int i = 0; i < transportBuffer.Length; i++)
                    {
                        BitConverter.TryWriteBytes(
                            new Span<byte>(quantized, i  * 2, 2), 
                            (short)(transportBuffer[i] * short.MaxValue)
                            );
                    }
                    
                    NetworkManager.instance.SendVoiceChatPacket(packetIndex++, quantized);
                    realSamplesPerSecond += transportBuffer.Length;
                    readLength -= transportBuffer.Length;
                    lastReadPos = (lastReadPos + transportBuffer.Length) % readBuffer.samples;
                }

                yield return null;
            }

            isRecording = false;
        }

        UInt64 latestPacketIndex;
        private void IngestVoice(UInt64 index, ArraySegment<byte> data)
        {
            for (int i = 0; i < transportBuffer.Length; i++)
                transportBuffer[i] = BitConverter.ToInt16(data.Array, data.Offset + i * 2) / (float)short.MaxValue;
            
            UInt64 packetWriteTime = (index * (UInt64)transportBuffer.Length) % (UInt64)audioBuffer.samples;
            
            audioBuffer.SetData(transportBuffer, (int)packetWriteTime);
            realSamplesPerSecond += transportBuffer.Length;
            // If the packet is out of order, we ignore setting the write position
            if (latestPacketIndex > index)
                writePos = (int)packetWriteTime;
            latestPacketIndex = index;
        }

        private IEnumerator MaintainBufferLength()
        {
            while (true)
            {
                var sampleBufferLength = writePos - audioOutput.timeSamples;
                if (sampleBufferLength < 0)
                    sampleBufferLength = writePos + audioBuffer.samples - audioOutput.timeSamples;
                
                var bufferLength = sampleBufferLength / (float)sampleRate.Value;
                var bufferDelta = bufferLength - bufferTime;
                if (bufferLength < 0)
                    audioOutput.Pause();
                if (bufferDelta > 0 && !audioOutput.isPlaying)
                    audioOutput.Play();
                if (bufferDelta > 0.25f)
                {
                    var newPos = writePos - (int)(sampleRate.Value * bufferTime);
                    if (newPos < 0)
                        newPos += audioBuffer.samples;
                    audioOutput.timeSamples = newPos;
                    Debug.Log("Buffer underrun detected, resetting buffer position");
                }

                yield return null;
            }
        }
        

        private IEnumerator UpdateSamplesLastSecond()
        {
            while (true)
            {
                lastRealSamplesPerSecond = (int)Mathf.Lerp((float)lastRealSamplesPerSecond, (float)realSamplesPerSecond, 0.1f);
                realSamplesPerSecond = 0;
                yield return new WaitForSeconds(1);
            }
        }

        private void OnDestroy()
        {
            if (isRecording)
                Microphone.End(microphoneDevice);
        }
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(NetworkedVoice))]
public class NetworkedVoiceEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        var voice = (NetworkedVoice)target;
        if (Application.isPlaying)
        {
            if (!voice.IsOwner || voice.debugMirror)
            {
                var sampleBufferLength = voice.writePos - voice.audioOutput.timeSamples;
                if (sampleBufferLength < 0)
                    sampleBufferLength = voice.writePos + voice.audioBuffer.samples - voice.audioOutput.timeSamples;
                var bufferLength = sampleBufferLength / (float)voice.sampleRate.Value;
                var bufferDelta = bufferLength - voice.bufferTime;
                EditorGUI.BeginDisabledGroup(true);
                EditorGUILayout.LabelField("Write pos: " + voice.writePos);
                EditorGUILayout.IntSlider("", voice.writePos, 0, voice.audioBuffer.samples);
                EditorGUILayout.LabelField("Time samples: " + voice.audioOutput.timeSamples);
                EditorGUILayout.IntSlider("", voice.audioOutput.timeSamples, 0, voice.audioBuffer.samples);
                EditorGUILayout.LabelField("Current Buffer Samples " + sampleBufferLength);
                EditorGUILayout.LabelField("Current Buffer Time " + bufferLength);
                EditorGUILayout.Slider("", bufferLength / voice.bufferTime, 0f, 2f);
                EditorGUILayout.LabelField("Current Buffer Delta" + bufferDelta);
                EditorGUILayout.LabelField("Real Samples Per Second " + voice.lastRealSamplesPerSecond);
                EditorGUILayout.LabelField("Is Playing " + voice.audioOutput.isPlaying);
                EditorGUI.EndDisabledGroup();
            }
            else
            {
                EditorGUILayout.LabelField("Read pos: " + voice.lastReadPos);
                EditorGUILayout.LabelField("Real Samples Per Second " + voice.lastRealSamplesPerSecond);
            }
        }
    }
}
#endif
