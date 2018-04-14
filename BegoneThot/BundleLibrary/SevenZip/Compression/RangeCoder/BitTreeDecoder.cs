namespace SevenZip.Compression.RangeCoder
{
	// Token: 0x020000EA RID: 234
	internal struct BitTreeDecoder
	{
		// Token: 0x060004C7 RID: 1223 RVA: 0x0001C338 File Offset: 0x0001A538
		public BitTreeDecoder(int numBitLevels)
		{
			this.NumBitLevels = numBitLevels;
			this.Models = new BitDecoder[1 << numBitLevels];
		}

		// Token: 0x060004C8 RID: 1224 RVA: 0x0001C354 File Offset: 0x0001A554
		public void Init()
		{
			uint i = 1u;
			while (i < (ulong)(1L << (this.NumBitLevels & 31)))
			{
				this.Models[(int)i].Init();
				i += 1u;
			}
		}

		// Token: 0x060004C9 RID: 1225 RVA: 0x0001C38C File Offset: 0x0001A58C
		public uint Decode(Decoder rangeDecoder)
		{
			uint i = 1u;
			for (int bitIndex = this.NumBitLevels; bitIndex > 0; bitIndex--)
			{
				i = (i << 1) + this.Models[(int)i].Decode(rangeDecoder);
			}
			return i - (1u << this.NumBitLevels);
		}

		// Token: 0x060004CA RID: 1226 RVA: 0x0001C3D0 File Offset: 0x0001A5D0
		public uint ReverseDecode(Decoder rangeDecoder)
		{
			uint i = 1u;
			uint symbol = 0u;
			for (int bitIndex = 0; bitIndex < this.NumBitLevels; bitIndex++)
			{
				uint bit = this.Models[(int)i].Decode(rangeDecoder);
				i <<= 1;
				i += bit;
				symbol |= bit << bitIndex;
			}
			return symbol;
		}

		// Token: 0x060004CB RID: 1227 RVA: 0x0001C418 File Offset: 0x0001A618
		public static uint ReverseDecode(BitDecoder[] Models, uint startIndex, Decoder rangeDecoder, int NumBitLevels)
		{
			uint i = 1u;
			uint symbol = 0u;
			for (int bitIndex = 0; bitIndex < NumBitLevels; bitIndex++)
			{
				uint bit = Models[(int)(startIndex + i)].Decode(rangeDecoder);
				i <<= 1;
				i += bit;
				symbol |= bit << bitIndex;
			}
			return symbol;
		}

		// Token: 0x04000617 RID: 1559
		private readonly BitDecoder[] Models;

		// Token: 0x04000618 RID: 1560
		private readonly int NumBitLevels;
	}
}
