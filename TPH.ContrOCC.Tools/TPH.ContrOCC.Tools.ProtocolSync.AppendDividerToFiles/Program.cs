using System;

using CLU = Microsoft.Extensions.CommandLineUtils;
using System.IO;

namespace TPH.ContrOCC.Tools.ProtocolSync.AppendDividerToFiles
{
	class Program
	{
		const string DEFAULT_DIVIDER = "/* DIVIDER */";

		static void Main(string[] args)
		{
			var app = new CLU.CommandLineApplication();

			app.Name = "AppendDividerToFiles";
			app.Description = ".Net Core console app. Appends a divider string to all files in a given directory.";
			var targetFolderArgument = app.Argument("TargetFolder", "The folder containing the files you wish to target");
			var dividerOption = app.Option("-d|--divider", "The divider to append.", CLU.CommandOptionType.SingleValue);
			app.HelpOption("-?|-h|--help");

			app.OnExecute(() =>
			{
				int result = 0;

				if (string.IsNullOrEmpty(targetFolderArgument.Value))
				{
					Console.WriteLine("Error: A target folder must be specified!");
					result = 1;
				}
				else if (!Directory.Exists(targetFolderArgument.Value))
				{
					Console.WriteLine("Error: Target folder not found!");
					result = 1;
				}
				else
				{
					string divider = (dividerOption.HasValue()) ? dividerOption.Value() : DEFAULT_DIVIDER;
					DoWork(targetFolderArgument.Value, divider);
				}

				return result;
			});
			
			var result = app.Execute(args);
		}

		static void DoWork(string targetFolder, string divider)
		{
			foreach (string file in Directory.GetFiles(targetFolder,"*.sql", SearchOption.AllDirectories))
			{
				using (FileStream fs = new FileStream(file, FileMode.Append))
				{
					using (StreamWriter sw = new StreamWriter(fs))
					{
						sw.Write(Environment.NewLine);
						sw.Write(divider);
					}
				}
			}
		}
	}
}
