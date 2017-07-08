using System;
using MiNET;
using MiNET.Net;
using MiNET.Worlds;
using Newtonsoft.Json.Linq;
using System.Collections.Concurrent;
using System.Collections;
using MiNET.Plugins;
namespace xCore
{
	public interface xCoreInterface
	{
		bool Testing { get; set; }

		int ArenaLoaded { get; set; }

		string NameGame { get; set; }

		string Prefix { get; set; }

		string PrefDB { get; set; }

		int MinPlayers { get; set; }

		xCoreGames Core { get; set; }

		PluginContext Context { get; set; }

		LevelPoolGame Pool { get; set; }

		JObject conf { get; set; }

		int PlayerCount { get; set; }

		void Initialization(xCoreLevel level);

		void BlockPlace(object sender, BlockPlaceEventArgs e);

		void BlockBreak(object sender, BlockBreakEventArgs e);

		void Timer(Player[] players, xCoreLevel level);

		void onGameJoin(Player player, xCoreLevel level);

		void onGameLeave(Player player, xCoreLevel level);

		void DeathPlayer(Player player);

		bool Interact(McpeInteract package, Player player);

		bool isMove { get; set; }

		bool MovePlayer(McpeMovePlayer package, Player player);

		bool FallPlayer(McpeEntityFall package, Player player);

		bool Action(McpePlayerAction package, Player player);

		bool UseItem(McpeUseItem package, Player player, fNbt.NbtCompound comp);

		bool DropItem(McpeDropItem package, Player player);
	}
}

