using System;
using System.Configuration;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using UCS.Core;
using UCS.Core.Network;
using UCS.Core.Settings;
using UCS.Files.Logic;
using UCS.Helpers;
using UCS.Logic;
using UCS.Logic.AvatarStreamEntry;
using UCS.PacketProcessing.Messages.Server;
using UCS.Packets.Messages.Server;
using static UCS.PacketProcessing.Client;

namespace UCS.PacketProcessing.Messages.Client
{
    // Packet 10101
    internal class LoginMessage : Message
    {
        public LoginMessage(PacketProcessing.Client client, PacketReader br) : base(client, br)
        {
        }

        public string AdvertisingGUID;
        public string AndroidDeviceID;
        public string ClientVersion;
        public string DeviceModel;
        public string FacebookDistributionID;
        public string Region;
        public string MacAddress;
        public string MasterHash;
        public string OpenUDID;
        public string OSVersion;
        public string UserToken;
        public string VendorGUID;
        public int ContentVersion;
        public int LocaleKey;
        public int MajorVersion;
        public int MinorVersion;
        public uint Seed;
        public bool IsAdvertisingTrackingEnabled;
        public bool Android;
        public long UserID;
        public Level level;

        public override void Decode()
        {
            if (Client.State == ClientState.Login)
            {
                try
                {
                    using (var reader = new PacketReader(new MemoryStream(GetData())))
                    {
                        UserID = reader.ReadInt64();
                        UserToken = reader.ReadString();
                        MajorVersion = reader.ReadInt32();
                        ContentVersion = reader.ReadInt32();
                        MinorVersion = reader.ReadInt32();
                        MasterHash = reader.ReadString();
                        reader.ReadString();
                        OpenUDID = reader.ReadString();
                        MacAddress = reader.ReadString();
                        DeviceModel = reader.ReadString();
                        LocaleKey = reader.ReadInt32();
                        Region = reader.ReadString();
                        AdvertisingGUID = reader.ReadString();
                        OSVersion = reader.ReadString();
                        Android = reader.ReadBoolean();
                        reader.ReadString();
                        AndroidDeviceID = reader.ReadString();
                        FacebookDistributionID = reader.ReadString();
                        IsAdvertisingTrackingEnabled = reader.ReadBoolean();
                        VendorGUID = reader.ReadString();
                        Seed = reader.ReadUInt32();
                        reader.ReadByte();
                        reader.ReadString();
                        reader.ReadString();
                        ClientVersion = reader.ReadString();
                    }
                }
                catch 
                {
                    Client.State = ClientState.Exception;
                }
            }
        }

        public override void Process(Level a)
        {
            if (Client.State == ClientState.Login)
            {
                if (Constants.IsRc4)
                {
                    Client.ClientSeed = Seed;
                    new RC4SessionKey(Client).Send();
                }

                if(ParserThread.GetMaintenanceMode() == true)
                {
                    var p = new LoginFailedMessage(Client);
                    p.SetErrorCode(10);
                    p.RemainingTime(ParserThread.GetMaintenanceTime());
                    p.SetMessageVersion(8);
                    p.Send();
                    return;
                }
                                           
                if(Constants.IsPremiumServer == false)
                {
                    if (ResourcesManager.GetOnlinePlayers().Count >= 100)
                    {
                        var p = new LoginFailedMessage(Client);
                        p.SetErrorCode(11);
                        p.SetReason("This is a free Version of UCS. Please Upgrade to Premium on https://ultrapowa.com/forum");
                        p.Send();
                        return;
                    }
                }
                
                int time = Convert.ToInt32(ConfigurationManager.AppSettings["maintenanceTimeleft"]);
                if (time != 0)
                {
                    var p = new LoginFailedMessage(Client);
                    p.SetErrorCode(10);
                    p.RemainingTime(time);
                    p.SetMessageVersion(8);
                    p.Send();
                    return;
                }
                
                if (ConfigurationManager.AppSettings["CustomMaintenance"] != string.Empty)
                {
                    var p = new LoginFailedMessage(Client);
                    p.SetErrorCode(10);
                    p.SetReason(ConfigurationManager.AppSettings["CustomMaintenance"]);
                    p.Send();
                    return;
                }

                var cv2 = ConfigurationManager.AppSettings["ClientVersion"].Split('.');
                var cv = ClientVersion.Split('.');
                if (cv[0] != cv2[0] || cv[1] != cv2[1]) 
                {
                    var p = new LoginFailedMessage(Client);
                    p.SetErrorCode(8);
                    p.SetUpdateURL(Convert.ToString(ConfigurationManager.AppSettings["UpdateUrl"]));
                    p.Send();
                    return;
                }

                if (Convert.ToBoolean(ConfigurationManager.AppSettings["useCustomPatch"]) &&
                    MasterHash != ObjectManager.FingerPrint.sha)
                {
                    var p = new LoginFailedMessage(Client);
                    p.SetErrorCode(7);
                    p.SetResourceFingerprintData(ObjectManager.FingerPrint.SaveToJson());
                    p.SetContentURL(ConfigurationManager.AppSettings["patchingServer"]);
                    p.SetUpdateURL(ConfigurationManager.AppSettings["UpdateUrl"]);
                    p.Send();
                    return;
                }
                CheckClient();
            }
        }

        void LogUser()
        {

            ResourcesManager.LogPlayerIn(level, Client);
            level.Tick();
            level.SetIPAddress(Client.CIPAddress);            
            var loginOk = new LoginOkMessage(Client);
            var avatar = level.GetPlayerAvatar();
            loginOk.SetAccountId(avatar.GetId());
            loginOk.SetPassToken(avatar.GetUserToken());
            loginOk.SetServerMajorVersion(MajorVersion);
            loginOk.SetServerBuild(MinorVersion);
            loginOk.SetContentVersion(ContentVersion);
            loginOk.SetServerEnvironment("prod");
            loginOk.SetDaysSinceStartedPlaying(0);
            loginOk.SetServerTime(Math.Round(level.GetTime().Subtract(new DateTime(1970, 1, 1)).TotalSeconds * 1000).ToString(CultureInfo.InvariantCulture));
            loginOk.SetAccountCreatedDate(avatar.GetAccountCreationDate().ToString());
            loginOk.SetStartupCooldownSeconds(0);
            loginOk.SetCountryCode(avatar.GetUserRegion().ToUpper());
            loginOk.Send();
            var alliance = ObjectManager.GetAlliance(level.GetPlayerAvatar().GetAllianceId());
            if (ResourcesManager.IsPlayerOnline(level))
            {
                var mail = new AllianceMailStreamEntry();
                mail.SetId((int)DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1)).TotalSeconds);
                mail.SetSenderId(0);
                mail.SetSenderAvatarId(0);
  /*FOR FHX*/  //mail.SetSenderName("FHx-Admin");
                mail.SetSenderName("Server Manager");
                mail.SetIsNew(2);
                mail.SetAllianceId(0);
                mail.SetSenderLeagueId(22);
                mail.SetAllianceBadgeData(1526735450);
  /*FOR FHX*/  //mail.SetAllianceName("FHx-Server");
                mail.SetAllianceName("Server Admin");
                mail.SetMessage(ConfigurationManager.AppSettings["AdminMessage"]);
                mail.SetSenderLevel(500);
                var p = new AvatarStreamEntryMessage(level.GetClient());
                p.SetAvatarStreamEntry(mail);
                p.Send();
            }

            if (alliance != null)
            {
                  new AllianceFullEntryMessage(Client, alliance).Send();
                  new AllianceStreamMessage(Client, alliance).Send();
                  new AllianceWarHistoryMessage(Client, alliance).Send();
                //PacketManager.ProcessOutgoingPacket (new AllianceWarMapDataMessage(Client)); Don't activate it (not done!)
            }
            new AvatarStreamMessage(Client).Send();
            new OwnHomeDataMessage(Client, level).Send();
            new BookmarkMessage(Client).Send();
        }

        void CheckClient()
        {
            if (UserID == 0 || string.IsNullOrEmpty(UserToken))
            {
                NewUser();
                return;
            }

            level = ResourcesManager.GetPlayer(UserID);
            if (level != null)
            {
                if (level.Banned())
                {
                    var p = new LoginFailedMessage(Client);
                    p.SetErrorCode(11);
                    p.Send();
                    return;
                }
                if (string.Equals(level.GetPlayerAvatar().GetUserToken(), UserToken, StringComparison.Ordinal))
                {
                    LogUser();
                }
                else
                {
                    var p = new LoginFailedMessage(Client);
                    p.SetErrorCode(11);
/*FOR FHX*/         //p.SetReason("Please clear the Data of your FHx apps. \n\nSettings -> Application Manager -> Clear Data.(#1)\n\nMore Info, please check our official Website.\nOfficial Site: http://www.fhx-server.com");                  
                    p.SetReason("We have some Problems with your Account. Please clean your App Data. https://ultrapowa.com/forum");
                    p.Send();
                    return;
                }
            }
            else
            {
                var p = new LoginFailedMessage(Client);
                p.SetErrorCode(11);
/*FOR FHX*/     //p.SetReason("Please clear the Data of your FHx apps. \n\nSettings -> Application Manager -> Clear Data.(#1)\n\nMore Info, please check our official Website.\nOfficial Site: http://www.fhx-server.com");                                   
                p.SetReason("We have some Problems with your Account. Please clean your App Data. https://ultrapowa.com/forum");
                p.Send();
                return;
            }
        }

        void NewUser()
        {
            level = ObjectManager.CreateAvatar(0, null);
            if (string.IsNullOrEmpty(UserToken))
            {
                byte[] tokenSeed = new byte[20];
                new Random().NextBytes(tokenSeed);
                using (SHA1 sha = new SHA1CryptoServiceProvider())
                    UserToken = BitConverter.ToString(sha.ComputeHash(tokenSeed)).Replace("-", string.Empty);
            }

            level.GetPlayerAvatar().SetRegion(Region.ToUpper());
            level.GetPlayerAvatar().SetToken(UserToken);
            level.GetPlayerAvatar().InitializeAccountCreationDate();
            level.GetPlayerAvatar().SetAndroid(Android);

            var user = DatabaseManager.Single().Save(level);
            user.Wait();
            LogUser();
        }
    }
}
