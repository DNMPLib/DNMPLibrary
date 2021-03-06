﻿namespace DnmpLibrary.Interaction.Protocol
{
    public abstract class Protocol
    {
        public delegate void ReceiveCallback(byte[] data, IEndPoint sourceEndPoint);

        public ReceiveCallback OnReceive;

        public abstract void Send(byte[] data, IEndPoint endPoint);

        public abstract void Start(IEndPoint listenEndPoint);

        public abstract void Stop();

        public abstract IEndPointFactory GetEndPointFactory();
    }
}
