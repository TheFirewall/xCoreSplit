using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using MiNET;
using MiNET.Entities;
using MiNET.BlockEntities;
using MiNET.Utils;
using MiNET.Worlds;
using System.Numerics;
using Newtonsoft.Json.Linq;
using MiNET.Blocks;
namespace xCore
{
	public class MapInfo
	{

		public string Name { get; set; }
		public List<BlockCoordinates> CacheBlockEntites = null;

		//public Dictionary<string, List<BlockCoordinates>> CacheBlockEntites = null;

		public List<BlockCoordinates> CacheCoordinates = new List<BlockCoordinates>();

		public AnvilWorldProvider ProviderCache { get; set; }

		public bool ProviderBool { get; set; } = false;

		public BlockCoordinates lobby { get; set; }

		public BlockCoordinates Center { get; set; }

		public int Slots { get; set; }

		public List<List<Block>> Floors { get; set; }

		public MapInfo(string name, JObject conf)
		{
			Name = name;
			//int.Parse((string)game.conf["Maps"][map]["CenterZ"]);
			lobby = new BlockCoordinates(int.Parse((string)conf["lobbyx"]), int.Parse((string)conf["lobbyy"]), int.Parse((string)conf["lobbyz"]));
			Center = new BlockCoordinates(int.Parse((string)conf["centerx"]), 0, int.Parse((string)conf["centerz"]));
			Slots = int.Parse((string)conf["slots"]);
		}
	}
}

