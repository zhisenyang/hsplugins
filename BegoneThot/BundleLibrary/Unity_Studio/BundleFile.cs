using System;
using System.Collections.Generic;
using System.IO;
using Lz4;
using SevenZip.Compression.LZMA;

namespace Unity_Studio
{
	// Token: 0x020000B6 RID: 182
	public class BundleFile
	{
		// Token: 0x060003A1 RID: 929 RVA: 0x00006550 File Offset: 0x00004750
		public BundleFile(string fileName)
		{
			if (Path.GetExtension(fileName) == ".lz4")
			{
				byte[] filebuffer;
				using (BinaryReader lz4Stream = new BinaryReader(File.OpenRead(fileName)))
				{
					lz4Stream.ReadInt32();
					int uncompressedSize = lz4Stream.ReadInt32();
					int compressedSize = lz4Stream.ReadInt32();
					lz4Stream.ReadInt32();
					using (MemoryStream inputStream = new MemoryStream(lz4Stream.ReadBytes(compressedSize)))
					{
						Lz4DecoderStream lz4DecoderStream = new Lz4DecoderStream(inputStream);
						filebuffer = new byte[uncompressedSize];
						lz4DecoderStream.Read(filebuffer, 0, uncompressedSize);
						lz4DecoderStream.Dispose();
					}
				}
				using (EndianBinaryReader b_Stream = new EndianBinaryReader(new MemoryStream(filebuffer)))
				{
					this.readBundle(b_Stream);
					return;
				}
			}
			using (EndianBinaryReader b_Stream2 = new EndianBinaryReader(File.OpenRead(fileName)))
			{
				this.readBundle(b_Stream2);
			}
		}

		// Token: 0x060003A2 RID: 930 RVA: 0x00006674 File Offset: 0x00004874
		private void readBundle(EndianBinaryReader b_Stream)
		{
			string signature = b_Stream.ReadStringToNull();
			if (signature == "UnityWeb" || signature == "UnityRaw" || signature == "úúúúúúúú")
			{
				this.format = b_Stream.ReadInt32();
				b_Stream.ReadStringToNull();
				this.versionEngine = b_Stream.ReadStringToNull();
				if (this.format < 6)
				{
					b_Stream.ReadInt32();
				}
				else if (this.format == 6)
				{
					this.ReadFormat6(b_Stream, true);
					return;
				}
				b_Stream.ReadInt16();
				int offset = b_Stream.ReadInt16();
				b_Stream.ReadInt32();
				int lzmaChunks = b_Stream.ReadInt32();
				int lzmaSize = 0;
				for (int i = 0; i < lzmaChunks; i++)
				{
					lzmaSize = b_Stream.ReadInt32();
					b_Stream.ReadInt32();
				}
				b_Stream.Position = offset;
				if (signature != "úúúúúúúú" && signature != "UnityWeb")
				{
					if (signature != "UnityRaw")
					{
						return;
					}
				}
				else
				{
					using (EndianBinaryReader lzmaStream = new EndianBinaryReader(SevenZipHelper.StreamDecompress(new MemoryStream(b_Stream.ReadBytes(lzmaSize)))))
					{
						this.getFiles(lzmaStream, 0);
						return;
					}
				}
				this.getFiles(b_Stream, offset);
				return;
			}
			if (signature != "UnityFS")
			{
				return;
			}
			this.format = b_Stream.ReadInt32();
			b_Stream.ReadStringToNull();
			this.versionEngine = b_Stream.ReadStringToNull();
			if (this.format == 6)
			{
				this.ReadFormat6(b_Stream);
			}
		}

		// Token: 0x060003A3 RID: 931 RVA: 0x000067F4 File Offset: 0x000049F4
		private void getFiles(EndianBinaryReader f_Stream, int offset)
		{
			int fileCount = f_Stream.ReadInt32();
			for (int i = 0; i < fileCount; i++)
			{
				MemoryAssetsFile memFile = new MemoryAssetsFile();
				memFile.fileName = f_Stream.ReadStringToNull();
				int fileOffset = f_Stream.ReadInt32();
				fileOffset += offset;
				int fileSize = f_Stream.ReadInt32();
				long nextFile = f_Stream.Position;
				f_Stream.Position = fileOffset;
				byte[] buffer = f_Stream.ReadBytes(fileSize);
				memFile.memStream = new MemoryStream(buffer);
				this.MemoryAssetsFileList.Add(memFile);
				f_Stream.Position = nextFile;
			}
		}

		// Token: 0x060003A4 RID: 932 RVA: 0x00006874 File Offset: 0x00004A74
		private void ReadFormat6(EndianBinaryReader b_Stream, bool padding = false)
		{
			b_Stream.ReadInt64();
			int compressedSize = b_Stream.ReadInt32();
			int uncompressedSize = b_Stream.ReadInt32();
			int num = b_Stream.ReadInt32();
			if (padding)
			{
				b_Stream.ReadByte();
			}
			byte[] blocksInfoBytes;
			if ((num & 128) != 0)
			{
				long position = b_Stream.Position;
				b_Stream.Position = b_Stream.BaseStream.Length - compressedSize;
				blocksInfoBytes = b_Stream.ReadBytes(compressedSize);
				b_Stream.Position = position;
			}
			else
			{
				blocksInfoBytes = b_Stream.ReadBytes(compressedSize);
			}
			int num2 = num & 63;
			MemoryStream blocksInfoStream;
			if (num2 != 1)
			{
				if (num2 - 2 > 1)
				{
					blocksInfoStream = new MemoryStream(blocksInfoBytes);
				}
				else
				{
					byte[] uncompressedBytes = new byte[uncompressedSize];
					using (MemoryStream mstream = new MemoryStream(blocksInfoBytes))
					{
						Lz4DecoderStream lz4DecoderStream = new Lz4DecoderStream(mstream);
						lz4DecoderStream.Read(uncompressedBytes, 0, uncompressedSize);
						lz4DecoderStream.Dispose();
					}
					blocksInfoStream = new MemoryStream(uncompressedBytes);
				}
			}
			else
			{
				blocksInfoStream = SevenZipHelper.StreamDecompress(new MemoryStream(blocksInfoBytes));
			}
			using (EndianBinaryReader blocksInfo = new EndianBinaryReader(blocksInfoStream))
			{
				blocksInfo.Position = 16L;
				int blockcount = blocksInfo.ReadInt32();
				MemoryStream assetsDataStream = new MemoryStream();
				for (int i = 0; i < blockcount; i++)
				{
					uncompressedSize = blocksInfo.ReadInt32();
					compressedSize = blocksInfo.ReadInt32();
					int num3 = blocksInfo.ReadInt16();
					byte[] compressedBytes = b_Stream.ReadBytes(compressedSize);
					num2 = (num3 & 63);
				    if (num2 == 1)
				    {
				        byte[] uncompressedBytes3 = new byte[uncompressedSize];
				        using (MemoryStream mstream3 = new MemoryStream(compressedBytes))
				        {
				            MemoryStream memoryStream = SevenZipHelper.StreamDecompress(mstream3, uncompressedSize);
				            memoryStream.Read(uncompressedBytes3, 0, uncompressedSize);
				            memoryStream.Dispose();
				        }
				        assetsDataStream.Write(uncompressedBytes3, 0, uncompressedSize);
				    }
				    else
				    {
				        if (num2 - 2 < 1)
				            assetsDataStream.Write(compressedBytes, 0, compressedSize);
				        else
				        {
				            byte[] uncompressedBytes2 = new byte[uncompressedSize];
				            using (MemoryStream mstream2 = new MemoryStream(compressedBytes))
				            {
				                Lz4DecoderStream lz4DecoderStream2 = new Lz4DecoderStream(mstream2);
				                lz4DecoderStream2.Read(uncompressedBytes2, 0, uncompressedSize);
				                lz4DecoderStream2.Dispose();
				            }
				            assetsDataStream.Write(uncompressedBytes2, 0, uncompressedSize);
				        }
				    }
				}
				using (EndianBinaryReader assetsData = new EndianBinaryReader(assetsDataStream))
				{
					int entryinfo_count = blocksInfo.ReadInt32();
					for (int j = 0; j < entryinfo_count; j++)
					{
						MemoryAssetsFile memFile = new MemoryAssetsFile();
						long entryinfo_offset = blocksInfo.ReadInt64();
						long entryinfo_size = blocksInfo.ReadInt64();
						blocksInfo.ReadInt32();
						memFile.fileName = blocksInfo.ReadStringToNull();
						assetsData.Position = entryinfo_offset;
						byte[] buffer = assetsData.ReadBytes((int)entryinfo_size);
						memFile.memStream = new MemoryStream(buffer);
						this.MemoryAssetsFileList.Add(memFile);
					}
				}
			}
		}

		public int format;
		public string versionEngine;
		public List<MemoryAssetsFile> MemoryAssetsFileList = new List<MemoryAssetsFile>();

		public class MemoryAssetsFile
		{
			public string fileName;
			public MemoryStream memStream;
		}
	}
}
