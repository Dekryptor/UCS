using System;
using System.Collections.Generic;
using UCS.Helpers;
using UCS.Logic;

namespace UCS.PacketProcessing.Messages.Server
{
    // Packet 24101
    internal class OwnHomeDataMessage : Message
    {
        public OwnHomeDataMessage(PacketProcessing.Client client, Level level) : base(client)
        {
            SetMessageType(24101);
            Player = level;
        }

        public Level Player { get; set; }

        public override void Encode()
        {
            var Avatar = Player.GetPlayerAvatar();
            var data = new List<byte>();
            var home = new ClientHome(Avatar.GetId());

            home.SetShieldTime(Avatar.RemainingShieldTime);
            home.SetHomeJSON(Player.SaveToJSON());

            data.AddInt32(0); 
            data.AddInt32(-1); 
            data.AddInt32((int) Player.GetTime().Subtract(new DateTime(1970, 1, 1)).TotalSeconds);
            data.AddRange(home.Encode());
            data.AddRange(Avatar.Encode());
            if(Avatar.State == ClientAvatar.UserState.Editmode)
            {
                data.AddInt32(1);               
            }
            else
            {
                data.AddInt32(0);
            }
            data.AddInt32(0);
            data.AddInt64(0); 
            data.AddInt64(0); 
            data.AddInt64(0); 

            Encrypt(data.ToArray());
        }
    }
}
