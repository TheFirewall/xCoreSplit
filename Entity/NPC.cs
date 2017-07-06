using System;
using MiNET;
using MiNET.Entities.Passive;
using MiNET.Entities;
using MiNET.Worlds;
using MiNET.Net;
using System.Threading;
using System.Threading.Tasks;
using log4net;
namespace xCore
{
	public class xCoreNPC : PlayerMob
	{
		private static ILog Log = LogManager.GetLogger(typeof(xCoreNPC));

		public xCoreInterface Game { get; set; }

		public string NameString { get; set; }

		public MiNetServer server = null;

		public xCoreLevelLobby LevelLobby { get; set; }

		public xCoreGames Plugin;

		public xCoreNPC(string name, xCoreLevelLobby level, xCoreInterface G, MiNetServer server = null) : base(name, level)
		{
			Game = G;
			this.server = server;
			LevelLobby = level;
		}

		public void Tap(Player player)
		{
			Game.Pool.addQueue(player);
		}

		public int Time = 0;

		public int Minute = 0;

		public override void OnTick()
		{
			Time++;
			if (Time == 20)
			{
				Minute++;
				if (Game != null)
				{
					NameTag = NameString + Game.PlayerCount;
					BroadcastSetEntityData();

				}
				else {
					if (LevelLobby.isGlobalLobby && NameString != null)
					{
						NameTag = string.Format(NameString, server.ServerInfo.NumberOfPlayers);
						BroadcastSetEntityData();
						if (Minute == 60)
						{
							Minute = 0;
							Level.BroadcastMessage(Plugin.Rm.get());
						}
					}
				}
				Time = 0;
			}
		}
	}


	public class Hologram : Entity
	{
		public string StaticName { get; set; } = null;

		public MiNetServer server { get; set; } = null;

		public bool Lobby { get; set; } = true;

		public Hologram(string name, Level level, bool lob = true, MiNetServer server = null) : base((int)EntityType.Slime, level)
		{
			this.server = server;
			Width = 0;
			Length = 0;
			Height = 0;
			NameTag = name;
			Scale = 0;
			HideNameTag = false;
			IsAlwaysShowName = true;
			Lobby = lob;
		}

		public virtual void SetNameTag(string nameTag)
		{
			NameTag = nameTag;

			BroadcastSetEntityData();
		}

		public int Tick = 0;

		public override void OnTick()
		{
			if (Tick++ == 20)
			{
				Tick = 0;
				if (Lobby && StaticName != null)
				{
					NameTag = string.Format(StaticName, server.ServerInfo.NumberOfPlayers);
					BroadcastSetEntityData();
				}
			}
		}
	}

	public class xCoreVillData
	{
		public string Name { get; set; }
		public string NameColor { get; set; }
		public int ItemId { get; set; }
		public string Skin { get; set; }
		public float X { get; set; }
		public float Y { get; set; }
		public float Z { get; set; }
		public float HeadYaw { get; set; }
		public float Yaw { get; set; }
		public float Pitch { get; set; }
	}
}

