namespace SevenZip.Compression.RangeCoder
{
	// Token: 0x020000E8 RID: 232
	internal struct BitDecoder
	{
		// Token: 0x060004BC RID: 1212 RVA: 0x0001C01F File Offset: 0x0001A21F

	    // Token: 0x060004BD RID: 1213 RVA: 0x0001C05B File Offset: 0x0001A25B
		public void Init()
		{
			this.Prob = 1024u;
		}

		// Token: 0x060004BE RID: 1214 RVA: 0x0001C068 File Offset: 0x0001A268
		public uint Decode(Decoder rangeDecoder)
		{
			uint newBound = (rangeDecoder.Range >> 11) * this.Prob;
			if (rangeDecoder.Code < newBound)
			{
				rangeDecoder.Range = newBound;
				this.Prob += 2048u - this.Prob >> 5;
				if (rangeDecoder.Range < 16777216u)
				{
					rangeDecoder.Code = (rangeDecoder.Code << 8 | (byte)rangeDecoder.Stream.ReadByte());
					rangeDecoder.Range <<= 8;
				}
				return 0u;
			}
			rangeDecoder.Range -= newBound;
			rangeDecoder.Code -= newBound;
			this.Prob -= this.Prob >> 5;
			if (rangeDecoder.Range < 16777216u)
			{
				rangeDecoder.Code = (rangeDecoder.Code << 8 | (byte)rangeDecoder.Stream.ReadByte());
				rangeDecoder.Range <<= 8;
			}
			return 1u;
		}

		// Token: 0x04000614 RID: 1556
		private uint Prob;
	}
}
