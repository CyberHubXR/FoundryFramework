using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
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

        private readonly Queue<NetworkMessage> sendQueue = new();
        private readonly Queue<NetworkMessage> receiveQueue = new();
        private readonly Mutex sendMutex = new();
        private readonly Mutex receiveMutex = new();
        private IPAddress endpointIP;
        
        public bool IsOpen => socket.State == WebSocketState.Open;
        public bool UnreadMessages => receiveQueue.Count > 0;
        public bool closedGracefully = false;
        
        public static async Task<FoundryWebSocket> Connect(Uri uri)
        {
            var manager = new FoundryWebSocket();
            manager.socket = new ClientWebSocket();
            if (!IPAddress.TryParse(uri.Host, out manager.endpointIP))
            {
                IPHostEntry hostEntry = await Dns.GetHostEntryAsync(uri.Host);
                if (hostEntry.AddressList.Length == 0)
                    throw new Exception("Could not resolve address!");
                foreach (var ip in hostEntry.AddressList)
                {
                    if (ip.AddressFamily != AddressFamily.InterNetwork)
                        continue;
                    manager.endpointIP = ip;
                    break;
                }
            }
            
            manager.socket.Options.KeepAliveInterval = TimeSpan.FromSeconds(2);
            await manager.socket.ConnectAsync(uri, CancellationToken.None);
            return manager;
        }

        public void Start()
        {
            Task.Run(SendMessages);
            Task.Run(ReceiveMessages);
        }

        private async Task SendMessages()
        {
            while (socket.State == WebSocketState.Open)
            {
                if (sendQueue.Count > 0)
                {
                    sendMutex.WaitOne();
                    var message = sendQueue.Dequeue();
                    sendMutex.ReleaseMutex();

                    try
                    {
                        await SendMessageAsync(Message.FromText(message.Header));
                        await SendMessageAsync(Message.FromStream(message.Stream, message.BodyType));
                    }
                    catch (Exception e)
                    {
                        if (!closedGracefully)
                            Debug.LogError("Socket error: " + e);
                    }
                    await message.DisposeAsync();
                }
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

                    receiveMutex.WaitOne();
                    receiveQueue.Enqueue(new NetworkMessage
                    {
                        Header = headerText,
                        Stream = body.Stream,
                        BodyType = body.MessageType
                    });
                    receiveMutex.ReleaseMutex();
                }
                catch(Exception e)
                {
                    if(!closedGracefully)
                        Debug.LogError("Socket error: " + e);
                }
            }
        }

        public Task Stop()
        {
            closedGracefully = true;
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
        
        private byte[] inBuffer = new byte[1024 * 4];
        private async Task<Message> ReceiveMessageAsync()
        {

            var memoryStream = new MemoryStream(1024 * 2);
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
                closedGracefully = true;
                if(socket.State == WebSocketState.Open)
                    socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None).Wait();
                socket.Dispose();
            }
        }

        public async ValueTask DisposeAsync()
        {
            if (socket != null)
            {
                if (socket.State == WebSocketState.Open)
                {
                    Debug.Log("Closing socket.");
                    closedGracefully = true;
                    await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
                }
                socket.Dispose();
            }
        }

        public void SendMessage(NetworkMessage networkMessage)
        {
            sendMutex.WaitOne();
            sendQueue.Enqueue(networkMessage);
            sendMutex.ReleaseMutex();
        }

        public async Task AwaitAllSent()
        {
            while (sendQueue.Count > 0)
            {
                await Task.Delay(100);
            }
        }
        
        /// <summary>
        /// If there are any unread messages, this will return the next one.
        /// </summary>
        /// <returns>NetworkMessage or Null</returns>
        public NetworkMessage ReceiveMessage()
        {
            
            receiveMutex.WaitOne();
            if (receiveQueue.Count == 0)
            {
                receiveMutex.ReleaseMutex();
                return null;
            }
            var value = receiveQueue.Dequeue();
            receiveMutex.ReleaseMutex();
            return value;
        }

        public IPAddress GetIP()
        {
            return endpointIP;
        }
    }
}