using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Foundry.Core.Serialization;
using UnityEngine;

namespace Foundry.Package.Networking.Scripts
{
    public class NetworkMessage: IAsyncDisposable, IDisposable
    {
        public string Header;
        public WebSocketMessageType BodyType;
        public MemoryStream Stream;

        public void Dispose()
        {
            Stream?.Dispose();
        }

        public async ValueTask DisposeAsync()
        {
            if (Stream != null) await Stream.DisposeAsync();
        }

        public static NetworkMessage FromText(string header, string body)
        {
            var stream = new MemoryStream();
            var writer = new StreamWriter(stream);
            writer.Write(body);
            writer.Flush();
            stream.Seek(0, SeekOrigin.Begin);
            return new NetworkMessage
            {
                Header = header,
                BodyType = WebSocketMessageType.Text,
                Stream = stream
            };
        }

        public static NetworkMessage FromBytes(string header, byte[] body)
        {
            var stream = new MemoryStream(body);
            return new NetworkMessage
            {
                Header = header,
                BodyType = WebSocketMessageType.Binary,
                Stream = stream
            };
        }
        
        public string AsText()
        {
            Debug.Assert(BodyType == WebSocketMessageType.Binary, "Cannot convert binary message to text.");
            Stream.Seek(0, SeekOrigin.Begin);
            var reader = new StreamReader(Stream, Encoding.UTF8);
            return reader.ReadToEnd();
        }
            
        public BinaryReader AsReader()
        {
            Debug.Assert(BodyType == WebSocketMessageType.Binary, "Cannot deserialize text message.");
            Stream.Seek(0, SeekOrigin.Begin);
            return new BinaryReader(Stream);
        }
    }
    
    public class FoundryWebSocket: IDisposable, IAsyncDisposable
    {
        private ClientWebSocket socket;

        private readonly Queue<NetworkMessage> oQueue = new();
        private readonly Queue<NetworkMessage> iQueue = new();
        
        public bool IsOpen => socket.State == WebSocketState.Open;
        public bool UnreadMessages => iQueue.Count > 0;

        public static async Task<FoundryWebSocket> Connect(string uri)
        {
            var manager = new FoundryWebSocket();
            manager.socket = new ClientWebSocket();
            await manager.socket.ConnectAsync(new Uri(uri), CancellationToken.None);
            return manager;
        }

        public void Start()
        {
            Task.Run(ReceiveMessages);
            Task.Run(SendMessages);
        }

        private async Task SendMessages()
        {
            var lastSend = DateTime.Now;
            while (socket.State == WebSocketState.Open)
            {
                if (oQueue.Count > 0)
                {
                    var message = oQueue.Dequeue();
                    
                    await SendMessageAsync(Message.FromText(message.Header));
                    await SendMessageAsync(Message.FromStream(message.Stream, message.BodyType));
                    lastSend = DateTime.Now;
                    await message.DisposeAsync();
                }
                else if ((DateTime.Now - lastSend).TotalSeconds > 2)
                {
                    await SendMessageAsync(Message.FromText("heartbeat"));
                    lastSend = DateTime.Now;
                }
                else
                    await Task.Delay(1);
            }
        }
        
        private async Task ReceiveMessages()
        {
            while (socket.State == WebSocketState.Open)
            {
                try
                {
                    var header = await ReceiveMessageAsync();
                    if (header.MessageType != WebSocketMessageType.Text)
                        throw new Exception("Expected text header message, got binary.");
                    string headerText = header.AsText();
                        
                    var body = await ReceiveMessageAsync();
                        
                    iQueue.Enqueue(new NetworkMessage
                    {
                        Header = headerText,
                        Stream = body.Stream,
                        BodyType = body.MessageType
                    });
                }
                catch(Exception e)
                {
                    Debug.LogError("Socket error: " + e);
                }
            }
        }

        public Task Stop()
        {
            if (socket.State == WebSocketState.Open)
                return socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
            return Task.CompletedTask;
        }

        private class Message: IDisposable, IAsyncDisposable
        {
            public WebSocketMessageType MessageType;
            public MemoryStream Stream;
            
            public static Message FromText(string text)
            {
                var stream = new MemoryStream();
                var writer = new StreamWriter(stream);
                writer.Write(text);
                writer.Flush();
                stream.Seek(0, SeekOrigin.Begin);
                return new Message
                {
                    MessageType = WebSocketMessageType.Text,
                    Stream = stream
                };
            }
            
            public static Message FromStream(MemoryStream stream, WebSocketMessageType type)
            {
                return new Message
                {
                    MessageType = type,
                    Stream = stream
                };
            }
            
            public string AsText()
            {
                Stream.Seek(0, SeekOrigin.Begin);
                var reader = new StreamReader(Stream, Encoding.UTF8);
                return reader.ReadToEnd();
            }
            
            public BinaryReader AsReader()
            {
                Stream.Seek(0, SeekOrigin.Begin);
                return new BinaryReader(Stream);
            }

            public void Dispose()
            {
                Stream?.Dispose();
            }

            public async ValueTask DisposeAsync()
            {
                if (Stream != null) await Stream.DisposeAsync();
            }
        }
        
        private async Task SendMessageAsync(Message message)
        {
            await socket.SendAsync(new ArraySegment<byte>(message.Stream.ToArray()), message.MessageType, true, CancellationToken.None);
        }
        
        private byte[] inBuffer = new byte[1024];
        private async Task<Message> ReceiveMessageAsync()
        {

            var memoryStream = new MemoryStream();
            WebSocketReceiveResult result;
            do
            {
                result = await socket.ReceiveAsync(new ArraySegment<byte>(inBuffer), CancellationToken.None);
                if (result.MessageType == WebSocketMessageType.Close)
                {
                    await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
                    throw new WebSocketException("Server initiated close before message was fully received.");
                }

                memoryStream.Write(inBuffer, 0, result.Count);
            }
            while (!result.EndOfMessage);

            memoryStream.Seek(0, SeekOrigin.Begin);

            return new Message
            {
                MessageType = result.MessageType,
                Stream = memoryStream
            };
        }
        
        public void Dispose()
        {
            if (socket != null)
            {
                if(socket.State == WebSocketState.Open)
                    socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None).Wait();
                socket.Dispose();
            }
        }

        public async ValueTask DisposeAsync()
        {
            if (socket != null)
            {
                if(socket.State == WebSocketState.Open)
                    await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
                socket.Dispose();
            }
        }

        public void SendMessage(NetworkMessage networkMessage)
        {
            oQueue.Enqueue(networkMessage);
        }
        
        /// <summary>
        /// If there are any unread messages, this will return the next one.
        /// </summary>
        /// <returns>NetworkMessage or Null</returns>
        public NetworkMessage ReceiveMessage()
        {
            if (iQueue.Count == 0)
                return null;
            return iQueue.Dequeue();
        }
    }
}