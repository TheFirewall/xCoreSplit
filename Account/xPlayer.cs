using System;
using MiNET.Utils;
using MiNET;
using System.Net;
using MiNET.Net;
using System.Threading.Tasks;
using MiNET.Worlds;
using MiNET.Entities;
using MiNET.Items;
using MiNET.Blocks;
using fNbt;
using AuthME;
using System.Collections.Generic;
using log4net;
using System.Numerics;
namespace xCore
{
	public class xPlayerFactory : PlayerFactory
	{

		public xCoreGames xCore { get; set; }

		public override Player CreatePlayer(MiNetServer server, IPEndPoint endPoint, PlayerInfo pd)
		{
			var player = new xPlayer(server, endPoint, xCore);
			player.HealthManager = new xCoreHealthManager(player, xCore);
			player.HungerManager = new xCoreHungerManager(player);
			player.MaxViewDistance = 7;
			player.UseCreativeInventory = false;
			OnPlayerCreated(new PlayerEventArgs(player));
			return player;
		}
	}

	public class xPlayer : Player
	{
		private static ILog Log = LogManager.GetLogger(typeof(xPlayer));

		public xCoreGames xCore { get; set; }

		public Inventory OpenMenu { get; set; }

		public object _playerLock = new object();

		public object isFindingGameSync = new object();

		public bool isFindingGame = false;

		public object DynamicInvSync = new object();

		public PlayerData PlayerData { get; set; }

		public PlayerData GameData { get; set; }

		public xPlayer(MiNetServer server, IPEndPoint endpoint, xCoreGames core) : base(server, endpoint)
		{
			xCore = core;
			//IsWorldImmutable = true;
		}

		private bool _haveJoined = false;

		public override void InitializePlayer()
		{
			base.InitializePlayer();
			if (!_haveJoined)
			{
				_haveJoined = true;
				//Task.Run(delegate
				//{
				//char[] charsToTrim = { '§' };
				if (Username.Contains("§")){
					//Username = Username.Trim(charsToTrim);
					//NameTag = Username;
					//BroadcastSetEntityData();
					Disconnect("Цветные ники запрещенны!");
				}
				if (Username.Contains(" "))
				{
					//Username = Username.Trim(charsToTrim);
					//NameTag = Username;
					//BroadcastSetEntityData();
					Disconnect("Пробелы в никах запрещенны!");
				}
				try
				{
					if (!xCore.Auth.onCheckBan(this)) return;
					Dictionary<string, string> userdata = xCore.Auth.Query("SELECT id, ip, clientid, pass, perm, stuff, prefix, muted, mute_time, lang, booster, online, tester, exp, lvl, coin, cristalix, arrow, epic, legendary, mythical, day FROM userdata WHERE user_low='" + Username.ToLower() + "'");
					if (userdata == null)
					{
						PlayerData = new PlayerData(xCore.Auth.Lang.getLang("rus"));
					}
					else {
						if (bool.Parse(userdata["online"]))
						{
							Disconnect("Вы уже авторизованы!");
							return;
						}
					}
				}
				catch (Exception exeption)
				{
					Log.Error("Playerexeption init: " + exeption);
					Log.Info("Playerexeption init: " + exeption.Message);
				}
				//});
			}
		}

		public override void Disconnect(string reason, bool sendDisconnect = true)
		{
			Task.Run(delegate
			{
				try
				{
						
				}
				catch (Exception exeption)
				{
					Log.Error("Playerexeption disconnect: " + exeption.Message);
					Log.Info("Playerexeption disconnect: " + exeption.Message);
				}
			});

			base.Disconnect(reason, sendDisconnect);
		}

		public override void HandleMcpeMovePlayer(McpeMovePlayer message)
		{
			base.HandleMcpeMovePlayer(message);
			if (Level is xCoreLevel)
			{
				if (((xCoreLevel)Level).Game.isMove)
				{
					((xCoreLevel)Level).Game.MovePlayer(message, this);
				}
				return;
			}
		}

		protected override bool AcceptPlayerMove(McpeMovePlayer message, bool isOnGround, bool isFlyingHorizontally)
		{
			if (CooldownTick-- > 0) return true;
			if (!isOnGround)
			{
				if (isFlyingHorizontally)
				{
					//if (!message.onGround)
					//{
						if (!IsSpectator)
						{
							if (message.y <= 256)
							{
								Disconnect("Error #375! Flight is not allow.");
								//Level.BroadcastMessage(Username + " возможно читер!!!");
								return true;
							}
						}
					//}
				}
			}
			return true;
		}

		public override void HandleMcpeRequestChunkRadius(McpeRequestChunkRadius message)
		{
			base.HandleMcpeRequestChunkRadius(message);
			MoveRenderDistance = ChunkRadius - 2;
			MaxViewDistance = ChunkRadius;
		}

		public int CooldownTick { get; set; }

		public override void Teleport(PlayerLocation newPosition)
		{
			CooldownTick = 20;
			base.Teleport(newPosition);
		}

		public override void SpawnLevel(Level toLevel, PlayerLocation spawnPoint, bool useLoadingScreen = false, Func<Level> levelFunc = null, Action postSpawnAction = null)
		{
			CooldownTick = 20;
			//lock(DynamicInvSync)
			//	DynInventory = null;
			for (int i = 0; i < Inventory.Slots.Count; ++i)
			{
				if (Inventory.Slots[i] == null || Inventory.Slots[i].Id != 0) Inventory.Slots[i] = new ItemAir();
			}

			if (Inventory.Helmet.Id != 0) Inventory.Helmet = new ItemAir();
			if (Inventory.Chest.Id != 0) Inventory.Chest = new ItemAir();
			if (Inventory.Leggings.Id != 0) Inventory.Leggings = new ItemAir();
			if (Inventory.Boots.Id != 0) Inventory.Boots = new ItemAir();
			AllowFly = false;
			IsSpectator = false;
			base.SpawnLevel(toLevel, spawnPoint, useLoadingScreen, levelFunc, null);
		}

		public void InHub(Level toLevel)
		{
			CooldownTick = 20;
			//DynInventory = null;

			Level.RemovePlayer(this, true);
			//Level.EntityManager.RemoveEntity(null, this);

			Level = toLevel; // Change level

			SetPosition(SpawnPosition);

			Level.AddPlayer(this, true);
		}

		public void SpawnLevelAction(Level toLevel, bool useLoadingScreen, Action action)
		{
			CooldownTick = 20;
			//lock (DynamicInvSync)
			//	DynInventory = null;
			for (int i = 0; i < Inventory.Slots.Count; ++i)
			{
				if (Inventory.Slots[i] == null || Inventory.Slots[i].Id != 0) Inventory.Slots[i] = new ItemAir();
			}

			if (Inventory.Helmet.Id != 0) Inventory.Helmet = new ItemAir();
			if (Inventory.Chest.Id != 0) Inventory.Chest = new ItemAir();
			if (Inventory.Leggings.Id != 0) Inventory.Leggings = new ItemAir();
			if (Inventory.Boots.Id != 0) Inventory.Boots = new ItemAir();
			action();
			base.SpawnLevel(toLevel, null, useLoadingScreen);
		}

		public override void HandleMcpeInteract(McpeInteract message)
		{
			if (message.actionId != 2) return;
			if (Level is xCoreLevel && GameMode == GameMode.Survival){
				if (((xCoreLevel)Level).Game.Interact(message, this)) base.HandleMcpeInteract(message);
				return;
			}
			if (!PlayerData.isAuth) return;
			Entity target = Level.GetEntity(message.targetRuntimeEntityId);
			if (target == null) return;
			if (!(target is xCoreNPC)) return;
			((xCoreNPC)target).Tap(this);
		}

		public override void HandleMcpeDropItem(McpeDropItem message)
		{
			if (Level is xCoreLevel)
			{
				if (((xCoreLevel)Level).Game.DropItem(message, this))
				{
					base.HandleMcpeDropItem(message);
					return;
				}
			}
			else {
				SendPlayerInventory();
			}
			return;
		}

		public override void HandleMcpeContainerClose(McpeContainerClose message)
		{
			OpenMenu = null;
			base.HandleMcpeContainerClose(message);
		}

		public override void HandleMcpeContainerSetSlot(McpeContainerSetSlot message)
		{
			if (message.item.Id == Inventory.Slots[message.slot].Id)
			{
				if (message.item.Count == Inventory.Slots[message.slot].Count)
					return;
			}
			if (Level is xCoreLevel)
			{
				if (!(((xCoreLevel)Level).Status == Status.Start))
				{
						base.HandleMcpeContainerSetSlot(message);
				}
				else {
					if (message.hotbarslot == 0)
					{
						McpeContainerSetContent strangeContent = McpeContainerSetContent.CreateObject();
						strangeContent.windowId = (byte)0x7b;
						strangeContent.entityIdSelf = EntityId;
						strangeContent.slotData = new ItemStacks();
						strangeContent.hotbarData = new MetadataInts();
						SendPackage(strangeContent);

						McpeContainerSetContent inventoryContent = McpeContainerSetContent.CreateObject();
						inventoryContent.windowId = (byte)0x00;
						inventoryContent.entityIdSelf = EntityId;
						inventoryContent.slotData = Inventory.GetSlots();
						inventoryContent.hotbarData = Inventory.GetHotbar();
						SendPackage(inventoryContent);

						McpeMobEquipment order = McpeMobEquipment.CreateObject();
						order.runtimeEntityId = EntityManager.EntityIdSelf;
						order.item = message.item;
						order.selectedSlot = (byte)message.selectedSlot; // Selected hotbar slot
						order.slot = (byte)message.slot;
						SendPackage(order);
						//SendPlayerInventory();
					}
					return;
				}
			}
			else {
				if (message.hotbarslot == 0)
				{
					McpeContainerSetContent strangeContent = McpeContainerSetContent.CreateObject();
					strangeContent.windowId = (byte)0x7b;
					strangeContent.entityIdSelf = EntityId;
					strangeContent.slotData = new ItemStacks();
					strangeContent.hotbarData = new MetadataInts();
					SendPackage(strangeContent);

					McpeContainerSetContent inventoryContent = McpeContainerSetContent.CreateObject();
					inventoryContent.windowId = (byte)0x00;
					inventoryContent.entityIdSelf = EntityId;
					inventoryContent.slotData = Inventory.GetSlots();
					inventoryContent.hotbarData = Inventory.GetHotbar();
					SendPackage(inventoryContent);
				}
				return;
			}
			base.HandleMcpeContainerSetSlot(message);
		}

		public override void HandleMcpeMobEquipment(McpeMobEquipment message)
		{
			if (Level is xCoreLevelLobby)
			{
				Item itemStack = message.item;
				if (GameMode != GameMode.Creative && itemStack != null && !VerifyItemStack(itemStack))
				{
					Log.Error($"Kicked {Username} for equipment hacking.");
					Disconnect("Error #376. Please report this error.");
				}

				byte selectedHotbarSlot = message.selectedSlot;
				int selectedInventorySlot = (byte)(message.slot - PlayerInventory.HotbarSize);

				if (Log.IsDebugEnabled) Log.Debug($"Player {Username} called set equiptment with inv slot: {selectedInventorySlot}({message.slot}) and hotbar slot {message.selectedSlot} with item {message.item}");

				// 255 indicates empty hmmm
				if (selectedInventorySlot < 0 || (message.slot != 255 && selectedInventorySlot >= Inventory.Slots.Count))
				{
					if (GameMode != GameMode.Creative)
					{
						Log.Error($"Player {Username} set equiptment fails with inv slot: {selectedInventorySlot}({message.slot}) and hotbar slot {selectedHotbarSlot} for inventory size: {Inventory.Slots.Count} and Item ID: {message.item?.Id}");
					}
					return;
				}

				if (message.slot == 255)
				{
					//Inventory.ItemHotbar[selectedHotbarSlot] = -1;
					//return;
					selectedInventorySlot = -1;
				}
				else
				{
					for (int i = 0; i < Inventory.ItemHotbar.Length; i++)
					{
						if (Inventory.ItemHotbar[i] == selectedInventorySlot)
						{
							Inventory.ItemHotbar[i] = Inventory.ItemHotbar[selectedHotbarSlot];
							break;
						}
					}
				}

				Inventory.ItemHotbar[selectedHotbarSlot] = selectedInventorySlot;
				Inventory.InHandSlot = selectedHotbarSlot;
				//Inventory.SetHeldItemSlot(selectedHotbarSlot, false);

			}
			if (message.item.Id == Inventory.Slots[message.slot].Id)
			{
				if (message.item.Count == Inventory.Slots[message.slot].Count)
					return;
			}
			base.HandleMcpeMobEquipment(message);
		}

		public void SetSlot(Item i, int slot)
		{
			McpeContainerSetSlot sendSlot = McpeContainerSetSlot.CreateObject();
			sendSlot.windowId = 0;
			sendSlot.slot = slot;
			sendSlot.item = i;
			SendPackage(sendSlot);
			McpeMobEquipment order = McpeMobEquipment.CreateObject();
			order.runtimeEntityId = EntityManager.EntityIdSelf;
			order.item = i;
			order.selectedSlot = (byte)slot; // Selected hotbar slot
			order.slot = (byte)slot;
			SendPackage(order);
		}

		public void SetBlock(Item i, BlockFace blockface, BlockCoordinates coord, uint id)
		{
			if (id != 31)
			{
				Block b = Level.GetBlock(i.GetNewCoordinatesFromFace(coord, blockface));
				var message = McpeUpdateBlock.CreateObject();
				message.blockId = 0;
				message.coordinates = b.Coordinates;
				message.blockMetaAndPriority = (byte)(0xb << 4 | (b.Metadata & 0xf));
				SendPackage(message);
			}
			else {
				var message = McpeUpdateBlock.CreateObject();
				message.blockId = 31;
				message.coordinates = coord;
				message.blockMetaAndPriority = (byte)(0xb << 4 | (1 & 0xf));
				SendPackage(message);
			}
		}

		private TimeSpan s500 { get; set; } = TimeSpan.FromMilliseconds(500);
		private TimeSpan s700 { get; set; } = TimeSpan.FromMilliseconds(700);

		public override void HandleMcpeUseItem(McpeUseItem message)
		{
			if (!PlayerData.isAuth) return;
			//if (DynInventory == null)
			{
				Item i = Inventory.GetItemInHand();
				if (i.Id != 0)
				{
					if (!Level.AllowBuild)
					{
						if (message.item is ItemBlock)
							SetBlock(message.item, (BlockFace)message.face, message.blockcoordinates, message.blockId);
					}
					NbtCompound lines;
					if (!i.ExtraData.TryGet("display", out lines))
					{
						base.HandleMcpeUseItem(message);
						return;
					}
					DateTime now = DateTime.UtcNow;
					if (Level is xCoreLevelLobby && (now - PlayerData.ActionTime) < s500)
					{
						if (i is ItemBlock)
						{
							//SetBlock(i, (BlockFace)message.face,message.blockcoordinates);
							SetSlot(i, message.slot);
						}
						return;
					}

					if ((now - PlayerData.ActionTime) < s500)
					{
						base.HandleMcpeUseItem(message);
						return;
					}
					NbtInt action;
					PlayerData.ActionTime = now;
					//new Task(() =>t
				}
			}
			base.HandleMcpeUseItem(message);
		}

		public override void HandleMcpeEntityFall(McpeEntityFall message)
		{
			if (Level is xCoreLevel)
			{
				xCoreLevel level = (xCoreLevel)Level;
				if (level.Status == Status.Game)
				{
					if (level.Game.FallPlayer(message, this))
					{
						base.HandleMcpeEntityFall(message);
					}
				}
			}
		}

		public override void HandleMcpePlayerAction(McpePlayerAction message)
		{
			switch (message.actionId)
			{
				case (int)PlayerAction.AbortBreak:
					if (Level is xCoreLevel)
					{
						if (((xCoreLevel)Level).Game.Action(message, this))
						{
							base.HandleMcpePlayerAction(message);
							return;
						}
					}
					break;
				case (int)PlayerAction.Jump:
					CooldownTick = 20;
					break;
			}
			base.HandleMcpePlayerAction(message);
		}

		public override void HandleMcpeCommandStep(McpeCommandStep message)
		{
			if (!PlayerData.isAuth)
			{
				if (message.commandName.StartsWith("log") || message.commandName.StartsWith("reg"))
				{
					base.HandleMcpeCommandStep(message);
				}
				return;
			}
			base.HandleMcpeCommandStep(message);
		}

		public override void HandleMcpeText(McpeText message)
		{
			if (!PlayerData.isAuth) return;
			DateTime now = DateTime.UtcNow;
			if ((now - PlayerData.ActionTime) < s500) return;
			PlayerData.ActionTime = DateTime.UtcNow;
			if (PlayerData.muted)
			{
				if ((long)PlayerData.mute_time > Database.UnixTime())
				{
					long mute = (long)PlayerData.mute_time - Database.UnixTime();
					SendMessage(string.Format(PlayerData.lang.get("bm.mute.blocking"), mute));
					return;
				}
				else {
					SendMessage(PlayerData.lang.get("bm.mute.unlocking"));
					PlayerData.muted = false;
					PlayerData.mute_time = 0;
				}
			}
			string chatFormat = $"[{ChatColors.Yellow}" + PlayerData.DataValue["lvl"] + $"{ChatColors.White}]{ChatColors.Gray}[" + PlayerData.prefix + $"{ChatColors.Gray}]{ChatColors.White}" + Username + " : §r" + message.message;
			Level.BroadcastMessage(chatFormat);
			return;
		}
	}
}