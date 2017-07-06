using System;
using System.Text;
using System.Collections.Generic;
using System.IO;
using log4net;

namespace AuthME
{
	public class RandomMessage
	{
		private ILog Log = LogManager.GetLogger(typeof(Lang));

		public List<string> lines = new List<string>();

		public string Name;

		public RandomMessage(string name, string path)
		{
			Name = name;
			var lineas = File.ReadAllLines(path);
			foreach (string line in lineas)
			{
				lines.Add(line);
			}
		}

		public void AddLines(string path)
		{
			var lineas = File.ReadAllLines(path);
			foreach (string line in lineas)
			{
				lines.Add(line);
			}
		}


		public string get()
		{
			Random r = new Random();
			return lines[r.Next(0, lines.Count - 1)];
		}
	}
}