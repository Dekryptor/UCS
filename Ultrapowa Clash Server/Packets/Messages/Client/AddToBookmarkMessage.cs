﻿using System.IO;
using UCS.Core;
using UCS.Core.Network;
using UCS.Helpers;
using UCS.Logic;
using UCS.Logic.DataSlots;
using UCS.PacketProcessing.Messages.Server;

namespace UCS.PacketProcessing.Messages.Client
{
    // Packet 14343
    internal class AddToBookmarkMessage : Message
    {
        public AddToBookmarkMessage(PacketProcessing.Client client, PacketReader br) : base(client, br)
        {
        }

        private long id;

        public override void Decode()
        {
            using (PacketReader br = new PacketReader(new MemoryStream(GetData())))
            {
                id = br.ReadInt64WithEndian();
            }
        }

        public override void Process(Level level)
        {
            BookmarkSlot ds = new BookmarkSlot(id);
            level.GetPlayerAvatar().BookmarkedClan.Add(ds);
            new BookmarkAddAllianceMessage(level.GetClient()).Send();
            var user = DatabaseManager.Single().Save(level);
            user.Wait();
        }
    }
}