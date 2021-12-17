using System;
using System.IO;

namespace Discord_Bot
{
	class Logger
	{
		public static void Setup()
		{
			try
			{
				new FileStream("out.txt", FileMode.Create, FileAccess.Write).Close();
			}
			catch (Exception e)
			{
				Console.WriteLine("Failed to open or create out.txt");
				Console.WriteLine(e.Message);
			}
			try
			{
				new FileStream("err.txt", FileMode.Append, FileAccess.Write).Close();
			}
			catch (Exception e)
			{
				Console.WriteLine("Failed to open or create err.txt");
				Console.WriteLine(e.Message);
			}
		}

		public static void Shutdown()
		{

		}

		public static void Write(string s)
		{
			Console.Write(s);

			var oStream = new FileStream("out.txt", FileMode.OpenOrCreate, FileAccess.Write);
			var writer = new StreamWriter(oStream);
			writer.Write(s);
			writer.Flush();
			writer.Close();
			oStream.Close();
		}

		public static void WriteLine(string s)
		{
			Console.WriteLine(s);

			var oStream = new FileStream("out.txt", FileMode.OpenOrCreate, FileAccess.Write);
			var writer = new StreamWriter(oStream);
			writer.WriteLine(s);
			writer.Flush();
			writer.Close();
			oStream.Close();
		}

		public static void WriteErr(string s)
		{
			Console.Write(s);

			var errOStream = new FileStream("err.txt", FileMode.Append, FileAccess.Write);
			var errWriter = new StreamWriter(errOStream);
			errWriter.Write(s);
			errWriter.Flush();
			errWriter.Close();
			errOStream.Close();
		}

		public static void WriteLineErr(string s)
		{
			Console.WriteLine(s);

			var errOStream = new FileStream("err.txt", FileMode.Append, FileAccess.Write);
			var errWriter = new StreamWriter(errOStream);
			errWriter.WriteLine(s);
			errWriter.Flush();
			errWriter.Close();
			errOStream.Close();
		}
	}
}
