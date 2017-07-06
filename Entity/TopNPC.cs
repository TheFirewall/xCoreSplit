using System;
using System.Collections.Generic;
using log4net;
using MiNET;
using MiNET.Entities;
using MiNET.Items;
using MiNET.Net;
using MiNET.Utils;
using MiNET.Worlds;
namespace xCore
{
	public class xCoreTop : Entity
	{
		private static ILog Log = LogManager.GetLogger(typeof(xCoreNPC));

		public xCoreInterface Game { get; set; }

		public bool HideName { get; set; } = false;

		public MetadataDictionary Meta { get; set; }

		public xCoreTop(EntityManager m, xCoreInterface G, string[] args, string namegame) : base((int)EntityType.Slime, null)
		{
			m.AddEntity(this);
			Game = G;
			//EntityId = 1000000000;

			var timelevel = new System.Diagnostics.Stopwatch();
			timelevel.Start();
			Log.Info(args.ToLower());
			List<Dictionary<string, string>> sel = Game.Core.Auth.MultipleQuery("SELECT userid, "+ args.ToLower() +" FROM " + Game.PrefDB + "_stats ORDER BY wins DESC LIMIT 10", Game.PrefDB + "_users");
			string sql = "SELECT user, id FROM userdata WHERE ";
			int sqlss = 1;
			foreach (var top in sel)
			{
				if (sqlss++ == 1)
				{
					sqlss++;
					sql += "id IN ('" + top["userid"] + "'";
				}
				else {
					sql += ", '" + top["userid"] + "'";
				}
			}
			sql += ") ORDER BY FIELD(id";
			foreach (var top in sel)
			{
				sql += ", '" + top["userid"] + "'";
			}
			sql += ")";
			List<Dictionary<string, string>> nick = Game.Core.Auth.MultipleQuery(sql, "userdata");
			int ind = 1;
			int list = 1;
			List<string> str = new List<string>();
			str.Add("§3Cristalix " + namegame);
			str.Add("§eTop 10 players");
			foreach (var top in sel)
			{
				string n = nick[ind - 1]["user"];
				string h = $"§a{ind}. §7{n} §3has ";
				foreach (string line in args)
				{
					h += $"§e{top[line.ToLower()]} {line.Substring(0,1)}§8,"; 
				}
				if (list <= h.Length)
				{
					list = h.Length;
				}
				str.Add(h);
				ind++;
			}
			//s += "Статистика обновляется раз в час!";
			timelevel.Stop();
			string hh = "§4";
			foreach (string line in args)
			{
				hh += line.Substring(0, 1).ToUpper() + " - " + line + ", ";
			}
			str.Add(hh);
			Log.Info("Тест прошел за " + timelevel.ElapsedTicks + " ticks");
			NameTag = str.CenterName();
			/*long flags = 0;
			//flags |= 1 << 5;
			flags |= 1 << 14;
			flags |= 1 << 15;
			flags |= 1 << 16;
			MetadataDictionary metadata = new MetadataDictionary();
			metadata[(int)Entity.MetadataFlags.EntityFlags] = new MetadataLong(flags);
			//metadata[1] = new MetadataInt(1);
			//metadata[2] = new MetadataInt(0);
			metadata[(int)Entity.MetadataFlags.NameTag] = new MetadataString(NameTag ?? string.Empty);
			metadata[(int)Entity.MetadataFlags.Scale] = new MetadataFloat(0); // Scale
			metadata[(int)Entity.MetadataFlags.CollisionBoxHeight] = new MetadataFloat(0); // Collision box width
			metadata[(int)Entity.MetadataFlags.CollisionBoxWidth] = new MetadataFloat(0); // Collision box height

			Meta = metadata;*/
			Width = 0;
			Length = 0;
			Height = 0;

			HideNameTag = false;
			IsAlwaysShowName = true;

		}

		public virtual void SpawnToPlayer(Player player, PlayerLocation pl, int x, int z)
		{
			{
				var addEntity = McpeAddEntity.CreateObject();
				addEntity.entityType = (byte)EntityType.Slime;
				addEntity.entityIdSelf = EntityId;
				addEntity.runtimeEntityId = EntityId;
				addEntity.x = pl.X + x + 0.5f;
				addEntity.y = pl.Y - 2;
				addEntity.z = pl.Z + z + 0.5f;
				addEntity.yaw = 0;
				addEntity.pitch = 0;
				addEntity.metadata = GetMetadata();
				addEntity.speedX = 0;
				addEntity.speedY = 0;
				addEntity.speedZ = 0;
				addEntity.attributes = GetEntityAttributes();
				player.SendPackage(addEntity);
			}
		}

		public virtual void DespawnFromPlayer(Player player)
		{
			McpeRemoveEntity mcpeRemovePlayer = McpeRemoveEntity.CreateObject();
			mcpeRemovePlayer.entityIdSelf = EntityId;
			player.SendPackage(mcpeRemovePlayer);
		}
	}

	public static class StringArray
	{
		public static string ToLower(this string[] args)
		{
			string line = "";
			int i = 0;
			foreach (string s in args)
			{
				if (i++ == 0)
				{
					line += s.ToLower();
					continue;
				}
				line += ", " + s.ToLower();
			}
			return line;
		}
	}

	public static class ListHelper
	{

		public static string CenterName(this List<string> args)
		{

			int list = 1;
			foreach (string line in args)
			{
				string ss = "";
				ss += line;

				for (int i = 0; i <= 9; i++)
				{
					ss = ss.Replace("§" + i, "");
				}
				string sss = "";
				sss = ss.Replace("§a", "").Replace("§r", "").Replace("§l", "").Replace("§b", "").Replace("§c", "").Replace("§d", "").Replace("§e", "").Replace("§f", "").Replace("\n", "");
				if (list <= sss.Length)
				{
					list = sss.Length;
				}
			}
			string s = "";
			foreach (string line in args)
			{
				string ss = "";
				ss += line;

				for (int i = 0; i <= 9; i++)
				{
					ss = ss.Replace("§" + i, "");
				}
				string sss = "";
				sss = ss.Replace("§a", "").Replace("§r", "").Replace("§l", "").Replace("§b", "").Replace("§c", "").Replace("§d", "").Replace("§e", "").Replace("§f", "").Replace("\n", "");

				if (sss.Length <= list)
				{
					int min = list - sss.Length;
					String prob = new String(' ', min / 2);
					s += prob + line + prob + "\n";
				}
			}
			return s;
		}
	}
}

