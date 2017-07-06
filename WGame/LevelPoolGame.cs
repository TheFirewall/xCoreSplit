using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using fNbt;
using log4net;
using MiNET;
using MiNET.BlockEntities;
using MiNET.Utils;
using MiNET.Worlds;
using Newtonsoft.Json.Linq;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
namespace xCore
{
	public class LevelPoolGame
	{

		private static readonly ILog Log = LogManager.GetLogger(typeof(LevelPoolGame));

		public List<xCoreLevel> Levels { get; set; } = new List<xCoreLevel>();

		public Dictionary<string, MapInfo> Maps { get; set; } = new Dictionary<string, MapInfo>();

		public xCoreGames Core { get; set; }

		public xCoreInterface Game { get; set; }

		public int LevelCount { get; set; } = 1;

		public List<int> LevelPoolId { get; set; } = new List<int>();

		public bool OnlyRead { get; set; }

		public LevelPoolGame(xCoreGames core, xCoreInterface game, bool onlyread, bool parse)
		{
			Random rnd = new Random();
			var timings = new Stopwatch();
			timings.Start();
			string basepath = Config.GetProperty("PluginDirectory", "Plugins") + "\\" + game.NameGame;
			for (int id = 1; id <= game.ArenaLoaded; id++)
			{
				if (Maps.Count == 0)
				{
					DirectoryInfo dirInfo = new DirectoryInfo(basepath);
					foreach (var dir in dirInfo.GetDirectories())
					{
						var provider = new AnvilWorldProvider(basepath + "\\" + dir.Name);
						var cfg = File.ReadAllText(basepath + "\\" + dir.Name + "\\config.json");
						JObject jobj = JObject.Parse(cfg);
						Maps.Add(dir.Name, new MapInfo(dir.Name, jobj));
						Maps[dir.Name].ProviderCache = provider;
						Maps[dir.Name].ProviderCache.Initialize();
						Maps[dir.Name].ProviderCache.PruneAir();
						Maps[dir.Name].ProviderCache.MakeAirChunksAroundWorldToCompensateForBadRendering();
					}
				}

				string map = Maps.Keys.ElementAt(rnd.Next(0, Maps.Keys.Count - 1));

				Level levelkey = Levels.FirstOrDefault(l => l.LevelId.Equals(game.NameGame + id, StringComparison.InvariantCultureIgnoreCase));
				if (levelkey == null)
				{
					AnvilWorldProvider provider;
					//provider = Maps[map].ProviderCache;
					//if (!Maps[map].ProviderBool)
					//{
					if (onlyread)
						provider = Maps[map].ProviderCache;
					else
						provider = Maps[map].ProviderCache.Clone() as AnvilWorldProvider;
					//}
					var level = new xCoreLevel(provider, game.Context.Server.LevelManager.EntityManager, game, Maps[map], LevelCount);
					LevelCount++;
					level.Initialize();
					SkyLightCalculations.Calculate(level);
					while (provider.LightSources.Count > 0)
					{
						var block = provider.LightSources.Dequeue();
						block = level.GetBlock(block.Coordinates);
						BlockLightCalculations.Calculate(level, block);
					}
					if (parse)
					{
						int X = Maps[map].Center.X;
						int Z = Maps[map].Center.Z;
						List<BlockCoordinates> BE = new List<BlockCoordinates>();
						if (Maps[map].CacheBlockEntites == null)
						{
							int startX = Math.Min(X - 256, X + 256);
							int endX = Math.Max(X - 256, X + 256);
							int startY = Math.Min(11, 129);
							int endY = Math.Max(11, 129);
							int startZ = Math.Min(Z - 256, Z + 256);
							int endZ = Math.Max(Z - 256, Z + 256);
							for (int x = startX; x <= endX; ++x)
							{
								for (int y = startY; y <= endY; ++y)
								{
									for (int z = startZ; z <= endZ; ++z)
									{
										var bc = new BlockCoordinates(x, y, z);
										BlockEntity blockentity;
										if ((blockentity = level.GetBlockEntity(bc)) != null)
										{
											if (blockentity is ChestBlockEntity)
											{
												if (!BE.Contains(blockentity.Coordinates))
													BE.Add(blockentity.Coordinates);
											}
											else if (blockentity is Sign)
											{
												CleanSignText(((Sign)blockentity).GetCompound(), "Text1");
												CleanSignText(((Sign)blockentity).GetCompound(), "Text2");
												CleanSignText(((Sign)blockentity).GetCompound(), "Text3");
												CleanSignText(((Sign)blockentity).GetCompound(), "Text4");
												BE.Add(bc);
											}
										}
									}
								}
							}
							for (int x = startX; x <= endX; ++x)
							{
								for (int z = startZ; z <= endZ; ++z)
								{
									var bc = new BlockCoordinates(x, 127, z);
									MiNET.Blocks.Block b = level.GetBlock(bc);
									if (b.Id == 95)
									{
										level.SetAir(bc, false);
									}
								}
							}
						}
						if (Maps[map].CacheBlockEntites == null)
							Maps[map].CacheBlockEntites = BE;
					}
					game.Initialization(level);
					Levels.Add(level);
				}
			}
			Core = core;
			Game = game;
			OnlyRead = onlyread;
			timings.Stop();
			Log.Info("Loaded " + Levels.Count + " arenas . Load time " + timings.ElapsedMilliseconds + " ms");
			_tickerHighPrecisionTimer = new Timer(QueueTimer, null, 0, 1000);
		}

		private Timer _tickerHighPrecisionTimer;

		public bool CheckTimer { get; set; } = true;

		public ConcurrentQueue<Player> Queue = new ConcurrentQueue<Player>();

		private void QueueTimer(object sender)
		{
			try
			{
				int Number = Queue.Count <= 12 ? Queue.Count : 12;
				for (int i = 0; i < Number; i++)
				{
					xCoreLevel[] ls = GetArenas();
					if (ls.Count() >= 1)
					{
						Player player;
						if (Queue.TryDequeue(out player))
						{
							if (player != null)
							{
								if (!((xPlayer)player).isFindingGame)
								{
									lock (((xPlayer)player).isFindingGameSync)
										((xPlayer)player).isFindingGame = true;
									lock (ls[0]._playerQueueLock)
										ls[0].TempPlayers.TryAdd(player, player);
									player.IsSpectator = false;
									player.SetAllowFly(false);
									Task.Run(delegate { player.SpawnLevel(ls[0], null, true); });
								}
							}
						}
					}
				}

				xCoreLevel[] Arena = GetArenas();
				if (Arena.Count() == 0)
				{
					Random rand = new Random();
					string map = Maps.Keys.ElementAt(rand.Next(0, Maps.Keys.Count - 1));
					if(Maps.ContainsKey(map))
						NewArena(Maps[map]);
				}
				else if (Arena.Count() >= 5 && Levels.Count() >= 15)
					RemoveArena(Arena.Last());
			}
			catch (Exception e)
			{
				Log.Error("Game on " + Game.NameGame, e);
				Log.Info("Game on " + Game.NameGame, e);
			}
		}

		public void addQueue(Player player)
		{
			if (!Queue.Contains(player))
			{
				if (!(player.Level is xCoreLevel) || (player.Level is xCoreLevel && player.GameMode != GameMode.Survival))
				{
					Queue.Enqueue(player);
				}
			}
		}

		private static Regex _regex = new Regex(@"^((\{""extra"":\[)?)""(.*?)""(],""text"":""""})?$");

		public static void CleanSignText(NbtCompound blockEntityTag, string tagName)
		{
			var text = blockEntityTag[tagName].StringValue;
			var replace = Regex.Unescape(_regex.Replace(text, "$3"));
			blockEntityTag[tagName] = new NbtString(tagName, replace);
		}

		public object LevelSync = new object();

		public void NewArena(MapInfo map)
		{
			lock (LevelSync)
			{
				AnvilWorldProvider l;
				if (OnlyRead)
					l = map.ProviderCache;
				else
					l = map.ProviderCache.Clone() as AnvilWorldProvider;
				int id = LevelCount;
				if (LevelPoolId.Count > 0)
				{
					id = LevelPoolId.First();
					LevelPoolId.Remove(id);
				}
				else {
					LevelCount++;
					id = LevelCount;
				}
				var level = new xCoreLevel(l, Game.Context.Server.LevelManager.EntityManager, Game, map, id);
				LevelCount++;
				level.Initialize();
				SkyLightCalculations.Calculate(level);
				while (l.LightSources.Count > 0)
				{
					var block = l.LightSources.Dequeue();
					block = level.GetBlock(block.Coordinates);
					BlockLightCalculations.Calculate(level, block);
				}
				Game.Initialization(level);
				Levels.Add(level);
			}
		}

		public void RemoveArena(xCoreLevel level)
		{
			lock (LevelSync)
			{
				if (Levels.Contains(level))
				{
					Levels.Remove(level);
					level.Close();
					LevelPoolId.Remove(level.Id);
				}
			}
		}

		public xCoreLevel[] GetArenas()
		{
			return Levels.Where(l => !l.PreStarted && (l.GetSpawnedPlayers().Count() + l.TempPlayers.Count()) < l.Slots).OrderByDescending(arg => arg.PlayerCount).ToArray();
		}

		public object LevelWriteLock = new object();

		public int GetPlayerCount()
		{
			int count = 0;
			for (int i = 0; i < Levels.Count; i++)
			{
				xCoreLevel levelarena = Levels[i] as xCoreLevel;
				count += levelarena.PlayerCount;
			}
			return count;
		}
	}
}