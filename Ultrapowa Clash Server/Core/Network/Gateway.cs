using System;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using UCS.Core.Settings;
using UCS.Core.Threading;
using UCS.PacketProcessing;
using static UCS.Core.Logger;
using static UCS.Core.Settings.UCSControl;

namespace UCS.Core.Network
{
	internal class Gateway
    {
        static Thread T { get; set; }
        public static ManualResetEvent AllDone = new ManualResetEvent(false);
        public Gateway()
        {
            try
            {
                IPHostEntry ipHostInfo = Dns.Resolve(Dns.GetHostName());
                IPAddress ipAddress = ipHostInfo.AddressList[0];
                IPEndPoint localEndPoint = new IPEndPoint(IPAddress.Any, Convert.ToInt32(ConfigurationManager.AppSettings["ServerPort"]));
                Socket listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

                listener.Bind(localEndPoint);
                listener.Listen(100);

                Say();
                Say("TCP Gateway started at " + ipAddress + ":" + localEndPoint.Port);

                UpdateTitle(true);
                while (true)
                {
                    AllDone.Reset();
                    listener.BeginAccept(AcceptCallback, listener);
                    AllDone.WaitOne();
                }
            }
            catch (Exception)
            {
                Error("Gateway failed to start. Restarting");
                Thread.Sleep(5000);
                UCSControl.UCSRestart();
            }
		}

        private void AcceptCallback(IAsyncResult ar)
        {        
            try
            {
				AllDone.Set();

				Socket listener = (Socket)ar.AsyncState;
                Socket handler = listener.EndAccept(ar);

                Say("New TCP Client connected -> " + ((IPEndPoint)handler.RemoteEndPoint).Address);
                Logger.Write("New TCP Client connected -> " + ((IPEndPoint)handler.RemoteEndPoint).Address);

				ResourcesManager.AddClient(handler);
                new Reader(handler, ProcessPacket);
            }
            catch (Exception)
            {
            }
        }

		private void ProcessPacket(Reader read, byte[] data)
		{
			try
			{
				long socketHandle = read.Socket.Handle.ToInt64();
				Client c = ResourcesManager.GetClient(socketHandle);
				c.DataStream.AddRange(data);
				Message p;
                	while (c.TryGetPacket(out p))
                    		p.Receive();
			}
			catch
			{
			}
		}

		public static void Disconnect(Socket handler)
		{
			try
			{
				handler.Shutdown(SocketShutdown.Both);
				handler.Close();
			}
			catch (Exception)
			{
			}
		}
	}	
}
