using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using AuthME;
using log4net;
using MiNET;
using MiNET.Entities;
using MiNET.Net;
using MiNET.Utils;
using MiNET.Worlds;
using MiNET.Items;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using System.Threading;
namespace xCore
{
	public enum Status
	{
		Start = 0,
		PreGame = 1,
		Game = 2,
		Finish = 3,
		Reload = 4,
		Match = 5
	}

	public class xCoreLevel : Level
	{
		private static readonly ILog Log = LogManager.GetLogger(typeof(xCoreLevel));

		public int Id { get; private set; }

		public Status Status { get; set; }

		public bool Started { get; set; } = false;

		public bool PreStarted { get; set; } = false;

		public int Slots { get; private set; }

		public int Time { get; set; }

		public MapInfo Map { get; set; }

		public int Timer { get; set; } = 0;

		public xCoreInterface Game { get; set; }

		public bool pvp { get; set; } = false;

		public xCoreObject Other { get; set; } = null;

		public object _playerQueueLock = new object();

		public ConcurrentDictionary<Player, Player> TempPlayers = new ConcurrentDictionary<Player, Player>();

		public Dictionary<string, int> Points = new Dictionary<string, int>();

		public bool Lock { get; set; } = false;

		public xCoreLevel(IWorldProvider worldProvider, EntityManager entitymanager, xCoreInterface game, MapInfo map, int id) : base(null, game.NameGame + id, worldProvider, entitymanager, GameMode.Survival, (MiNET.Worlds.Difficulty)2, 7)
		{
			Game = game;
			Id = id;
			Status = Status.Start;
			Time = xCoreGames.StartTime;
			Map = map;
			Slots = map.Slots;
			CurrentWorldTime = 6000;
			IsWorldTimeStarted = false;
			BlockBreak += game.BlockBreak;
			BlockPlace += game.BlockPlace;
			WorldTick += GameTimer;
		}

		private object _playerLock = new object();

		public override void AddPlayer(Player newPlayer, bool spawn)
		{
			base.AddPlayer(newPlayer, spawn);
			//lock (((xPlayer)newPlayer).DynamicInvSync)
			//	newPlayer.DynInventory = null;
			Player removed;
			TempPlayers.TryRemove(newPlayer, out removed);
			Game.PlayerCount++;
			newPlayer.ClearPopups();
			Task.Run(delegate
			{
				//lock(((xPlayer)newPlayer)._playerLock)
				//{
				try
				{
					
				}
				catch (Exception exeption)
				{
					Log.Error("Playerexeption add: " + exeption.Message);
					Log.Info("Playerexeption add: " + exeption.Message);
				}
				//}
			});
			foreach (Player pl in GetSpawnedPlayers())
				BroadcastTip(pl, newPlayer.Username + " §3joined §e(" + (Players.Count()) + "/" + Slots + ")");
			Game.Core.BossBar.SendName(newPlayer, Game.Prefix);
			Game.Core.BossBar.SendBossEventBar(newPlayer, 0);
			Game.onGameJoin(newPlayer, this);
		}

		public override void RemovePlayer(Player player, bool despawn = false)
		{
			lock (((xPlayer)player).isFindingGameSync)
				((xPlayer)player).isFindingGame = false;
			player.ClearPopups();
			Game.onGameLeave(player, this);
			base.RemovePlayer(player, despawn);
			Task.Run(delegate
			{
				try
				{
					
				}
				catch (Exception exeption)
				{
					Log.Error("Playerexeption remove: " + exeption.Message);
					Log.Info("Playerexeption remove: " + exeption.Message);
				}
			});
			//Game.Core.BossBar.SendBossEventBar(this, 0);
			//Game.Core.BossBar.SendName(this, "§l§eДобро пожаловать на §3Cristalix §aPocket Edition");
			//Game.Core.BossBar.SendBossEventBar(this, 0);
			if (Status == Status.Start)
				foreach (Player pl in GetSpawnedPlayers())
					BroadcastTip(pl, player.Username + " §3has left §e(" + (PlayerCount - 1) + "/" + Slots + ")");
			if (Status == Status.Game)
				BroadcastMessage(Game.Prefix + player.Username + " has left! (" + (PlayerCount - 1) + "/" + Slots + ")", MessageType.Raw, null);
			Game.PlayerCount--;
		}

		public int GetPlayerCount()
		{
			return Players.Count;
		}

		public Player[] GetSurvivalPlayers()
		{
			if (Players == null) return new Player[0]; // HACK

			return Players.Values.Where(player => player.IsSpawned && player.GameMode == GameMode.Survival).ToArray();
		}

		public void ResetMap()
		{
			//if (_worldProvider == null) throw new Exception($"Can not get level from the WorldProvider");
			AnvilWorldProvider worldProvider = WorldProvider as AnvilWorldProvider;
			//worldProvider._chunkCache.Clear();
			//worldProvider = Map.ProviderCache.Clone() as AnvilWorldProvider;
			ConcurrentDictionary<ChunkCoordinates, ChunkColumn> chunkCache = new ConcurrentDictionary<ChunkCoordinates, ChunkColumn>();
			foreach (KeyValuePair<ChunkCoordinates, ChunkColumn> valuePair in Map.ProviderCache._chunkCache)
			{
				chunkCache.TryAdd(valuePair.Key, (ChunkColumn)valuePair.Value?.Clone());
			}
			worldProvider._chunkCache.Clear();
			worldProvider._chunkCache = chunkCache;
			SkyLightCalculations.Calculate(this);
			while (worldProvider.LightSources.Count > 0)
			{
				var block = worldProvider.LightSources.Dequeue();
				block = this.GetBlock(block.Coordinates);
				BlockLightCalculations.Calculate(this, block);
			}
			//WorldProvider. = worldProvider;
		}

		public void ResetData()
		{
			Time = xCoreGames.StartTime;
			pvp = false;
			Status = Status.Start;
			Started = false;
			PreStarted = false;
		}


		public string TimeString { get; set; }

		public void BroadcastTip(Player player, string line)
		{
			player.ClearPopups();
			player.AddPopup(new Popup
			{
				MessageType = MessageType.Tip,
				Message = line,
				Duration = 60
			});
		}

		public void BroadcastPopup(Player player, string line)
		{
			player.ClearPopups();
			player.AddPopup(new Popup
			{
				MessageType = MessageType.Popup,
				Message = line,
				Duration = 30
			});
		}

		public void BroadcastPopup(Player[] players, string line)
		{
			foreach (Player player in players)
			{
				player.ClearPopups();
				player.AddPopup(new Popup
				{
					MessageType = MessageType.Popup,
					Message = line,
					Duration = 30
				});
			}
		}

		public string OldNameTag { get; set; } = "1";
		public string NameTag { get; set; } = "2";
		public bool SendNameTag { get; set; } = false;
		public bool UpdateTag { get; set; } = false;

		public int MaxInt { get; set; } = 20;
		public int Int { get; set; } = 20;

		public void SetNameTag(string s)
		{
			OldNameTag = NameTag;
			if (OldNameTag != s)
			{
				NameTag = s;
				UpdateTag = true;
			}
			else {
				UpdateTag = false;
			}
		}

		protected override void BroadCastMovement(Player[] players, Entity[] entities)
		{
			base.BroadCastMovement(players, entities);
			if (Timer++ != 20)
				return;
			Timer = 0;
			if (players.Length == 0) return;
			var s = TimeSpan.FromSeconds(Time);
			if (s.Seconds < 10)
			{
				TimeString = "§a" + s.Minutes + ":0" + s.Seconds;
			}
			else {
				TimeString = "§a" + s.Minutes + ":" + s.Seconds;
			}
			//Game.Timer(players, this);
			if (!PreStarted)
			{
				if (Status == Status.Start)
					if (Time <= 10)
						PreStarted = true;
			}
			OnWorldTick(new onTimerEventArgs(players, this));
			if (UpdateTag)
			{
				if (SendNameTag) Game.Core.BossBar.SendName(this, NameTag);
				Game.Core.BossBar.SendBossEventBar(this, 0);
			}
		}

		public void GameTimer(object sender, onTimerEventArgs e)
		{
			Game.Timer(e.Players, e.Level);
		}

		public event EventHandler<onTimerEventArgs> WorldTick;

		protected virtual void OnWorldTick(onTimerEventArgs e)
		{
			WorldTick?.Invoke(this, e);
		}

		public class onTimerEventArgs : EventArgs
		{
			public Player[] Players { get; set; }
			public xCoreLevel Level { get; set; }

			public bool Cancel { get; set; }

			public onTimerEventArgs(Player[] players, xCoreLevel level)
			{
				Players = players;
				Level = level;
			}
		}
	}
}

