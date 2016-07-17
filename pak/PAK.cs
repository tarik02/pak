using System;
using System.Collections.Generic;
using System.IO;

namespace PAK
{
	public abstract class PAKException : Exception
	{
		public PAKException(String Message) : base(Message)
		{

		}
	}

	public class InvalidPAKIdException : PAKException
	{
		public InvalidPAKIdException() : base("Invalid PAK id")
		{

		}
	}

	public class InvalidPAKDirLengthException : PAKException
	{
		public InvalidPAKDirLengthException() : base("Invalid PAK dir length exception")
		{

		}
	}


	public class PAKFile
	{
		private struct PAKHeader
		{
			public String Id;
			public uint DirOffset, DirLength;
		}

		private struct PAKEntry
		{
			public String Name;
			public byte[] Data;
		}


		public const String PAKID = "PACK";
		public const uint PAKHeaderSize = 4 + 4 + 4;
		public const uint PAKEntrySize = 56 + 4 + 4;
		private List<PAKEntry> Entries = new List<PAKEntry>();

		private PAKFile()
		{

		}

		

		public void SaveToDirectory(String DirName)
		{
			SaveToDirectory(DirName, S => {  });
		}

		public void SaveToDirectory(String DirName, Action<String> FileCallback)
		{
			Directory.CreateDirectory(DirName);
			
			foreach (var Entry in Entries)
			{
				var FileName = DirName + Path.DirectorySeparatorChar + Entry.Name.Replace('/', Path.DirectorySeparatorChar);
				var FileDir = FileName.Substring(0, FileName.LastIndexOf(Path.DirectorySeparatorChar));

				FileCallback(FileName);


				Directory.CreateDirectory(FileDir);

				var Stream = File.Create(FileName, Entry.Data.Length);
				Stream.Write(Entry.Data, 0, Entry.Data.Length);
				Stream.Close();
			}
		}


		public void SaveToStream(Stream Stream, Action<String> FileCallback, Action SavingCallback)
		{
			using (var Writer = new BinaryWriter(Stream))
			{
				Writer.Write(PAKID.ToCharArray());
				Writer.Write((int)PAKHeaderSize);
				Writer.Write((int)(Entries.Count * PAKEntrySize));

				uint EntryWritePos = (uint)(PAKHeaderSize);
				uint DataWritePos = (uint)(EntryWritePos + Entries.Count * PAKEntrySize);

				foreach (var Entry in Entries)
				{
					FileCallback(Entry.Name);

					Stream.Position = EntryWritePos;

					var Chars = Entry.Name.ToCharArray();
					Writer.Write(Chars);
					Writer.Write(new byte[(EntryWritePos + 56) - Stream.Position]);

					EntryWritePos += PAKEntrySize;


					Writer.Write((int)DataWritePos);
					Writer.Write((int)Entry.Data.Length);


					Stream.Position = DataWritePos;
					Writer.Write(Entry.Data);
					DataWritePos += (uint)Entry.Data.Length;
				}

				SavingCallback();
			}
		}

		public void SaveToPAK(string FileName, Action<String> FileCallback, Action SavingCallback)
		{
			SaveToStream(File.OpenWrite(FileName), FileCallback, SavingCallback);
		}



		public static PAKFile CreateFromFileStream(Stream Stream)
		{
			using (var Reader = new BinaryReader(Stream))
			{
				Stream.Position = 0;

				var Header = new PAKHeader();

				Header.Id = new String(Reader.ReadChars(4));
				Header.DirOffset = (uint)Reader.ReadInt32();
				Header.DirLength = (uint)Reader.ReadInt32();

				if (Header.Id != PAKID)
				{
					throw new InvalidPAKIdException();
				}

				if (Header.DirLength % PAKEntrySize != 0)
				{
					throw new InvalidPAKDirLengthException();
				}


				Stream.Position = Header.DirOffset;


				var EntriesCount = (uint)(Header.DirLength / PAKEntrySize);
				var Result = new PAKFile();
				
				for (uint i = 0; i < EntriesCount; i++)
				{
					var Name = (new String(Reader.ReadChars(56))).Replace("\0", "");
					var Offset = Reader.ReadInt32();
					var Length = Reader.ReadInt32();

					var OldPos = Stream.Position;
					Stream.Position = Offset;

					Result.Entries.Add(new PAKEntry()
					{
						Name = Name,
						Data = Reader.ReadBytes(Length)
					});

					Stream.Position = OldPos;
				}

				return Result;
			}
		}

		public static PAKFile CreateFromFile(String FileName)
		{
			return CreateFromFileStream(File.OpenRead(FileName));
		}


		public static PAKFile CreateFromDirectory(String DirName)
		{
			var Result = new PAKFile();

			PackDirectory(Result, DirName);

			return Result;
		}

		private static void PackDirectory(PAKFile Result, String DirName, String RelativePath = "")
		{
			foreach (var ASubDir in Directory.GetDirectories(DirName))
			{
				var SubDir = ASubDir.Substring(DirName.Length + 1);

				PackDirectory(Result, ASubDir, Path.Combine(RelativePath, SubDir));
			}

			foreach (var ASubFile in Directory.GetFiles(DirName))
			{
				var SubFile = ASubFile.Substring(DirName.Length + 1);

				Result.Entries.Add(new PAKEntry()
				{
					Name = Path.Combine(RelativePath, SubFile),
					Data = File.ReadAllBytes(ASubFile)
				});
			}
		}
	}
}
