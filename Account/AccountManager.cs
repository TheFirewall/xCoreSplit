using System;
using System.Collections.Generic;
using log4net;
using MiNET;
using MiNET.Plugins;
using MiNET.Plugins.Attributes;
using xCore;
namespace AuthME
{
	public class AccountManager
	{
		static ILog Log = LogManager.GetLogger(typeof(AccountManager));

		public Account Auth { get; private set; }

		public AccountManager(Account auth){
			Auth = auth;
		}
	}

	public class PlayerData
	{
		//Ид игрока с базы данных
		public int Id { get; set; } = 0;
		//Пароль игрока
		public string password { get; set; } = null;
		//Префикс игрока
		public string prefix { get; set; } = "Player";
		//Авторизован ли игрок. False - нет
		public bool isAuth { get; set; } = false;
		//Открыт ли древний сундук
		public bool openChest { get; set; } = false;
		public int Attempts { get; set; } = 0;
		public int Chest { get; set; } = 0;
		//Класс языка игрока
		public Lang lang { get; set; }
		//Привелегия игрока
		public int perm { get; set; } = 0;
		//Персонал
		public int stuff { get; set; } = 0;
		//Замучен ли игрок
		public bool muted { get; set; } = false;
		//Время мута
		public long mute_time { get; set; } = 0;
		//Питомец
		//public ConcurrentDictionary<string, int> Pets = new ConcurrentDictionary<string, int>();
		//Бустер привелегии
		public float booster { get; set; } = 1;
		//Время действий
		public DateTime ActionTime { get; set; } = DateTime.UtcNow;
		//Время гаджета
		public DateTime GadgetTime { get; set; } = DateTime.UtcNow;
		//Изменять ли базу данных
		public bool Changed = false;
		//Данные игрока(Монеты,Кристалики и т.д)
		public Dictionary<string, int> DataValue = new Dictionary<string, int>(7);
		//Слушает ли игрок
		public bool Radio { get; set; } = true;

		public Dictionary<string, int> SubData = new Dictionary<string, int>();

		public List<ActionLog> ActionLogs = new List<ActionLog>();
		//Данные стрел
		public int Arrow { get; set; } = 0;

		public bool Yes { get; set; } = false;

		public int Rare { get; set; }

		public PlayerData(Lang l)
		{
			lang = l;
			DataValue.Add("exp", 0);
			DataValue.Add("coin", 0);
			DataValue.Add("lvl", 0);
			//DataValue.Add("cristalix", 0);
			DataValue.Add("epic", 0);
			DataValue.Add("legendary", 0);
			DataValue.Add("mythical", 0);
		}

		public PlayerData(Dictionary<string, string> userdata, Lang lang)
		{
			prefix = userdata["prefix"];
			this.lang = lang;
			Id = int.Parse(userdata["id"]);
			perm = int.Parse(userdata["perm"]);
			mute_time = int.Parse(userdata["mute_time"]);
			muted = bool.Parse(userdata["muted"]);
			stuff = int.Parse(userdata["stuff"]);
			booster = float.Parse(userdata["booster"]);
			password = userdata["pass"];
			Arrow = int.Parse(userdata["arrow"]);
			DataValue.Add("exp", int.Parse(userdata["exp"]));
			DataValue.Add("coin", int.Parse(userdata["coin"]));
			DataValue.Add("lvl", int.Parse(userdata["lvl"]));
			//DataValue.Add("cristalix", int.Parse(userdata["cristalix"]));
			//Cristalix = int.Parse(userdata["cristalix"]);
			DataValue.Add("epic", int.Parse(userdata["epic"]));
			DataValue.Add("legendary", int.Parse(userdata["legendary"]));
			DataValue.Add("mythical", int.Parse(userdata["mythical"]));
		}

		public enum Permission
		{
			GUEST = 0,
			IRON = 1,
			VIP = 2,
			VIPPlus = 3,
			Gold = 4,
			Premium = 5,
			PremiumP = 6,
			Diamond = 7,
			MVP = 8,
			MVPP = 9,
			Emerald = 10,
			Sponsor = 11
		}

		public enum Stuff
		{
			GUEST = 0,
			Builder = 1,
			YouTube = 2,
			Helper = 3,
			Helperp = 4,
			Moder = 5,
			Curator = 6,
			Admin = 7,
			Dev = 8,
			Owner = 9
		}
	}

	/* type 0 = add coin and take cristalix
	 * type 1 = take cristalix
	 * type 2 = take coin
	 * type 3
	 * type 4
	*/

	public class ActionLog
	{
		public int userid;

		public string username;

		public int coin;

		public int cristalix;

		public byte type;

		public string info;

		public DateTime date;

		public bool logs;

		public ActionLog(xPlayer p, byte typed, string infod, int cristalixd, int coind = 0, bool log = true)
		{
			userid = p.PlayerData.Id;
			username = p.Username.ToLower();
			coin = coind;
			cristalix = cristalixd;
			info = infod;
			type = typed;
			date = DateTime.UtcNow;
			logs = log;
		}
	}
}