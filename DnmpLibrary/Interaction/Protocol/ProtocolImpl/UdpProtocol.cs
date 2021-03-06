﻿using System;
using System.Net;
using System.Net.Sockets;
using DnmpLibrary.Interaction.Protocol.EndPointFactoryImpl;
using DnmpLibrary.Interaction.Protocol.EndPointImpl;
using NLog;

namespace DnmpLibrary.Interaction.Protocol.ProtocolImpl
{
    public class UdpProtocol : Protocol, IDisposable
    {
        private readonly Logger logger = LogManager.GetCurrentClassLogger();

        private Socket socket;

        private readonly int receiveBufferSize;
        private readonly int sendBufferSize;

        private class StateObject
        {
            public readonly byte[] Buffer;
            public EndPoint EndPoint;

            public StateObject(int bufferSize)
            {
                Buffer = new byte[bufferSize];
            }
        }

        public UdpProtocol(int receiveBufferSize = 1048576, int sendBufferSize = 1048576)
        {
            this.receiveBufferSize = receiveBufferSize;
            this.sendBufferSize = sendBufferSize;
        }

        public override void Send(byte[] data, IEndPoint endPoint)
        {
            if (!(endPoint is RealIPEndPoint))
                return;
            
            socket.SendTo(data, ((RealIPEndPoint)endPoint).RealEndPoint);
        }

        public override void Start(IEndPoint listenEndPoint)
        {
            if (!(listenEndPoint is RealIPEndPoint))
                return;

            var realEndPoint = ((RealIPEndPoint) listenEndPoint).RealEndPoint;

            socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            socket.Bind(realEndPoint);
            socket.IOControl(
                (IOControlCode)(-1744830452),
                new byte[] { 0, 0, 0, 0 },
                null
            );
            socket.ReceiveBufferSize = receiveBufferSize;
            socket.SendBufferSize = sendBufferSize;
            var stateObject = new StateObject(receiveBufferSize)
            {
                EndPoint = new IPEndPoint(IPAddress.Any, 0)
            };
            socket.BeginReceiveFrom(stateObject.Buffer, 0, stateObject.Buffer.Length, SocketFlags.None,
                ref stateObject.EndPoint, SocketReceiveCallback, stateObject);
        }


        private void SocketReceiveCallback(IAsyncResult asyncResult)
        {
            var currentStateObject = (StateObject)asyncResult.AsyncState;
            while (true)
            {
                try
                {
                    socket.EndReceiveFrom(asyncResult, ref currentStateObject.EndPoint);
                    break;
                }
                catch (ObjectDisposedException)
                {
                    return;
                }
                catch (Exception e)
                {
                    logger.Error(e, "Exception in EndReceiveFrom");
                }
            }

            OnReceive(currentStateObject.Buffer, new RealIPEndPoint((IPEndPoint)currentStateObject.EndPoint));

            currentStateObject.EndPoint = new IPEndPoint(IPAddress.Any, 0);
            while (true)
            {
                try
                {
                    socket.BeginReceiveFrom(currentStateObject.Buffer, 0, currentStateObject.Buffer.Length,
                        SocketFlags.None,
                        ref currentStateObject.EndPoint, SocketReceiveCallback, currentStateObject);
                    break;
                }
                catch (ObjectDisposedException)
                {
                    return;
                }
                catch (Exception e)
                {
                    logger.Error(e, "Exception in BeginReceiveFrom");
                }
            }
        }

        public override void Stop()
        {
            socket.Shutdown(SocketShutdown.Both);
            socket.Close();
        }

        public override IEndPointFactory GetEndPointFactory()
        {
            return new RealIPEndPointFactory();
        }

        public void Dispose()
        {
            socket?.Dispose();
        }
    }
}
