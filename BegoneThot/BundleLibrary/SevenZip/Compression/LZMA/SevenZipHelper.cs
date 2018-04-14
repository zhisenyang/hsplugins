using System;
using System.IO;

namespace SevenZip.Compression.LZMA
{
	// Token: 0x020000F3 RID: 243
	public static class SevenZipHelper
	{
		// Token: 0x06000524 RID: 1316 RVA: 0x0001FE3C File Offset: 0x0001E03C

	    // Token: 0x06000525 RID: 1317 RVA: 0x0001FEB0 File Offset: 0x0001E0B0

	    // Token: 0x06000526 RID: 1318 RVA: 0x0001FF60 File Offset: 0x0001E160
		public static MemoryStream StreamDecompress(MemoryStream newInStream)
		{
			Decoder decoder = new Decoder();
			newInStream.Seek(0L, SeekOrigin.Begin);
			MemoryStream newOutStream = new MemoryStream();
			byte[] properties2 = new byte[5];
			if (newInStream.Read(properties2, 0, 5) != 5)
			{
				throw new Exception("input .lzma is too short");
			}
			long outSize = 0L;
			for (int i = 0; i < 8; i++)
			{
				int v = newInStream.ReadByte();
				if (v < 0)
				{
					throw new Exception("Can't Read 1");
				}
				outSize |= (long)(byte)v << 8 * i;
			}
			decoder.SetDecoderProperties(properties2);
		    decoder.Code(newInStream, newOutStream, outSize);
			newOutStream.Position = 0L;
			return newOutStream;
		}

		// Token: 0x06000527 RID: 1319 RVA: 0x00020008 File Offset: 0x0001E208
		public static MemoryStream StreamDecompress(MemoryStream newInStream, long outSize)
		{
			Decoder decoder = new Decoder();
			newInStream.Seek(0L, SeekOrigin.Begin);
			MemoryStream newOutStream = new MemoryStream();
			byte[] properties2 = new byte[5];
			if (newInStream.Read(properties2, 0, 5) != 5)
			{
				throw new Exception("input .lzma is too short");
			}
			decoder.SetDecoderProperties(properties2);
		    decoder.Code(newInStream, newOutStream, outSize);
			newOutStream.Position = 0L;
			return newOutStream;
		}

		// Token: 0x040006A4 RID: 1700

	    // Token: 0x040006A5 RID: 1701

	    // Token: 0x040006A6 RID: 1702

	    // Token: 0x040006A7 RID: 1703
	}
}
