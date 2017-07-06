using System.Collections.Generic;
using System.IO;
using log4net;

namespace AuthME
{
	public class Lang
	{
		private ILog Log = LogManager.GetLogger(typeof(Lang));

		public Dictionary<string, string> lines = new Dictionary<string, string>(260);

		public string Name;

		public Lang (string name, string path)
		{
			Name = name;
			var lineas = File.ReadAllLines(path);
			foreach(string line in lineas){
				string[] str = line.Split('=');
				string In = str[0];
				string Out = str [1];
				lines.Add (In, Out);
			}
		}

		public void AddLines(string path)
		{
			var lineas = File.ReadAllLines(path);
			foreach (string line in lineas)
			{
				if (line.Equals("") || line[0] == '#')
				{
					continue;
				}
				string[] str = line.Split('=');
				string In = str[0];
				string Out = str[1];
				lines.Add(In, Out);
			}
		}

		public string get(string line){
			string value;
			if (lines.TryGetValue (line, out value)) {
				return value;
			} else {
				return line;
			}
		}
	}
}

