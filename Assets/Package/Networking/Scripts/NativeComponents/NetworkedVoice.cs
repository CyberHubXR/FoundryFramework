using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Foundry.Core.Serialization;
using Foundry.Networking;
using UnityEngine;

namespace Foundry
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(AudioSource))]
    public class NetworkedVoice : NetworkComponent
    {
        [Tooltip("The sample rate of the audio. Higher values will increase quality but also increase bandwidth usage.")]
        public int sampleRate = 44100;
        [Tooltip("The time in seconds to record audio for before sending it. Lower values will reduce latency but may cause audio artifacts and dataloss.")]
        public float recordTime = 0.1f;
        [Tooltip("The time in seconds a receiver will buffer audio for before playing it. Higher values will reduce audio artifacts but increase latency.")]
        public float bufferTime = 0.4f;
        
        [Tooltip("The name of the microphone device to use for recording. If left blank the default microphone will be used. ONLY USE THIS FOR TESTING, as it will not work on other clients.")]
        [SerializeField] private string microphoneDevice;
        [Tooltip("If true, the audio will be mirrored back to the sender. This is useful for testing but should be disabled in production.")]
        [SerializeField] private bool debugMirror;
        
        private NetworkEvent<AudioBuffer> SendVoiceClip { get; } = new();
        private AudioClip audioBuffer;
        private AudioSource audioOutput;

        private bool isRecording;
        public bool IsRecording => isRecording;

        private int lastReadPos;
        private int writePos;
        
        private Coroutine recordingCoroutine;
        

        public override void RegisterProperties(List<INetworkProperty> props, List<INetworkEvent> events)
        {
            SendVoiceClip.MaxQueueLength = 15;
            events.Add(SendVoiceClip);
        }

        private void OnValidate()
        {
            recordTime = Mathf.Max(recordTime, 1f / 65f);
            bufferTime = Mathf.Max(bufferTime, recordTime);
        }
        
        public override void OnConnected()
        {
            audioOutput = GetComponent<AudioSource>();
            if (IsOwner)
                recordingCoroutine = StartCoroutine(SendVoice());
            if (!IsOwner || debugMirror)
            {
                audioBuffer = AudioClip.Create("Voice", Mathf.CeilToInt(sampleRate * (bufferTime + recordTime * 2)), 1, sampleRate, false);
                audioOutput.clip = audioBuffer;
                audioOutput.loop = true;
                SendVoiceClip.AddListener((s, buffer) =>
                {
                    audioBuffer.SetData(buffer.data, writePos);
                    writePos = (writePos + buffer.data.Length) % audioBuffer.samples;
                    if (!audioOutput.isPlaying)
                    {
                        audioOutput.timeSamples = (writePos - (int)(bufferTime * sampleRate) + audioBuffer.samples) % audioBuffer.samples;
                        audioOutput.Play();
                    }
                });
            }
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
        
        private IEnumerator SendVoice()
        {
            Debug.Log("Starting recording");
            var readBuffer = Microphone.Start(microphoneDevice, true,  Mathf.CeilToInt(recordTime * 2.5f), sampleRate);
            yield return new WaitUntil(()=>Microphone.GetPosition(microphoneDevice) > 0);
            isRecording = true;

            yield return null;
            while (Microphone.IsRecording(microphoneDevice))
            {
                int readPos = Microphone.GetPosition(microphoneDevice);
                int readLength = readPos - lastReadPos;
                if (readLength < 0)
                    readLength = readPos + (readBuffer.samples - lastReadPos);
                
                if (readLength > 0)
                {
                    float[] data = new float[readLength];
                    readBuffer.GetData(data, lastReadPos);
                    SendVoiceClip.Invoke(new AudioBuffer
                    {
                        data = data
                    });
                    lastReadPos = readPos;
                }
                
                yield return new WaitForSeconds(recordTime);
            }
            isRecording = false;
        }

        private void OnDestroy()
        {
            if(isRecording)
                Microphone.End(microphoneDevice);
        }

        private struct AudioBuffer : IFoundrySerializable
        {
            public float[] data;
            public void Serialize(FoundrySerializer serializer)
            {
                UInt32 length = (UInt32)data.Length;
                serializer.Serialize(length);
                for (int i = 0; i < data.Length; i++)
                    serializer.Serialize(data[i]);
            }
            
            public void Deserialize(FoundryDeserializer deserializer)
            {
                UInt32 length = 0;
                deserializer.Deserialize(ref length);
                data = new float[length];
                for (int i = 0; i < length; i++)
                    deserializer.Deserialize(ref data[i]);
            }
        }
    }
}
