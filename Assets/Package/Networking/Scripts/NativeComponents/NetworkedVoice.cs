using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
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
        public NetworkProperty<int> sampleRate = new(44100);
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
            props.Add(sampleRate);
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
                audioOutput.loop = true;
                CreateBuffer();
                SendVoiceClip.AddListener((s, buffer) =>
                {
                    audioBuffer.SetData(buffer.data, writePos);
                    writePos = (writePos + buffer.data.Length) % audioBuffer.samples;
                    if (!audioOutput.isPlaying && writePos > bufferTime * sampleRate.Value)
                    {
                        audioOutput.timeSamples = ((int)(-bufferTime * sampleRate.Value) + audioBuffer.samples) % audioBuffer.samples;
                        audioOutput.Play();
                    }
                });
                sampleRate.OnChanged += CreateBuffer;
                StartCoroutine(MaintainBufferLength());
            }
        }

        private void CreateBuffer()
        {
            audioBuffer = AudioClip.Create("Voice", Mathf.CeilToInt(sampleRate.Value * (bufferTime + recordTime * 2)), 1, sampleRate.Value, false);
            audioOutput.clip = audioBuffer;
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
            var readBuffer = Microphone.Start(microphoneDevice, true,  Mathf.CeilToInt(recordTime * 2.5f), sampleRate.Value);
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

        private IEnumerator MaintainBufferLength()
        {
            while (true)
            {
                var bufferLength = ((writePos - audioOutput.timeSamples + audioBuffer.samples) % audioBuffer.samples) / (float)sampleRate.Value;
                var bufferDelta = bufferTime - bufferLength;
                if (bufferDelta > bufferTime)
                    audioOutput.pitch = Mathf.MoveTowards(audioOutput.pitch, 1.02f, Time.deltaTime);
                else if (bufferDelta < -bufferTime/2)
                    audioOutput.pitch = Mathf.MoveTowards(audioOutput.pitch, 0.90f, Time.deltaTime);
                else 
                    audioOutput.pitch = Mathf.MoveTowards(audioOutput.pitch, 1f, Time.deltaTime * 2f);
                    
                yield return null;
            }
        }

        private void OnDestroy()
        {
            if(isRecording)
                Microphone.End(microphoneDevice);
        }

        private struct AudioBuffer : IFoundrySerializable
        {
            public float[] data;

            public IFoundrySerializer GetSerializer()
            {
                return new Serializer();
            }
            
            public struct Serializer : IFoundrySerializer
            {
                public void Serialize(in object obj, BinaryWriter writer)
                {
                    var buffer = (AudioBuffer)obj;
                    writer.Write((Int32)buffer.data.Length);

                    var quantized = new byte[buffer.data.Length * 2];

                    for (int i = 0; i < buffer.data.Length; i++)
                    {
                        var value = (short)(buffer.data[i] * short.MaxValue);
                        quantized[i * 2] = (byte)(value & 0xFF);         // Lower byte
                        quantized[i * 2 + 1] = (byte)((value >> 8) & 0xFF);
                    }
                    writer.Write(quantized);
                }

                public void Deserialize(ref object obj, BinaryReader reader)
                {
                    var buffer = (AudioBuffer)obj;
                    Int32 length = reader.ReadInt32();
                    var values = reader.ReadBytes(length * 2);
                    buffer.data = new float[length];
                    for (int i = 0; i < length; i++)
                    {
                        short value = (short)(values[i * 2] | (values[i * 2 + 1] << 8));
                        buffer.data[i] = value / (float)short.MaxValue;
                    }
                    
                    obj = buffer;
                }
            }
        }
    }
}
