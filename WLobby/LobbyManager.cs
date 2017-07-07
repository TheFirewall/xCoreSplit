using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using log4net;
using MiNET.Utils;
using MiNET.Worlds;
using MiNET;
using MiNET.Blocks;
using MiNET.BlockEntities;
namespace xCore
{
	public class LobbyManager : LevelManager
	{
		private static readonly ILog Log = LogManager.GetLogger(typeof(LevelManager));

		public xCoreGames xCore { get; set; }

		public LobbyManager(xCoreGames xc)
		{
			xCore = xc;
			Levels = new List<Level>();
			EntityManager = new EntityManager();
		}

		public override Level GetLevel(Player player, string name)
		{
			Level level2 = Levels.FirstOrDefault(l => l.LevelId.Equals(name, StringComparison.InvariantCultureIgnoreCase));
			if (level2 == null)
			{
				AnvilWorldProvider _provider = null;
				for (int i = 0; i <= 4; i++)
				{
					string name2;
					if (i == 0) { name2 = "overworld"; }
					else { name2 = "overworld" + i; }
					GameMode gameMode = Config.GetProperty("GameMode", GameMode.Survival);
					Difficulty difficulty = Config.GetProperty("Difficulty", Difficulty.Normal);
					int viewDistance = Config.GetProperty("ViewDistance", 7);
					AnvilWorldProvider world = _provider;
					if (world == null)
					{
						world = new AnvilWorldProvider();
					}
					var level = new xCoreLevelLobby(name2, world, EntityManager, xCore);
					level.ViewDistance = 8;
					level.isGlobalLobby = true;
					level.Initialize();
					if (_provider == null){
						world.MakeAirChunksAroundWorldToCompensateForBadRendering();
						_provider = world;
					}
					level.Id = i + 1;
					Levels.Add(level);
					return null;
				}
				return null;
			}else{
				foreach (Level l in Levels)
				{
					if (l.PlayerCount < 50)
					{
						return l;
					}
				}
			}
			return null;
		}

		public void AddLevel(Level level)
		{
			Levels.Add(level);
		}

		public Level GetLobbyId(int id)
		{
			if (id <= Levels.Count - 1)
			{
				return Levels[id];
			}
			return Levels[0];
		}
	}
}
