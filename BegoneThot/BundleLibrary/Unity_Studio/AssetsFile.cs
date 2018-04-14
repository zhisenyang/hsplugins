using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace Unity_Studio
{
	// Token: 0x020000D8 RID: 216
	public class AssetsFile
	{
		// Token: 0x0600040D RID: 1037 RVA: 0x00014230 File Offset: 0x00012430
		public AssetsFile(EndianBinaryReader fileStream)
		{
			this.a_Stream = fileStream;
		    try
			{
				int tableSize = this.a_Stream.ReadInt32();
				int dataEnd = this.a_Stream.ReadInt32();
				this.fileGen = this.a_Stream.ReadInt32();
				uint dataOffset = this.a_Stream.ReadUInt32();
			    switch (this.fileGen)
				{
				case 6:
					this.a_Stream.Position = dataEnd - tableSize;
					this.a_Stream.Position += 1L;
					goto IL_268;
				case 7:
					this.a_Stream.Position = dataEnd - tableSize;
					this.a_Stream.Position += 1L;
					this.m_Version = this.a_Stream.ReadStringToNull();
					goto IL_268;
				case 8:
					this.a_Stream.Position = dataEnd - tableSize;
					this.a_Stream.Position += 1L;
					this.m_Version = this.a_Stream.ReadStringToNull();
					this.platform = this.a_Stream.ReadInt32();
					goto IL_268;
				case 9:
					this.a_Stream.Position += 4L;
					this.m_Version = this.a_Stream.ReadStringToNull();
					this.platform = this.a_Stream.ReadInt32();
					goto IL_268;
				case 14:
				case 15:
				case 16:
				case 17:
					this.a_Stream.Position += 4L;
					this.m_Version = this.a_Stream.ReadStringToNull();
					this.platform = this.a_Stream.ReadInt32();
					this.baseDefinitions = this.a_Stream.ReadBoolean();
					goto IL_268;
				}
				return;
				IL_268:
				if (this.platform > 255 || this.platform < 0)
				{
					byte[] b32 = BitConverter.GetBytes(this.platform);
					Array.Reverse(b32);
					this.platform = BitConverter.ToInt32(b32, 0);
					this.a_Stream.endian = EndianType.LittleEndian;
				}
				int num = this.platform;
				if (num <= 19)
				{
					switch (num)
					{
					case -2:
					    goto IL_40E;
					case -1:
					case 0:
					case 1:
					case 2:
					case 3:
					case 8:
					case 12:
					case 14:
					case 15:
						break;
					case 4:
					    goto IL_40E;
					case 5:
					    goto IL_40E;
					case 6:
					    goto IL_40E;
					case 7:
					    goto IL_40E;
					case 9:
					    goto IL_40E;
					case 10:
					    goto IL_40E;
					case 11:
					    goto IL_40E;
					case 13:
					    goto IL_40E;
					case 16:
					    goto IL_40E;
					default:
						if (num == 19)
						{
						}
						break;
					}
				}
				else
				{
					if (num == 21)
					{
					    goto IL_40E;
					}
					if (num == 25)
					{
					    goto IL_40E;
					}
					if (num == 29)
					{
					}
				}
			    IL_40E:
				int baseCount = this.a_Stream.ReadInt32();
				for (int i = 0; i < baseCount; i++)
				{
					if (this.fileGen < 14)
					{
						this.a_Stream.ReadInt32();
						this.a_Stream.ReadStringToNull();
						this.a_Stream.ReadStringToNull();
						this.a_Stream.Position += 20L;
						int memberCount = this.a_Stream.ReadInt32();
						List<ClassMember> cb = new List<ClassMember>();
						for (int j = 0; j < memberCount; j++)
						{
							this.readBase(cb, 1);
						}
					}
					else
					{
						this.readBase5();
					}
				}
				if (this.fileGen >= 7 && this.fileGen < 14)
				{
					this.a_Stream.Position += 4L;
				}
				int assetCount = this.a_Stream.ReadInt32();
				string assetIDfmt = "D" + assetCount.ToString().Length;
				for (int k = 0; k < assetCount; k++)
				{
					if (this.fileGen >= 14)
					{
						this.a_Stream.AlignStream(4);
					}
					AssetPreloadData asset = new AssetPreloadData();
					asset.m_PathID = ((this.fileGen < 14) ? this.a_Stream.ReadInt32() : this.a_Stream.ReadInt64());
					asset.Offset = this.a_Stream.ReadUInt32();
					asset.Offset += dataOffset;
					this.a_Stream.ReadInt32();
					if (this.fileGen > 15)
					{
						int index = this.a_Stream.ReadInt32();
						asset.Type2 = this.classIDs[index][1];
					}
					else
					{
						this.a_Stream.ReadInt32();
						asset.Type2 = this.a_Stream.ReadUInt16();
						this.a_Stream.Position += 2L;
					}
					if (this.fileGen == 15)
					{
						this.a_Stream.ReadByte();
					}
				    asset.uniqueID = k.ToString(assetIDfmt);
					asset.sourceFile = this;
					this.preloadTable.Add(asset.m_PathID, asset);
					if (asset.Type2 == 141 && this.fileGen == 6)
					{
						long nextAsset = this.a_Stream.Position;
						BuildSettings BSettings = new BuildSettings(asset);
						this.m_Version = BSettings.m_Version;
						this.a_Stream.Position = nextAsset;
					}
				}
				this.buildType = this.m_Version.Split(buildTypeSplit, StringSplitOptions.RemoveEmptyEntries);
				IEnumerable<string> strver = from Match m in Regex.Matches(this.m_Version, "[0-9]")
				select m.Value;
				this.version = Array.ConvertAll(strver.ToArray(), int.Parse);
				if (this.version[0] == 2 && this.version[1] == 0 && this.version[2] == 1 && this.version[3] == 7)
				{
					int[] nversion = new int[this.version.Length - 3];
					nversion[0] = 2017;
					Array.Copy(this.version, 4, nversion, 1, this.version.Length - 4);
					this.version = nversion;
				}
				if (this.fileGen >= 14)
				{
					int someCount = this.a_Stream.ReadInt32();
					for (int l = 0; l < someCount; l++)
					{
						this.a_Stream.ReadInt32();
						this.a_Stream.AlignStream(4);
						this.a_Stream.ReadInt64();
					}
				}
				int sharedFileCount = this.a_Stream.ReadInt32();
				for (int n = 0; n < sharedFileCount; n++)
				{
					UnityShared shared = new UnityShared();
					this.a_Stream.ReadStringToNull();
					this.a_Stream.Position += 20L;
					this.a_Stream.ReadStringToNull();
				    this.sharedAssetsList.Add(shared);
				}
				this.valid = true;
			}
			catch
			{
			}
		}

		// Token: 0x0600040E RID: 1038 RVA: 0x00014B38 File Offset: 0x00012D38
		private void readBase(List<ClassMember> cb, int level)
		{
			string varType = this.a_Stream.ReadStringToNull();
			string varName = this.a_Stream.ReadStringToNull();
			int size = this.a_Stream.ReadInt32();
			this.a_Stream.ReadInt32();
			this.a_Stream.ReadInt32();
			this.a_Stream.ReadInt32();
			int flag = this.a_Stream.ReadInt32();
			int childrenCount = this.a_Stream.ReadInt32();
			cb.Add(new ClassMember
			{
				Level = level - 1,
				Type = varType,
				Name = varName,
				Size = size,
				Flag = flag
			});
			for (int i = 0; i < childrenCount; i++)
			{
				this.readBase(cb, level + 1);
			}
		}

		// Token: 0x0600040F RID: 1039 RVA: 0x00014BF4 File Offset: 0x00012DF4
		private void readBase5()
		{
			int classID = this.a_Stream.ReadInt32();
			if (this.fileGen > 15)
			{
				this.a_Stream.ReadByte();
				int type;
				if ((type = this.a_Stream.ReadInt16()) >= 0)
				{
					type = -1 - type;
				}
				else
				{
					type = classID;
				}
				this.classIDs.Add(new[]
				{
					type,
					classID
				});
				if (classID == 114)
				{
					this.a_Stream.Position += 16L;
				}
			}
			else if (classID < 0)
			{
				this.a_Stream.Position += 16L;
			}
			this.a_Stream.Position += 16L;
			if (this.baseDefinitions)
			{
				int varCount = this.a_Stream.ReadInt32();
				int stringSize = this.a_Stream.ReadInt32();
				this.a_Stream.Position += varCount * 24;
				EndianBinaryReader stringReader = new EndianBinaryReader(new MemoryStream(this.a_Stream.ReadBytes(stringSize)));
				this.a_Stream.Position -= varCount * 24 + stringSize;
				for (int i = 0; i < varCount; i++)
				{
					this.a_Stream.ReadUInt16();
					this.a_Stream.ReadByte();
					this.a_Stream.ReadBoolean();
					ushort varTypeIndex = this.a_Stream.ReadUInt16();
				    if (this.a_Stream.ReadUInt16() == 0)
					{
						stringReader.Position = varTypeIndex;
						stringReader.ReadStringToNull();
					}
					ushort varNameIndex = this.a_Stream.ReadUInt16();
				    if (this.a_Stream.ReadUInt16() == 0)
					{
						stringReader.Position = varNameIndex;
						stringReader.ReadStringToNull();
					}
					this.a_Stream.ReadInt32();
					this.a_Stream.ReadInt32();
					this.a_Stream.ReadInt32();
				}
				stringReader.Dispose();
				this.a_Stream.Position += stringSize;
			}
		}

		// Token: 0x0400055F RID: 1375
		public EndianBinaryReader a_Stream;

		// Token: 0x04000560 RID: 1376

	    // Token: 0x04000561 RID: 1377

	    // Token: 0x04000562 RID: 1378

	    // Token: 0x04000563 RID: 1379
		public int fileGen;

		// Token: 0x04000564 RID: 1380
		public bool valid;

		// Token: 0x04000565 RID: 1381
		public string m_Version = "2.5.0f5";

		// Token: 0x04000566 RID: 1382
		public int[] version = new int[4];

		// Token: 0x04000567 RID: 1383
		public string[] buildType;

		// Token: 0x04000568 RID: 1384
		public int platform = 100663296;

		// Token: 0x04000569 RID: 1385

	    // Token: 0x0400056A RID: 1386
		public Dictionary<long, AssetPreloadData> preloadTable = new Dictionary<long, AssetPreloadData>();

		// Token: 0x0400056B RID: 1387
		public Dictionary<long, GameObject> GameObjectList = new Dictionary<long, GameObject>();

		// Token: 0x0400056C RID: 1388
		public Dictionary<long, Transform> TransformList = new Dictionary<long, Transform>();

		// Token: 0x0400056D RID: 1389

	    // Token: 0x0400056E RID: 1390
		public List<UnityShared> sharedAssetsList = new List<UnityShared>
		{
			new UnityShared()
		};

		// Token: 0x0400056F RID: 1391

		// Token: 0x04000570 RID: 1392
		private readonly bool baseDefinitions;

		// Token: 0x04000571 RID: 1393
		private readonly List<int[]> classIDs = new List<int[]>();

		// Token: 0x04000572 RID: 1394
		public static readonly string[] buildTypeSplit = {
			".",
			"0",
			"1",
			"2",
			"3",
			"4",
			"5",
			"6",
			"7",
			"8",
			"9"
		};

		// Token: 0x04000573 RID: 1395

	    // Token: 0x0200010F RID: 271
		public class UnityShared
		{
			// Token: 0x04000706 RID: 1798
			public int Index = -1;

			// Token: 0x04000707 RID: 1799

		    // Token: 0x04000708 RID: 1800
		}
	}
}
