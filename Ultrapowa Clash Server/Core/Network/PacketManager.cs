using System;
using System.Collections.Concurrent;
using System.Net.Sockets;
using System.Threading;
using UCS.Logic;
using UCS.PacketProcessing;
using UCS.Packets.Messages.Server;

namespace UCS.Core.Network
{
	internal static class PacketManager
	{
		public static void Receive(this Message p)
		{
            p.Decrypt();
			p.Decode();
			p.Process(p.Client.GetLevel());
		}

		public static void Send(this Message p)
		{
			try
			{
				p.Encode();
                if (p.GetMessageType() == 20000)
                {
                    byte[] sessionKey = ((RC4SessionKey)p).Key;
                    p.Client.UpdateKey(sessionKey);
                }
				p.Process(p.Client.GetLevel());
				p.Client.Socket.BeginSend(p.GetRawData(), 0, p.GetRawData().Length, 0, SendCallback, p.Client.Socket);
			}
			catch (Exception)
			{
			}
		}
		private static void SendCallback(IAsyncResult ar)
		{
			try
			{
				Socket handler = (Socket)ar.AsyncState;
			}
			catch (Exception)
			{
			}
		}
	}
}
