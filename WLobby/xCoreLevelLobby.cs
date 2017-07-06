using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using MiNET;
using MiNET.Worlds;
using MiNET.Entities;
using MiNET.BlockEntities;
using log4net;

namespace xCore
{
	public class xCoreLevelLobby : Level
	{
		private static readonly ILog Log = LogManager.GetLogger(typeof(xCoreLevel));

		public int Id { get; set; }

		public int Time { get; set; }

		public bool isGlobalLobby { get; set; } = false;

		public xCoreGames Core { get; set; }

		//public List<Gadget> Gadgets = new List<Gadget>();

		//public ChestInfo Chest { get; set; }

		public xCoreLevelLobby(string levelId, IWorldProvider worldProvider,EntityManager entitymanager, xCoreGames core) : base(null, levelId, worldProvider, entitymanager)
		{
			Core = core;
			AllowBreak = false;
			AllowBuild = false;
			CurrentWorldTime = 6000;
			IsWorldTimeStarted = false;
		}

		public int Tick = 0;

		public int Seconds = 0;

		protected override void BroadCastMovement(Player[] players, Entity[] entities)
		{
			base.BroadCastMovement(players, entities);

			if (Tick++ >= 20)
			{
				Tick = 0;

				if (isGlobalLobby)
				{
					if (Seconds++ >= 60)
					{
						Seconds = 0;
						BroadcastMessage(Core.Rm.get());
					}
				}
			}
		}
	}
}

