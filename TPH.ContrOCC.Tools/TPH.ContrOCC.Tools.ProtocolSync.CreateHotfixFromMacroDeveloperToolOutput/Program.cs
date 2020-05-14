using System;

using CLU = Microsoft.Extensions.CommandLineUtils;
using System.IO;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace TPH.ContrOCC.Tools.ProtocolSync.CreateHotfixFromMacroDeveloperToolOutput
{
	class Program
	{
		static void Main(string[] args)
		{
			var app = new CLU.CommandLineApplication();

			app.Name = "CreateHotfixFromMacroDeveloperToolOutput";
			app.Description = ".Net Core console app. Reads a Protocol Sync SQL file containing output from the Macro Developer Tool split by a divider string, reorganises the SQL and outputs to a specified file.";
			var targetFileArgument = app.Argument("TargetFile", "The path and filename of the file containing the Macro Developer Tool output");
			var outputFileArgument = app.Argument("OutputFile", "The path and filename of the file where output will be written to");
			var dividerArgument = app.Argument("Divider", "String used to divide discrete blocks of SQL");
			var overwriteOption = app.Option("-o|--overwrite", "Flag indicating that existing file matching the output file should be overwritten", CLU.CommandOptionType.NoValue);
			app.HelpOption("-?|-h|--help");

			app.OnExecute(() =>
			{
				int result = 0;

				if (string.IsNullOrEmpty(targetFileArgument.Value))
				{
					Console.WriteLine("Error: A target file must be specified!");
					result = 1;
				}
				else if (!File.Exists(targetFileArgument.Value))
				{
					Console.WriteLine("Error: Target file not found!");
					result = 1;
				}
				else if (string.IsNullOrEmpty(outputFileArgument.Value))
				{
					Console.WriteLine("Error: An output file must be specified!");
					result = 1;
				}
				else if (File.Exists(outputFileArgument.Value) && !overwriteOption.HasValue())
				{
					Console.WriteLine("Error: File matching output file argument already exists and overwrite option has not been specified!");
					result = 1;
				}
				else if (string.IsNullOrEmpty(dividerArgument.Value))
				{
					Console.WriteLine("Error: The divider string must be specified!");
					result = 1;
				}
				else
				{
					DoWork(targetFileArgument.Value, outputFileArgument.Value, dividerArgument.Value);
				}

				return result;
			});

			var result = app.Execute(args);

		}

		static void DoWork(string targetFile, string outputFile, string divider)
		{
			string sqlScript = string.Empty;
			Dictionary<string, string> views = new Dictionary<string, string>();
			Dictionary<string, string> procedures = new Dictionary<string, string>();
			Dictionary<string, string> functions = new Dictionary<string, string>();

			#region Read target script

			using (FileStream fs = new FileStream(targetFile, FileMode.Open))
			{
				using (StreamReader sr = new StreamReader(fs))
				{
					sqlScript = sr.ReadToEnd();
				}
			}

			#endregion

			#region Split target script into discrete modules

			string[] sqlModules = sqlScript.Split(divider);

			#endregion

			#region Process modules (obtain module type and name and put into appropriate dictionary)

			Regex regex = new Regex(@"CREATE\s*(PROCEDURE|VIEW|FUNCTION)\s*\[?(ProtocolSync|dbo)\]?\.\[?(\w*)\]?", RegexOptions.IgnoreCase);

			foreach (string sqlModule in sqlModules)
			{
				if (!string.IsNullOrWhiteSpace(sqlModule))
				{
					Match m = regex.Match(sqlModule);

					if (!m.Success)
					{
						throw new ApplicationException($"Could not parse the following module code: {sqlModule}");
					}
					else if (string.Compare(m.Groups[1].Value, "PROCEDURE", true) == 0)
					{
						procedures.Add(m.Groups[3].Value, sqlModule);
					}
					else if (string.Compare(m.Groups[1].Value, "VIEW", true) == 0)
					{
						views.Add(m.Groups[3].Value, sqlModule);
					}
					else if (string.Compare(m.Groups[1].Value, "FUNCTION", true) == 0)
					{
						functions.Add(m.Groups[3].Value, sqlModule);
					}
					else
					{
						throw new ApplicationException($"Unrecognised module type: {m.Groups[1].Value}");
					}
				}
			}

			#endregion

			#region Re-combine modules into output file

			using (FileStream fs = new FileStream(outputFile, FileMode.Create))
			{
				using (StreamWriter sw = new StreamWriter(fs))
				{
					foreach (KeyValuePair<string, string> entry in functions)
					{
						sw.Write(entry.Value);
					}

					foreach (KeyValuePair<string, string> entry in views)
					{
						sw.Write(entry.Value);
					}

					foreach (KeyValuePair<string, string> entry in procedures)
					{
						sw.Write(entry.Value);
					}
				}
			}

			#endregion
		}
	}
}
