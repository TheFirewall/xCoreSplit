using System.Collections.Generic;

namespace AuthME
{
	public class LangManager
	{
		private Dictionary<string, Lang> Languages = new Dictionary<string, Lang>();

		public static string DefaultLang = "eng";

		public Account Account;

		public LangManager(Account acc)
		{
			Account = acc;
		}

		public void addLang(string name, Lang lang){
			Languages.Add(name, lang);
		}

		public Lang getLang(string name){
			Lang value;
			if (Languages.TryGetValue (name, out value)) {
				return value;
			} else {
				return Languages [DefaultLang];
			}
		}

		public Dictionary<string, Lang> getLangs()
		{
			return Languages;
		}
	}
}

