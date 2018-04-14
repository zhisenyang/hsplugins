using System.IO;

namespace SevenZip.Compression.RangeCoder
{
	// Token: 0x020000E6 RID: 230
	internal class Decoder
	{
		// Token: 0x060004AB RID: 1195 RVA: 0x0001BCB0 File Offset: 0x00019EB0
		public void Init(Stream stream)
		{
			this.Stream = stream;
			this.Code = 0u;
			this.Range = uint.MaxValue;
			for (int i = 0; i < 5; i++)
			{
				this.Code = (this.Code << 8 | (byte)this.Stream.ReadByte());
			}
		}

		// Token: 0x060004AC RID: 1196 RVA: 0x0001BCF9 File Offset: 0x00019EF9
		public void ReleaseStream()
		{
			this.Stream = null;
		}

		// Token: 0x060004AD RID: 1197 RVA: 0x0001BD02 File Offset: 0x00019F02

	    // Token: 0x060004AE RID: 1198 RVA: 0x0001BD0F File Offset: 0x00019F0F

	    // Token: 0x060004AF RID: 1199 RVA: 0x0001BD49 File Offset: 0x00019F49

	    // Token: 0x060004B0 RID: 1200 RVA: 0x0001BD84 File Offset: 0x00019F84

	    // Token: 0x060004B1 RID: 1201 RVA: 0x0001BDA9 File Offset: 0x00019FA9

	    // Token: 0x060004B2 RID: 1202 RVA: 0x0001BDD4 File Offset: 0x00019FD4
		public uint DecodeDirectBits(int numTotalBits)
		{
			uint range = this.Range;
			uint code = this.Code;
			uint result = 0u;
			for (int i = numTotalBits; i > 0; i--)
			{
				range >>= 1;
				uint t = code - range >> 31;
				code -= (range & t - 1u);
				result = (result << 1 | 1u - t);
				if (range < 16777216u)
				{
					code = (code << 8 | (byte)this.Stream.ReadByte());
					range <<= 8;
				}
			}
			this.Range = range;
			this.Code = code;
			return result;
		}

		// Token: 0x060004B3 RID: 1203 RVA: 0x0001BE48 File Offset: 0x0001A048

	    // Token: 0x04000606 RID: 1542

	    // Token: 0x04000607 RID: 1543
		public uint Range;

		// Token: 0x04000608 RID: 1544
		public uint Code;

		// Token: 0x04000609 RID: 1545
		public Stream Stream;
	}
}
