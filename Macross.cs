using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AuthME;
using fNbt;
using log4net;
using MiNET;
using MiNET.Items;
using MiNET.Net;
using MiNET.Plugins;
using MiNET.Plugins.Attributes;
using MiNET.Utils;
using MiNET.Worlds;
using Newtonsoft.Json;

namespace xCore
{
	[Plugin(Author = "Overlord", Description = "MiniGames Core", PluginName = "Macross Core", PluginVersion = "2.0")]
	public class xCoreGames : Plugin, IStartup
	{
		private static ILog Log = LogManager.GetLogger(typeof(xCoreGames));

		public Account Auth { get; private set; }

		public LangManager Lang { get; private set; }

		public RandomMessage Rm { get; private set; }

		public List<xCoreInterface> Games { get; set; }

		public List<xCoreVillData> Villagers { get; set; }

		public object Songs { get; set; }

		public Boss BossBar { get; set; }

		public static int StartTime { get; set; } = 50;

		public void Configure(MiNetServer server)
		{
			Log.Info("Startup begun.");

			//server.MotdProvider = new CrMotdProvider();
			server.PlayerFactory = new xPlayerFactory() { xCore = this };
			server.LevelManager = new LobbyManager(this);

			Log.Info("Startup complete.");
		}

		protected override void OnEnable()
		{
			//Context.Server.PlayerFactory = new xPlayerFactory() { xCore = this };
			//Context.Server.LevelManager = new LobbyManager(this);
			Log.Info("[Cristalix] xThreadPool enabled");
			Log.Info("[Cristalix] xDedicatedTaskFactory enabled");
			Log.Info("[Cristalix] LobbyManager enabled");
			Auth = new Account();
			Auth.OnEnable(this, Context);
			Log.Info("[Cristalix] AccountManager enabled");
			Lang = Auth.Lang;
			foreach (KeyValuePair<string, Lang> l in Lang.getLangs())
			{
				l.Value.AddLines(Config.GetProperty("PluginDirectory", "Plugins") + "\\lang\\xCore\\" + l.Value.Name + ".ini");
				l.Value.AddLines(Config.GetProperty("PluginDirectory", "Plugins") + "\\lang\\DynItem\\" + l.Value.Name + ".ini");
			}
			string _basepath = Config.GetProperty("PluginDirectory", "Plugins") + "\\xCore";
			string vill = File.ReadAllText(_basepath + "\\villagers.json");
			if (vill != null)
			{
				Villagers = JsonConvert.DeserializeObject<List<xCoreVillData>>(@vill);
				foreach (var v in Villagers) v.NameColor = v.NameColor.Replace('&', '§');
			}

			Rm = new RandomMessage("Global", Config.GetProperty("PluginDirectory", "Plugins") + "\\lang\\xCore\\RandomMessage.ini");
			//ItemSigner.DefaultItemSigner = new HashedItemSigner();
			//Log.Info("[Cristalix] PartyManager enabled");
			Games = new List<xCoreInterface>();
			Games.Add(new xCoreSkyWars(this, Context));
			//Games.Add(new xCoreBedWars(this, Context));
			//Games.Add(new xCoreHiddenJoker(this, Context));
			//Games.Add(new xCoreTNTRun(this, Context));
			//Games.Add(new xCoreBlockHunt(this, Context));
			//Games.Add(new xCoreBlockParty(this, Context));
			//Games.Add(new xCoreSurvivalGames(this, Context));
			//Chest = new Chests(this);
			Context.Server.LevelManager.GetLevel(null, "overworld");
			Log.Info("[Cristalix] CChest enabled");
			LoadLobby();
			//ServersMenu();
			//ProfileMenu();
			//ChestFun();
			//Settings();
			Log.Info("[Cristalix] AntiCheat enabled");
			//Songs = new PlayNoteBlockSong(Context.Server.LevelManager.Levels.ToArray());
			BossBar = new Boss(Context.Server.LevelManager.Levels[0]);
			Context.Server.LevelManager.EntityManager.AddEntity(BossBar);
			Log.Info("[Cristalix] xCore enabled! Version 2.0!");
		}

		private void LoadLobby()
		{
			foreach (var villdata in Villagers)
				foreach (var game in Games)
					if (villdata.Name == game.NameGame)
						foreach (xCoreLevelLobby lobby in Context.Server.LevelManager.Levels)
						{
							string name = villdata.NameColor + "\n§6Online " + 2033;
							var npc = new xCoreNPC(name, lobby, game);
							npc.NameString = villdata.NameColor + "\n§6Online ";
							npc.KnownPosition = new PlayerLocation(villdata.X, villdata.Y, villdata.Z, villdata.HeadYaw, villdata.Yaw, villdata.Pitch);
							string _basepath = Config.GetProperty("PluginDirectory", "Plugins") + "\\xCore";
							npc.Skin.Texture = Skin.GetTextureFromFile(_basepath + "\\" + villdata.Skin + ".png");
							npc.SpawnEntity();
						}

			foreach (xCoreLevelLobby lobby in Context.Server.LevelManager.Levels)
			{
				List<string> str = new List<string>();
				str.Add("§l§eДобро пожаловать§r");
				str.Add("§l§3Cristalix §aPocket Edition§r\n");
				str.Add("§l§e{0} §8игроков онлайн§r\n");
				str.Add("§l§bhttp://Сristalix.net§r");
				str.Add("§l§bhtts://vk.com/cristalixpe§r");
				//string name5 = "Cristalix";
				var npc = new Hologram(string.Format(str.CenterName(), 28193), lobby, true, Context.Server);
				npc.StaticName = str.CenterName();
				var pos = lobby.SpawnPoint.Clone() as PlayerLocation;
				pos.Z += 3;
				npc.KnownPosition = pos;
				npc.HealthManager.IsInvulnerable = true;
				npc.SpawnEntity();

				string name6 = "§7█§7█§7█§7█§7█§7█§7█§7█§9█§7█§7█§7█§7█§7█§7█§7█§7█§7█§7█§7█§7█§7█§7█§7█§7█§7█§7█\n§7█§7█§7█§7█§7█§7█§7█§9█§9█§9█§7█§7█§7█§7█§7█§7█§7█§7█§8█§7█§7█§7█§7█§7█§7█§7█§7█\n§7█§7█§7█§7█§7█§7█§9█§9█§9█§9█§9█§7█§7█§7█§7█§7█§7█§8█§8█§8█§7█§7█§7█§7█§7█§7█§7█\n§7█§7█§7█§7█§7█§9█§9█§9█§9█§9█§9█§9█§7█§7█§7█§7█§8█§8█§8█§8█§8█§7█§7█§7█§7█§7█§7█\n§7█§7█§7█§7█§9█§9█§9█§9█§9█§9█§9█§9█§9█§7█§7█§8█§8█§8█§8█§8█§8█§8█§7█§7█§7█§7█§7█\n§7█§7█§7█§9█§9█§9█§9█§9█§9█§9█§9█§9█§9█§9█§7█§7█§8█§8█§8█§8█§8█§7█§7█§8█§7█§7█§7█\n§7█§7█§9█§9█§9█§9█§9█§9█§9█§9█§9█§9█§9█§9█§9█§7█§7█§8█§8█§8█§7█§7█§8█§8█§8█§7█§7█\n§7█§9█§9█§9█§9█§9█§9█§9█§9█§9█§9█§9█§9█§9█§9█§9█§7█§7█§8█§7█§7█§8█§8█§8█§8█§8█§7█\n§9█§9█§9█§9█§9█§9█§9█§9█§9█§9█§9█§9█§9█§9█§9█§9█§9█§7█§7█§7█§8█§8█§8█§8█§8█§8█§8█\n§7█§9█§9█§9█§9█§9█§9█§9█§9█§9█§9█§9█§9█§9█§9█§9█§7█§7█§8█§7█§7█§8█§8█§8█§8█§8█§7█\n§7█§7█§9█§9█§9█§9█§9█§9█§9█§9█§9█§9█§9█§9█§9█§7█§7█§8█§8█§8█§7█§7█§8█§8█§8█§7█§7█\n§7█§7█§7█§9█§9█§9█§9█§9█§9█§9█§9█§9█§9█§9█§7█§7█§8█§8█§8█§8█§8█§7█§7█§8█§7█§7█§7█\n§7█§7█§7█§7█§9█§9█§9█§9█§9█§9█§9█§9█§9█§7█§7█§8█§8█§8█§8█§8█§8█§8█§7█§7█§7█§7█§7█\n§7█§7█§7█§7█§7█§9█§9█§9█§9█§9█§9█§9█§7█§7█§7█§7█§8█§8█§8█§8█§8█§7█§7█§7█§7█§7█§7█\n§7█§7█§7█§7█§7█§7█§9█§9█§9█§9█§9█§7█§7█§7█§7█§7█§7█§8█§8█§8█§7█§7█§7█§7█§7█§7█§7█\n§7█§7█§7█§7█§7█§7█§7█§9█§9█§9█§7█§7█§7█§7█§7█§7█§7█§7█§8█§7█§7█§7█§7█§7█§7█§7█§7█\n§7█§7█§7█§7█§7█§7█§7█§7█§9█§7█§7█§7█§7█§7█§7█§7█§7█§7█§7█§7█§7█§7█§7█§7█§7█§7█§7█";
				var npc2 = new Hologram(name6, lobby, true, Context.Server);
				var pos2 = lobby.SpawnPoint.Clone() as PlayerLocation;
				pos2.X = 4.5f;
				pos2.Y = 141;
				pos2.Z = 39.5f;
				npc2.KnownPosition = pos2;
				npc2.HealthManager.IsInvulnerable = true;
				npc2.SpawnEntity();
			}
		}

		public Player getPlayer(string username)
		{
			return Auth.getPlayer(username);
		}

		public override void OnDisable()
		{
			Log.Info("Stopping MiNET");
		}

		[Command(Name = "hub")]
		public void SpawnHub(Player player)
		{
			SpawnLobby(player);
		}

		[Command(Name = "lobby")]
		public void SpawnLobby(Player player)
		{
			if (player.Level is xCoreLevel)
			{
				if (player.GameMode != GameMode.Survival || player.AllowFly)
				{
					player.IsSpectator = false;
					player.AllowFly = false;
					//player.SetAllowFly(true);
					player.IsAlwaysShowName = true;
					//player.SetGameMode(GameMode.Survival);
					player.GameMode = GameMode.Survival;
				}

				player.IsInvisible = false;
				player.NameTag = player.Username;
				//lock (((xPlayer)player).DynamicInvSync)
				//	player.DynInventory = null;
				player.HealthManager.MaxHealth = 200;
				player.HealthManager.ResetHealth();
				((xCoreHungerManager)player.HungerManager).SetProcess();
				((xCoreHungerManager)player.HungerManager).Regen = true;
				player.IsWorldImmutable = true;
				player.NoAi = false;
				player.RemoveAllEffects();
				Action action = new Action(() =>
				{
					/*byte x = 0;
					foreach (var d in Auth.DynManager.Inv)
					{
						if (d.Value.Start)
							player.Inventory.Slots[x++] = d.Value.getItem(player);
					}
					Item item = ItemFactory.GetItem(131);
					player.Inventory.Slots[x++] = item;*/
				});
				Task.Run(() =>
				{
					((xPlayer)player).SpawnLevelAction(Context.Server.LevelManager.GetLevel(null, "overworld"), true, action);
					BossBar.SendBossEventBar(player, 0);
					BossBar.SendName(player, "§l§eДобро пожаловать на §3Cristalix §aPocket Edition");
					BossBar.SendBossEventBar(player, 0);
				});
			}
		}

		public void SetSpectator(Player player)
		{
			player.NameTag = player.Username;
			player.DespawnFromPlayers(player.Level.GetSpawnedPlayers());
			//player.IsInvisible = true;
			player.AllowFly = true;
			player.IsSpectator = true;
			//player.SetGameMode(GameMode.Spectator);
			player.GameMode = GameMode.Spectator;
			((xCoreHungerManager)player.HungerManager).SetProcess();
			//lock (((xPlayer)player).DynamicInvSync)
			//	player.DynInventory = null;
			for (int i = 0; i < player.Inventory.Slots.Count; ++i)
				if (player.Inventory.Slots[i] == null || player.Inventory.Slots[i].Id != 0) player.Inventory.Slots[i] = new ItemAir();
			if (player.Inventory.Helmet.Id != 0) player.Inventory.Helmet = new ItemAir();
			if (player.Inventory.Chest.Id != 0) player.Inventory.Chest = new ItemAir();
			if (player.Inventory.Leggings.Id != 0) player.Inventory.Leggings = new ItemAir();
			if (player.Inventory.Boots.Id != 0) player.Inventory.Boots = new ItemAir();
			//Item item = ItemFactory.GetItem(378);
			//item.ExtraData = new NbtCompound { new NbtCompound("display") { new NbtString ("Name", player.PlayerData.lang.get("xcore.returntohub")), new NbtInt ("Action", 4)}};
			/*Item item2 = ItemFactory.GetItem(324);
			item2.ExtraData = new NbtCompound {
					new NbtCompound("display") {
					new NbtString ("Name", player.PlayerData.lang.get("xcore.newgame")),
					new NbtInt ("Action", 4)
					}
			};
			player.Inventory.Slots[2] = item2;*/
			//player.Inventory.Slots[4] = item;
			player.BroadcastSetEntityData();
			player.SendAdventureSettings();
			player.SendPlayerInventory();
		}

		public static int UnixTime()
		{
			int unixtime = Convert.ToInt32((DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalSeconds);
			return unixtime;
		}
	}

	public class GameData
	{
		//Ид игрока с базы данных
		public int Id { get; set; }
		//Класс игрока
		public Player player { get; set; }
		//Игровые данные(отдельного мини режима)
		public string Class { get; set; } = "default";

		public int Perk { get; set; } = 0;
		public int PerkLvl { get; set; } = 0;
		//Данные даты для разных режимов
		public DateTime Date { get; set; }
		//Данные для для разных режимов
		public int Count { get; set; } = 0;
		//Данные даблджампов на TNTRun
		public int DoubleJump { get; set; } = 0;
		//Данные предметов покупки на SurvivalGames
		public List<Item> Items { get; set; }
		//Изменение на редактирование базы
		public bool Changed = false;

		public bool GameBool = false;
		public int GameInt = 0;
		//Данные мини режимов
		public Dictionary<string, int> Data = new Dictionary<string, int>(35);
		public List<string> DataUpdate = new List<string>(25);

		public string Team { get; set; }

		public string Text { get; set; }

		public GameData(int id, Player player, Dictionary<string, int> data)
		{
			Id = id;
			this.player = player;
			Data = data;
			Date = DateTime.UtcNow;
		}
	}
}

