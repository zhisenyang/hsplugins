using System;

namespace SevenZip.Compression.RangeCoder
{
	// Token: 0x020000E7 RID: 231
	internal struct BitEncoder
	{
		// Token: 0x060004B5 RID: 1205 RVA: 0x0001BE9C File Offset: 0x0001A09C
		public void Init()
		{
			this.Prob = 1024u;
		}

		// Token: 0x060004B6 RID: 1206 RVA: 0x0001BEA9 File Offset: 0x0001A0A9

	    // Token: 0x060004B7 RID: 1207 RVA: 0x0001BEE0 File Offset: 0x0001A0E0
		public void Encode(Encoder encoder, uint symbol)
		{
			uint newBound = (encoder.Range >> 11) * this.Prob;
			if (symbol == 0u)
			{
				encoder.Range = newBound;
				this.Prob += 2048u - this.Prob >> 5;
			}
			else
			{
				encoder.Low += newBound;
				encoder.Range -= newBound;
				this.Prob -= this.Prob >> 5;
			}
			if (encoder.Range < 16777216u)
			{
				encoder.Range <<= 8;
				encoder.ShiftLow();
			}
		}

		// Token: 0x060004B8 RID: 1208 RVA: 0x0001BF78 File Offset: 0x0001A178
		static BitEncoder()
		{
			for (int i = 8; i >= 0; i--)
			{
				uint num = 1u << 9 - i - 1;
				uint end = 1u << 9 - i;
				for (uint j = num; j < end; j += 1u)
				{
					ProbPrices[(int)j] = (uint)((i << 6) + (int)(end - j << 6 >> 9 - i - 1));
				}
			}
		}

		// Token: 0x060004B9 RID: 1209 RVA: 0x0001BFDA File Offset: 0x0001A1DA
		public uint GetPrice(uint symbol)
		{
			return ProbPrices[(int)((IntPtr)((unchecked(this.Prob - symbol ^ (ulong)-symbol) & 2047UL) >> 2))];
		}

		// Token: 0x060004BA RID: 1210 RVA: 0x0001BFF9 File Offset: 0x0001A1F9
		public uint GetPrice0()
		{
			return ProbPrices[(int)(this.Prob >> 2)];
		}

		// Token: 0x060004BB RID: 1211 RVA: 0x0001C009 File Offset: 0x0001A209
		public uint GetPrice1()
		{
			return ProbPrices[(int)(2048u - this.Prob >> 2)];
		}

		// Token: 0x0400060A RID: 1546

	    // Token: 0x0400060B RID: 1547

	    // Token: 0x0400060C RID: 1548

	    // Token: 0x0400060D RID: 1549

	    // Token: 0x0400060E RID: 1550

	    // Token: 0x0400060F RID: 1551
		private uint Prob;

		// Token: 0x04000610 RID: 1552
		private static readonly uint[] ProbPrices = new uint[512];
	}
}
