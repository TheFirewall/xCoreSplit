using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using log4net;
using MiNET;
using MiNET.Plugins;
using MiNET.Plugins.Attributes;
using MiNET.Utils;
using MiNET.Worlds;
using xCore;
using MiNET.Items;
using fNbt;
using System.Net;
using System.Web;
namespace AuthME
{
	//[Plugin(Author = "Overlord", Description = "AuthME and connection MySQL.", PluginName = "AuthME", PluginVersion = "0.0.1")]
	public class Account
	{
		private static ILog Log = LogManager.GetLogger(typeof(Account));

		public PluginContext Context { get; set; }

		public xCoreGames Core { get; set; }

		//public DynamicManager DynManager { get; set; }

		public Database Database { get; set; }

		public LangManager Lang { get; set; }

		public AccountManager AccManager { get; set; }

		public void OnEnable(xCoreGames core, PluginContext context)
		{
			Core = core;
			Context = context;
			var instance = new AccountManager(this);
			Context.PluginManager.LoadCommands(instance);
			Context.PluginManager.LoadCommands(this);
			AccManager = instance;
			Log.Info("[Cristalix] BanManager enabled");

			Database = new Database(this);
			Database.Open();
			Log.Info("[Cristalix] The database is running");

			Lang = new LangManager(this);
			Lang.addLang("eng", new Lang("eng", Config.GetProperty ("PluginDirectory", "Plugins") + "\\lang\\AuthME\\eng.ini"));
			//Lang.addLang("por", new Lang("por", Config.GetProperty ("PluginDirectory", "Plugins") + "\\lang\\AuthME\\por.ini"));
			Lang.addLang("rus", new Lang("rus", Config.GetProperty("PluginDirectory", "Plugins") + "\\lang\\AuthME\\rus.ini"));
			Log.Info("[Cristalix] LangManager enabled");
			//DynManager = new DynamicManager(core);
			Log.Info("[Cristalix] DynamicManager enabled");
			Log.Info("[Cristalix] AuthME enabled");
		}

		public LangManager getLang()
		{
			return Lang;
		}

		public Database getDatabase()
		{
			return Database;
		}

		public void OnDisable()
		{
			Database.CloseAsync();
			Log.Info("AuthME Disable");
		}

		public bool onCheckBan(Player player)
		{
			string sql = string.Format("SELECT admin, reason, time FROM banlist WHERE player='{0}' OR ip='{1}' OR clientid='{2}';", player.Username.ToLower(), player.EndPoint.Address, player.ClientUuid.ToString());
			Dictionary<string, string> query = Query(sql);
			if (query == null)
				return true;
			int Time = int.Parse(query["time"]);
			if (Time != 0)
			{
				if (Time < xCoreGames.UnixTime())
				{
					Database.Update("DELETE FROM banlist WHERE player = '" + player.Username.ToLower() + "'");
					player.SendMessage("Тебя разбанили!!!");
					return true;
				}
				else {
					int time = (Convert.ToInt32(query["time"]) - xCoreGames.UnixTime()) / 86400;
					player.Disconnect("§cВы в бане!\n Забанил: " + query["admin"] + "§c\n До разабана:\n §2" + time + " §cдней\n Причина:§9 " + query["reason"], true);
					return false;
				}
			}
			else {
				player.Disconnect("§cВы в бане!\n Забанил: " + query["admin"] + "§c\n Бан:\n §2навсегда§c!\n Причина:§9 " + query["reason"], true);
				return false;
			}
		}

		[Command(Name = "reg", Aliases = new[] { "register" })]
		public void Reg(xPlayer player, params string[] password)
		{
			string Name = player.Username;
			if(player.PlayerData.isAuth)
				return;
			Dictionary<string, string> userdata = Query("SELECT user_low FROM userdata WHERE user_low='" + player.Username.ToLower() + "'");
			if (userdata == null)
			{
				//Register code
			}
			else
			{
				player.SendMessage(player.PlayerData.lang.get("authme.register.error.registered"));
			}
		}

		[Command(Name = "log", Aliases = new[] { "login", "auth" })]
		public void Auth(xPlayer player, params string[] password)
		{
			if (player.PlayerData.password != null){
				if (player.PlayerData.password == GetPasswordHash (string.Join("", password))) {
					
				} else {
					player.SendMessage(player.PlayerData.lang.get("authme.login.error.password"));
				}
			} else {
				player.SendMessage(player.PlayerData.lang.get("authme.login.error.noaccount"));
            }
        }

        private string GetPasswordHash(string password)
        {
			string md5 = GenerateMD5Hash(password);
			return md5;
        }

		public string GenerateMD5Hash(string rawText)
		{
			MD5CryptoServiceProvider md5Hash = new MD5CryptoServiceProvider();
			byte[] randByte = Encoding.UTF8.GetBytes(rawText);
			byte[] computeHash = md5Hash.ComputeHash(randByte);
			string resultHash = String.Empty;
			foreach (byte currentByte in computeHash)
			{
				resultHash += currentByte.ToString("x2");
			}
			return resultHash;
		}

		public Dictionary<string, string> Query(string sql1)
		{
			DataTable sql = Database.Query(sql1);
			if (sql != null)
			{
				Dictionary<string, string> dic = new Dictionary<string, string>(sql.Columns.Count);
				foreach (DataColumn c in sql.Columns)
				{
					dic.Add(c.ColumnName, "null");
					dic[c.ColumnName] = sql.Rows[0].Field<Object>(c.ColumnName).ToString();
				}
				sql.Dispose();
				return dic.Count > 0 ? dic : null;
			}
			return null;
		}

		public Player getPlayer(string username)
		{
			foreach (var pl in Context.Server.ServerInfo.PlayerSessions)
			{
				if (pl.Key != null && pl.Value != null)
				{
					if (pl.Value.MessageHandler != null)
					{
						if (pl.Value.MessageHandler is Player)
						{
							Player PlayerSession = pl.Value.MessageHandler as Player;
							if (PlayerSession.Username.ToLower() == username.ToLower())
							{
								return PlayerSession;
							}
						}
					}
				}
			}
			return null;
		}
    }
}
