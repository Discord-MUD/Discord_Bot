using System;

namespace Discord_Bot.Bot
{
	[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
	public class ModuleInfoAttribute : Attribute
	{
		public string moduleName;
		public string moduleDescription = "";

		public ModuleInfoAttribute(string moduleName)
		{
			this.moduleName = moduleName;
		}
	}
}
