using System;
using System.IO;
using UCS.Core;
using UCS.Core.Network;
using UCS.Helpers;
using UCS.Logic;
using UCS.PacketProcessing.Commands.Client;
using UCS.PacketProcessing.Messages.Server;
using  UCS.PacketProcessing.Commands.Server;

namespace UCS.PacketProcessing.Messages.Client
{
    // Packet 14301
    internal class CreateAllianceMessage : Message
    {
        public CreateAllianceMessage(PacketProcessing.Client client, PacketReader br) : base(client, br)
        {

        }

        int m_vAllianceBadgeData;
        string m_vAllianceDescription;
        string m_vAllianceName;
        int m_vAllianceOrigin;
        int m_vAllianceType;
        int m_vRequiredScore;
        int m_vWarFrequency;
        byte m_vWarAndFriendlyStatus;

        public override void Decode()
        {
            using (PacketReader br = new PacketReader(new MemoryStream(GetData())))
            {
                m_vAllianceName = br.ReadString();
                m_vAllianceDescription = br.ReadString();
                m_vAllianceBadgeData = br.ReadInt32WithEndian();
                m_vAllianceType = br.ReadInt32WithEndian();
                m_vRequiredScore = br.ReadInt32WithEndian();
                m_vWarFrequency = br.ReadInt32WithEndian();
                m_vAllianceOrigin = br.ReadInt32WithEndian();
                m_vWarAndFriendlyStatus = br.ReadByte();
                //Console.WriteLine(m_vWarAndFriendlyStatus);
            }
        }

        public override void Process(Level level)
        {       
        if (m_vAllianceName == null)   
            m_vAllianceName = "Clan";
        
            var alliance = ObjectManager.CreateAlliance(0);
            alliance.SetAllianceName(m_vAllianceName);
            alliance.SetAllianceDescription(m_vAllianceDescription);
            alliance.SetAllianceType(m_vAllianceType);
            alliance.SetRequiredScore(m_vRequiredScore);
            alliance.SetAllianceBadgeData(m_vAllianceBadgeData);
            alliance.SetAllianceOrigin(m_vAllianceOrigin);
            alliance.SetWarFrequency(m_vWarFrequency);
            alliance.SetWarAndFriendlytStatus(m_vWarAndFriendlyStatus);
            level.GetPlayerAvatar().SetAllianceId(alliance.GetAllianceId());

            var member = new AllianceMemberEntry(level.GetPlayerAvatar().GetId());
            member.SetRole(2);
            alliance.AddAllianceMember(member);

            var b = new JoinedAllianceCommand();
            b.SetAlliance(alliance);

            var d = new AllianceRoleUpdateCommand();
            d.SetAlliance(alliance);
            d.SetRole(2);
            d.Tick(level);

            var a = new AvailableServerCommandMessage(Client);
            a.SetCommandId(1);
            a.SetCommand(b);

            var c = new AvailableServerCommandMessage(Client);
            c.SetCommandId(8);
            c.SetCommand(d);

            a.Send();
            c.Send();
        }
    }
}
