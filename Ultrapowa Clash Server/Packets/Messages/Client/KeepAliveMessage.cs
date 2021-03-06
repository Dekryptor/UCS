﻿using System.IO;
using UCS.Core.Network;
using UCS.Helpers;
using UCS.Logic;
using UCS.PacketProcessing.Messages.Server;

namespace UCS.PacketProcessing.Messages.Client
{
    // Packet 10108
    internal class KeepAliveMessage : Message
    {
        public KeepAliveMessage(PacketProcessing.Client client, PacketReader br) : base(client, br)
        {
        }

        public override void Process(Level level)
        {
            new KeepAliveOkMessage(Client, this).Send();
        }
    }
}