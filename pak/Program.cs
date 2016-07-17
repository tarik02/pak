using System;
using System.Collections.Generic;
using System.IO;

namespace PAK
{
	class Program
	{
		public static void Main(String[] Args)
		{
			new Program(new Queue<String>(Args));
		}


		public Program(Queue<String> Args)
		{
			if ((Args.Count < 1) || (Args.Count > 3))
			{
				PrintMessage("error.usage");
				return;
			}

			var Mode = Args.Peek();

			switch (Mode)
			{
			case "-pack":
				Args.Dequeue();
				Pack(Args);
				break;
			case "-unpack":
				Args.Dequeue();
				UnPack(Args);
				break;
			default:
				if (Directory.Exists(Mode))
				{
					Pack(Args);
				}
				else if(File.Exists(Mode))
				{
					UnPack(Args);
				}
				else
				{
					PrintMessage("error.usage");
				}
				break;
			}
		}
		

		private void Pack(Queue<string> Args)
		{
			String Input = Args.Dequeue();
			String Output = Input + ".pak";

			try { Output = Args.Dequeue(); } catch {  }

			if (!Directory.Exists(Input))
			{
				PrintMessage("pack.error.output-not-found", Input);
				return;
			}

			try
			{
				PAK.PAKFile.CreateFromDirectory(Input).SaveToPAK(Output, FileName =>
				{
					PrintMessage("pack.process", FileName);
				}, () =>
				{
					PrintMessage("pack.saving");
				});

				PrintMessage("pack.done");
			}
			catch (PAKException Exception)
			{
				PrintMessage("pack.error.custom", Exception);
			}
		}

		private void UnPack(Queue<string> Args)
		{
			String Input = Args.Dequeue();
			String Output = Input.Substring(0, Input.LastIndexOf("."));

			try { Output = Args.Dequeue(); } catch {  }

			if (!File.Exists(Input))
			{
				PrintMessage("unpack.error.input-not-found", Input);
				return;
			}

			try
			{
				PAK.PAKFile.CreateFromFile(Input).SaveToDirectory(Output, FileName =>
				{
					PrintMessage("unpack.process", FileName);
				});

				PrintMessage("unpack.done");
			}
			catch (PAKException Exception)
			{
				PrintMessage("unpack.error.custom", Exception);
			}
		}

		

		private static Dictionary<String, String> Messages = new Dictionary<string, string>()
		{
			{ "error.usage", "Usage: -pack <input directory> [output file] or -unpack <input file> [output directory] or <input directory> [output file] or <input file> [output directory]" },


			{ "unpack.error.input-not-found", "Input file {0} is not exists" },
			{ "unpack.error.custom", "Error while unpacking: {0}" },

			{ "unpack.process", "Unpacking file {0}..." },
			{ "unpack.done", "Unpacking done" },


			{ "pack.error.output-not-found", "Output file {0} is not exists" },
			{ "pack.error.custom", "Error while packing: {0}" },

			{ "pack.process", "Packing file {0}..." },
			{ "pack.saving", "Saving output" },
			{ "pack.done", "Packing done" },
		};

		private static String GetMessage(String Message)
		{
			Messages.TryGetValue(Message, out Message);

			return Message;
		}

		private static void PrintMessage(String Message, params Object[] Args)
		{
			Console.WriteLine(String.Format(GetMessage(Message), Args));
		}
	}
}
