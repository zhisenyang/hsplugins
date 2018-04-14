namespace SevenZip.Compression.RangeCoder
{
	// Token: 0x020000E9 RID: 233
	internal struct BitTreeEncoder
	{
		// Token: 0x060004BF RID: 1215 RVA: 0x0001C151 File Offset: 0x0001A351
		public BitTreeEncoder(int numBitLevels)
		{
			this.NumBitLevels = numBitLevels;
			this.Models = new BitEncoder[1 << numBitLevels];
		}

		// Token: 0x060004C0 RID: 1216 RVA: 0x0001C16C File Offset: 0x0001A36C
		public void Init()
		{
			uint i = 1u;
			while (i < (ulong)(1L << (this.NumBitLevels & 31)))
			{
				this.Models[(int)i].Init();
				i += 1u;
			}
		}

		// Token: 0x060004C1 RID: 1217 RVA: 0x0001C1A4 File Offset: 0x0001A3A4
		public void Encode(Encoder rangeEncoder, uint symbol)
		{
			uint i = 1u;
			int bitIndex = this.NumBitLevels;
			while (bitIndex > 0)
			{
				bitIndex--;
				uint bit = symbol >> bitIndex & 1u;
				this.Models[(int)i].Encode(rangeEncoder, bit);
				i = (i << 1 | bit);
			}
		}

		// Token: 0x060004C2 RID: 1218 RVA: 0x0001C1E8 File Offset: 0x0001A3E8
		public void ReverseEncode(Encoder rangeEncoder, uint symbol)
		{
			uint i = 1u;
			uint j = 0u;
			while (j < (ulong)this.NumBitLevels)
			{
				uint bit = symbol & 1u;
				this.Models[(int)i].Encode(rangeEncoder, bit);
				i = (i << 1 | bit);
				symbol >>= 1;
				j += 1u;
			}
		}

		// Token: 0x060004C3 RID: 1219 RVA: 0x0001C22C File Offset: 0x0001A42C
		public uint GetPrice(uint symbol)
		{
			uint price = 0u;
			uint i = 1u;
			int bitIndex = this.NumBitLevels;
			while (bitIndex > 0)
			{
				bitIndex--;
				uint bit = symbol >> bitIndex & 1u;
				price += this.Models[(int)i].GetPrice(bit);
				i = (i << 1) + bit;
			}
			return price;
		}

		// Token: 0x060004C4 RID: 1220 RVA: 0x0001C274 File Offset: 0x0001A474
		public uint ReverseGetPrice(uint symbol)
		{
			uint price = 0u;
			uint i = 1u;
			for (int j = this.NumBitLevels; j > 0; j--)
			{
				uint bit = symbol & 1u;
				symbol >>= 1;
				price += this.Models[(int)i].GetPrice(bit);
				i = (i << 1 | bit);
			}
			return price;
		}

		// Token: 0x060004C5 RID: 1221 RVA: 0x0001C2BC File Offset: 0x0001A4BC
		public static uint ReverseGetPrice(BitEncoder[] Models, uint startIndex, int NumBitLevels, uint symbol)
		{
			uint price = 0u;
			uint i = 1u;
			for (int j = NumBitLevels; j > 0; j--)
			{
				uint bit = symbol & 1u;
				symbol >>= 1;
				price += Models[(int)(startIndex + i)].GetPrice(bit);
				i = (i << 1 | bit);
			}
			return price;
		}

		// Token: 0x060004C6 RID: 1222 RVA: 0x0001C2FC File Offset: 0x0001A4FC
		public static void ReverseEncode(BitEncoder[] Models, uint startIndex, Encoder rangeEncoder, int NumBitLevels, uint symbol)
		{
			uint i = 1u;
			for (int j = 0; j < NumBitLevels; j++)
			{
				uint bit = symbol & 1u;
				Models[(int)(startIndex + i)].Encode(rangeEncoder, bit);
				i = (i << 1 | bit);
				symbol >>= 1;
			}
		}

		// Token: 0x04000615 RID: 1557
		private readonly BitEncoder[] Models;

		// Token: 0x04000616 RID: 1558
		private readonly int NumBitLevels;
	}
}
