using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Numerics;
using AuthME;
using fNbt;
using log4net;
using MiNET;
using MiNET.BlockEntities;
using MiNET.Blocks;
using MiNET.Effects;
using MiNET.Entities.Projectiles;
using MiNET.Items;
using MiNET.Net;
using MiNET.Plugins;
using MiNET.Utils;
using MiNET.Worlds;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace xCore
{
	public class xCoreSkyWars : xCoreInterface
	{
		public string _basepath;
		static ILog Log = LogManager.GetLogger(typeof(xCoreSkyWars));

		public bool Testing { get; set; } = false;

		public string Prefix { get; set; }
		public int ArenaLoaded { get; set; }
		public string NameGame { get; set; }
		public int MinPlayers { get; set; }

		public JObject conf { get; set; }

		public xCoreGames Core { get; set; }
		public LevelPoolGame Pool { get; set; }
		public PluginContext Context { get; set; }
	
		public int PlayerCount { get; set; } = 0;

		public bool CloseGame { get; set; }

		public Dictionary<string, List<PlayerLocation>> SpawnPoints = new Dictionary<string, List<PlayerLocation>>();
		public Dictionary<string, List<PlayerLocation>> DeathPoints = new Dictionary<string, List<PlayerLocation>>();
		public Dictionary<string, List<BlockCoordinates>> Chests = new Dictionary<string, List<BlockCoordinates>>();
		public Dictionary<string, List<BlockCoordinates>> TopChests = new Dictionary<string, List<BlockCoordinates>>();

		public string PrefDB { get; set; } = "sw";

		public string SqlString { get; set; }

		public xCoreSkyWars(xCoreGames core, PluginContext cont)
		{
			Core = core;
			Context = cont;
			Context.PluginManager.LoadPacketHandlers(this);
			NameGame = "SkyWars";

			_basepath = Config.GetProperty("PluginDirectory", "Plugins") + "\\" + NameGame;
			if (!Directory.Exists(_basepath)) Directory.CreateDirectory(_basepath);
			if (!File.Exists(_basepath + "\\config.json"))
			{
				Log.Error("[ERROR] конфиг отсутствует");
				OnDisable();
			}
			string cfg = File.ReadAllText(_basepath + "\\config.json");
			conf = JObject.Parse(cfg);
			ArenaLoaded = (int)conf["ArenaLoaded"];
			Prefix = (string)conf["Prefix"];
			MinPlayers = (int)conf["MinPlayers"];
			Pool = new LevelPoolGame(Core, this, false, true);;
			foreach (KeyValuePair<string, Lang> l in Core.Lang.getLangs())
			{
				l.Value.AddLines(Config.GetProperty("PluginDirectory", "Plugins") + "\\lang\\xCoreSkyWars\\" + l.Value.Name + ".ini");
			}
			SqlString = "kills, deaths, wins, games";
			Log.Info("[Cristalix] SkyWars Enabled");
		}

		public void OnDisable()
		{
			Log.Info("xCoreSkyWars Disable");
		}

		public void Initialization(xCoreLevel level)
		{
			List<PlayerLocation> Loclist;
			if (!SpawnPoints.TryGetValue(level.Map.Name, out Loclist))
			{
				List<PlayerLocation> location = new List<PlayerLocation>();
				List<PlayerLocation> death = new List<PlayerLocation>();
				List<BlockCoordinates> coords = new List<BlockCoordinates>();
				List<BlockCoordinates> topcoords = new List<BlockCoordinates>();
				foreach (BlockCoordinates bc in level.Map.CacheBlockEntites)
				{
					Block be = level.GetBlock(bc);
					if (be is StandingSign)
					{
						Sign sign = level.GetBlockEntity(be.Coordinates) as Sign;
						Log.Warn(sign.Text1);
						if (sign.Text1 == "spawnpoint")
						{
							location.Add(new PlayerLocation(new Vector3(sign.Coordinates.X + 0.5f, sign.Coordinates.Y, sign.Coordinates.Z + 0.5f)));//0.5
							level.SetAir(bc.X, bc.Y, bc.Z, false);
							level.RemoveBlockEntity(be.Coordinates);
						}
						else if(sign.Text1 == "match"){
							death.Add(new PlayerLocation(new Vector3(sign.Coordinates.X + 0.5f, sign.Coordinates.Y, sign.Coordinates.Z + 0.5f)));//0.5
							level.SetAir(bc.X, bc.Y, bc.Z, false);
							level.RemoveBlockEntity(be.Coordinates);
						}
					}
					else if (be is Chest)
					{
						var bcc = new BlockCoordinates(be.Coordinates.X, be.Coordinates.Y + 1, be.Coordinates.Z);
						if (level.GetBlock(bcc) is StandingSign)
						{
							topcoords.Add(bc);
						}
						else {
							coords.Add(bc);
						}
					}
				}
				Chests.Add(level.Map.Name, coords);
				TopChests.Add(level.Map.Name, topcoords);
				DeathPoints.Add(level.Map.Name, death);
				SpawnPoints.Add(level.Map.Name, location);
			}
			else {
				foreach (BlockCoordinates bc in level.Map.CacheBlockEntites)
				{
					BlockEntity be = level.GetBlockEntity(bc);
					if (be is Sign)
					{
						//if (((Sign)be).Text1 == "spawnpoint")
						{
							level.SetAir(be.Coordinates.X, be.Coordinates.Y, be.Coordinates.Z, true);
							level.RemoveBlockEntity(new BlockCoordinates(be.Coordinates.X, be.Coordinates.Y, be.Coordinates.Z));
						}
					}
				}
			}
			ChestFill(level);
		}

		public void BlockBreak(object sender, BlockBreakEventArgs e)
		{
			xCoreLevel level = e.Level as xCoreLevel;
			if (!level.Started || e.Player.GameMode != GameMode.Survival)
			{
				e.Cancel = true;
				return;
			}
		}

		public void BlockPlace(object sender, BlockPlaceEventArgs e)
		{
			xCoreLevel level = e.Level as xCoreLevel;
			if (!level.Started || e.Player.GameMode != GameMode.Survival)
			{
				e.Cancel = true;
				return;
			}
		}

		public void Timer(Player[] players, xCoreLevel level)
		{
			if (level.Status == Status.Game)
			{
				switch (level.Time)
				{
					case 600:
						foreach (xPlayer p in level.GetSpawnedPlayers())
						{
							p.SendMessage(p.PlayerData.lang.get("sw.timer.game.prerefill5"));
						}
						break;
					case 580:
						//
					break;
					case 420:
						foreach (xPlayer p in level.GetSpawnedPlayers())
						{
							p.SendMessage(p.PlayerData.lang.get("sw.timer.game.prerefill2"));
						}
						break;
					case 300:
						ChestFill(level);
						foreach (xPlayer p in level.GetSpawnedPlayers())
						{
							p.SendMessage(p.PlayerData.lang.get("sw.timer.game.refill"));
						}
						break;
					case 0:
						onGameMatch(level);
						break;
					default:
					Player[] Survided = level.GetSurvivalPlayers();
						if (Survided.Count() <= 3)
						{
							if (Survided.Count() <= 1)
							{
								onGameFinish(level);
								return;
							}
							if (level.Time > 30)
							{
								level.Time = 30;
							}
						}
						Core.BossBar.SetNameTag(level, "sw.game.timer.string", true);
						foreach (xPlayer p in level.GetSpawnedPlayers())
						{
							/*level.BroadcastPopup(p,
							 */    Core.BossBar.SendName(p, string.Format(p.PlayerData.lang.get("sw.game.timer.string"), Survided.Count(), level.Map.Name, level.TimeString));
						}
						break;
						
				}
				level.Time--;
			}
			else if (level.Status == Status.Finish)
			{
				
				switch (level.Time)
				{
					case 10:
						foreach (xPlayer p in level.GetSpawnedPlayers())
						{
							p.SendMessage(Prefix + p.PlayerData.lang.get("xcore.finish.to"));
						}
						break;
					case 0:
						onGameReload(level);
						break;
					default:
						if (players.Length == 0)
						{
							onGameReload(level);
						}
						/*foreach (Player p in level.GetSpawnedPlayers())
						{
							level.BroadcastPopup(p, level.TimeString);
						}*/
						Core.BossBar.SetNameTag(level, level.TimeString);
						break;
				}
				level.Time--;
			}
			else if (level.Status == Status.Match)
			{
				if (level.Time > 0)
				{
					Core.BossBar.SetNameTag(level, level.TimeString);
				}
				else if (level.Time == 0)
				{
					level.pvp = true;
					Core.BossBar.SetNameTag(level, "Deathmatch");
					foreach (xPlayer player in level.GetSpawnedPlayers())
					{
						player.SendMessage(Prefix + player.PlayerData.lang.get("sg.timer.dm.start"));
						player.SetNoAi(false);
					}
				}
				Player[] Survided = level.GetSurvivalPlayers();
				if (Survided.Count() <= 1)
				{
					onGameFinish(level);
					return;
				}
				level.Time--;
			}
			else if (level.Status == Status.Start)
			{
				if (players.Length >= MinPlayers)
				{
					switch (level.Time)
					{
						case 160:
						case 120:
						case 60:
						case 30:
						case 20:
						case 10:
						case 4:
						case 3:
						case 2:
						case 1:
							foreach (xPlayer player in level.GetSpawnedPlayers())
							{
								player.SendMessage(Prefix + string.Format(player.PlayerData.lang.get("xcore.start.to"), level.Time));
							}
							break;
						case 5:
							onGamePreStart(level);
							break;
						case 0:
							onGameStart(level);
							break;
						default:
							if (players.Length >= (level.Slots / 2))
							{
								if (level.Time > 60)
								{
									level.Time = 55;
									return;
								}
							}
							break;
					}
					Core.BossBar.SetNameTag(level, level.TimeString);
					level.Time--;
				}
				else {
					Core.BossBar.SetNameTag(level, "xcore.start.noplayers", true);
					foreach (xPlayer player in level.GetSpawnedPlayers())
					{
						Core.BossBar.SendName(player, string.Format(player.PlayerData.lang.get("xcore.start.noplayers"), MinPlayers));
					}
					level.Time = 85;
					level.PreStarted = false;
				}
			}
		}

		public void DeathPlayer(Player Killed)
		{
			xCoreLevel level = Killed.Level as xCoreLevel;
			if (level.Status == Status.Start)
			{
				Killed.Teleport(level.SpawnPoint);
				return;
			}
			if(Killed.GameMode != GameMode.Survival){
				Killed.Teleport(new PlayerLocation(level.Map.Center.X, level.Map.Center.Y, level.Map.Center.Z, 0, 0, 0));
				return;
			}
			var cause = Killed.HealthManager.LastDamageCause;
			if (cause == DamageCause.EntityAttack || cause == DamageCause.Projectile)
			{
				Player Killer = Killed.HealthManager.LastDamageSource as Player;
				if (Killer == null) Killer = ((Projectile)Killed.HealthManager.LastDamageSource).Shooter;
				if (Killer != null)
				{
					Player[] players = level.GetSpawnedPlayers();
					level.Points[Killer.Username]++;
					Killer.SendTitle(null, TitleType.AnimationTimes, 6, 6, 20 * 2);
					switch (level.Points[Killer.Username])
					{                                   
						case 2:
							Killer.SendTitle("Double Kill!", TitleType.SubTitle);
							break;
						case 3:
							Killer.SendTitle("Multi Kill!", TitleType.SubTitle);
							break;
						case 4:
							Killer.SendTitle("Ultra Kill!", TitleType.SubTitle);
							break;
						case 5:
							Killer.SendTitle("M-m-m MONSTER KILL!", TitleType.SubTitle);
							break;
						case 6:
							Killer.SendTitle("HOLY SHIT!", TitleType.SubTitle);
							break;
					}
					Killer.SendTitle(Killer.NameTag, TitleType.Title);

					Random rand = new Random();
					int id = rand.Next(0, 4);
					foreach (xPlayer pl in players)
					{
						pl.SendMessage(Prefix + string.Format(pl.PlayerData.lang.get("sw.death." + id), Killed.Username, Killer.Username));
					}
				}
			}
			else {
				Player Killer = Killed.HealthManager.LastDamageSource as Player;
				if (Killer != null)
				{
					Player[] players = level.GetSpawnedPlayers();
					foreach (xPlayer pl in players)
						pl.SendMessage(Prefix + string.Format(pl.PlayerData.lang.get("sw.death.voidp"), Killed.Username, Killer.Username));
				}
				else {
					Player[] players = level.GetSpawnedPlayers();
					foreach (xPlayer pl in players)
						pl.SendMessage(Prefix + string.Format(pl.PlayerData.lang.get("sw.death.void"), Killed.Username));
				}
				if (level.Status != Status.Finish || level.Status != Status.Start)
				{
					
				}
			}
			Killed.DropInventory();
			Core.SetSpectator(Killed);
			Killed.Teleport(new PlayerLocation(level.Map.Center.X, level.Map.Center.Y + 100,level.Map.Center.Z, 0, 0));
			return;
		}

		public void onGameJoin(Player player, xCoreLevel level)
		{
			
		}

		public void onGameLeave(Player player, xCoreLevel level) {
			
		}

		public void onGamePreStart(xCoreLevel level)
		{
			int i = 0;
			List<PlayerLocation> loc = SpawnPoints[level.Map.Name];
			Player[] pls = level.GetSpawnedPlayers();
			foreach (Player player in pls)
			{
				level.Points.Add(player.Username, 0);
				//player.DynInventory = null;
				for (int ii = 0; ii < player.Inventory.Slots.Count; ++ii)
				{
					if (player.Inventory.Slots[ii] == null || player.Inventory.Slots[ii].Id != 0) player.Inventory.Slots[ii] = new ItemAir();
				}
				if (player.Inventory.Helmet.Id != 0) player.Inventory.Helmet = new ItemAir();
				if (player.Inventory.Chest.Id != 0) player.Inventory.Chest = new ItemAir();
				if (player.Inventory.Leggings.Id != 0) player.Inventory.Leggings = new ItemAir();
				if (player.Inventory.Boots.Id != 0) player.Inventory.Boots = new ItemAir();

				player.IsWorldImmutable = false;
				player.AllowFly = false;
				player.HungerManager.ResetHunger();
				player.HealthManager.ResetHealth();
				((xCoreHungerManager)player.HungerManager).HungerProcess = true;
				//Core.UpdateData(player, "games", 1, this);
				player.SendPlayerInventory();
				player.SendAdventureSettings();
				player.SetNoAi(true);
				player.Teleport(loc[i]);
				i++;
				if (i > 16) Log.Info("мой ник: " + player.Username);
			}
		}

		public void onGameStart(xCoreLevel level)
		{
			foreach (xPlayer p in level.GetSpawnedPlayers())
			{
				p.SetNoAi(false);
				p.SendMessage(p.PlayerData.lang.get("sw.start.go"));
			}
			level.Time = 600;
			level.Status = Status.Game;
			level.Started = true;
			level.pvp = true;
		}

		public void onGameMatch(xCoreLevel level)
		{
			level.Status = Status.Match;
			level.Time = 10;
			level.pvp = false;
			int i = 0;
			List<PlayerLocation> loc = DeathPoints[level.Map.Name];
			foreach (Player player in level.GetSurvivalPlayers())
			{
				player.Teleport(loc[i]);
				player.SetNoAi(true);
				i++;
			}
		}

		public void onGameFinish(xCoreLevel level)
		{
			var players = level.Points.OrderByDescending((KeyValuePair<string, int> arg) => arg.Value);
			//string st = "§a!====§7[§fTop §4Kills§7]§a====!\n";
			string st = "§a§l--------------------§r\n";
			st += new String(' ', (20 - NameGame.Length) / 2) + NameGame + "\n\n";
			string winner = level.GetSurvivalPlayers()[0].Username;
			st += new String(' ', (20 - winner.Length)/2) + "§7" + winner + "\n";
			int ii = 1;
			string ll = "1st Killer - 0";
			foreach (var s in players)
			{
				/*if (ii <= 3)
				{
					st += "§a#" + ii + " §f|§4 " + s.Key + " §f| §4Kills §a" + s.Value + "\n";
				}*/

				if (ii == 1)
				{
					st += new String(' ', (20 - ll.Length) / 2) + "§e1st Killer - §7" + s.Key + " - " + s.Value + "\n";
				}
				else if (ii == 2)
				{
					st += new String(' ', (20 - ll.Length) / 2) + "§62nd Killer - §7" + s.Key + " - " + s.Value + "\n";
				}
				else if (ii == 3)
				{
					st += new String(' ', (20 - ll.Length) /2 ) + "§c3rd Killer - §7" + s.Key + " - " + s.Value + "\n";
				}
				ii++;
			}
			st += "\n§a§l--------------------";
			foreach (xPlayer pl in level.GetSurvivalPlayers())
			{
				Core.BossBar.SetNameTag(level, pl.Username + " win!");
				pl.SendMessage(Prefix + pl.PlayerData.lang.get("sw.finish.win"));
			}
			level.BroadcastMessage(st);
			level.Time = 10;
			level.Status = Status.Finish;
		}

		public void onGameReload(xCoreLevel level)
		{
			level.Status = Status.Reload;
			foreach (Player pl in level.GetSpawnedPlayers())
			{
				Core.SpawnLobby(pl);
			}
			ReloadLevel(level);
		}

		public void ReloadLevel(xCoreLevel level)
		{
			foreach (var entity in level.Entities.Values.ToArray())
			{
				entity.DespawnEntity();
			}
			foreach (var entity in level.BlockEntities.ToArray())
			{
				if (!level.Map.CacheBlockEntites.Contains(entity.Coordinates))
				{
					level.RemoveBlockEntity(entity.Coordinates);
				}
			}
			level.ResetMap();
			level.Points.Clear();
			Initialization(level);
			level.ResetData();
		}

		public short[] armor = { 298, 298, 314, 298, 298, 314, 298, 298, 314, 302, 302, 302 };
		public short[] toparmor = { 
			302, 302, 302, 306, 306, 310,
			303, 303, 303, 307, 307, 311,
			304, 304, 304, 308, 308, 312,
			305, 305, 305, 309, 309, 313
		};

		public short[] armor2 = { 299, 299, 315, 299, 299, 315, 299, 299, 315, 303, 303, 303 };
		public short[] toparmor2 = { 307, 307, 311, 307, 307, 311, 307, 307, 311, 307 };

		public short[] armor3 = { 300, 300, 316, 300, 300, 316, 300, 300, 316, 304, 304, 304 };
		public short[] toparmor3 = { 308, 308, 312, 308, 308, 312, 308, 308, 312, 308 };

		public short[] armor4 = { 301, 301, 317, 301, 301, 317, 301, 301, 317, 305, 305, 305 };
		public short[] toparmor4 = { 309, 309, 313, 309, 309, 313, 309, 309, 313, 309 };

		public short[] sword = { 268, 268, 283, 268, 268, 283, 268, 268, 283, 272, 272, 267 };
		public short[] topsword = { 267, 267, 276, 267, 267, 276, 267, 267, 276, 267 };
		/*public string[] armor2 = { "299", "303", "303", "303", "303", "303", "299", "299", "299", "299", "299", "315", "315", "315", "315", "299", "299", "299", "299", "307", "307", "307", "299", "299", "299", "311", "311", "299", "299" };
		public string[] armor3 = { "300", "304", "304", "304", "304", "304", "300", "300", "300", "300", "300", "316", "316", "316", "316", "300", "300", "300", "300", "308", "308", "308", "300", "300", "300", "312", "312", "300", "300" };
		public string[] armor4 = { "301", "305", "305", "305", "305", "305", "305", "301", "301", "301", "301", "317", "317", "317", "317", "301", "301", "301", "301", "309", "309", "309", "301", "301", "301", "313", "313", "301", "301" };
		public string[] sword1 = { "268", "272", "272", "272", "272", "272", "268", "268", "268", "283", "268", "283", "283", "283", "283", "268", "268", "268", "268", "267", "267", "267", "268", "268", "268", "276", "276", "268", "268" };
*/

		public void ChestFill(xCoreLevel level)
		{
			Random r = new Random();
			foreach (BlockCoordinates coord in Chests[level.Map.Name])
			{
				Inventory inv = level.InventoryManager.GetInventory(coord);

				if (inv != null)
				{
					for (byte slot = 0; slot < inv.Size; slot++) inv.SetSlot(null, slot, new ItemAir());
					inv.SetSlot(null, 0, ItemFactory.GetItem(armor[r.Next(0, armor.Length)], 0, 1));
					inv.SetSlot(null, 1, ItemFactory.GetItem(armor2[r.Next(0, armor2.Length)], 0, 1));
					inv.SetSlot(null, 2, ItemFactory.GetItem(armor3[r.Next(0, armor3.Length)], 0, 1));
					inv.SetSlot(null, 3, ItemFactory.GetItem(armor4[r.Next(0, armor4.Length)], 0, 1));
					/*switch (r.Next(0, 2))
					{
						case 0:
							Item ench = ItemFactory.GetItem(short.Parse(sword1[r.Next(0, sword1.Length)]), 0, 1);
							ench.ExtraData = new NbtCompound { new NbtList("ench") { new NbtCompound { new NbtShort("id", 0), new NbtShort("lvl", 1) } } };
							inv.SetSlot(null, 4, ench);
						break;
						case 1:
						case 2:
							break;
					}*/
					inv.SetSlot(null, 4, ItemFactory.GetItem(sword[r.Next(0, sword.Length)], 0, 1));
					short[] blocks = { 1, 3, 4, 5};
					string[] arr = { "258", "259", "261", "262", "264", "265", "266", "267", "272", "273", "274", "275", "277", "278", "279", "280", "287", "318", "297", "320", "322", "332", "344", "350", "364", "366", "357" };
					int rand = r.Next(1, 8);
					for (int i = 1; i <= rand; i++)
					{
						inv.SetSlot(null, (byte)r.Next(5, 10), ItemFactory.GetItem(short.Parse(arr[r.Next(0, arr.Length)]), 0, (byte)r.Next(1, 12)));
						inv.SetSlot(null, (byte)r.Next(11, 16), ItemFactory.GetItem(blocks[(short)r.Next(0, blocks.Length)], 0, (byte)r.Next(12, 25)));
					}
				}
			}
			foreach (BlockCoordinates coord in TopChests[level.Map.Name])
			{
				Inventory inv = level.InventoryManager.GetInventory(coord);
				if (inv != null)
				{
					for (byte slot = 0; slot < inv.Size; slot++) inv.SetSlot(null, slot, new ItemAir());
					int rand = r.Next(1, 6);
					for (int i = 1; i <= rand; i++)
					{
						inv.SetSlot(null, (byte)r.Next(1, 15), ItemFactory.GetItem(toparmor[r.Next(0, toparmor.Length)], 0, 1));
					}
				}
			}
		}

		public bool isMove { get; set; } = false;

		public bool MovePlayer(McpeMovePlayer package, Player player)//готово
		{
			return true;
		}

		public bool FallPlayer(McpeEntityFall message, Player player)
		{
			return true;
		}

		public bool Interact(McpeInteract package, Player player)//готово
		{
			if (((xCoreLevel)player.Level).pvp == false) return false;
			return true;
		}

		public bool UseItem(McpeUseItem package, Player player, NbtCompound comp)//готово
		{
			return true;
		}

		public bool DropItem(McpeDropItem package, Player player)//готово
		{
			if (((xCoreLevel)player.Level).Status == Status.Game)
			{
				if (player.GameMode == GameMode.Survival)
				{
					return true;
				}
				else {
					return false;
				}
			}
			else {
				return false;
			}
		}

		public bool Action(McpePlayerAction package, Player player)//готово
		{
			return true;
		}
	}
}