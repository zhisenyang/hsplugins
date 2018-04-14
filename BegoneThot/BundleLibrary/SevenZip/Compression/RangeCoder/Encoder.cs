using System.IO;

namespace SevenZip.Compression.RangeCoder
{
	// Token: 0x020000E5 RID: 229
	internal class Encoder
	{
		// Token: 0x0600049F RID: 1183 RVA: 0x0001BA4C File Offset: 0x00019C4C
		public void SetStream(Stream stream)
		{
			this.Stream = stream;
		}

		// Token: 0x060004A0 RID: 1184 RVA: 0x0001BA55 File Offset: 0x00019C55
		public void ReleaseStream()
		{
			this.Stream = null;
		}

		// Token: 0x060004A1 RID: 1185 RVA: 0x0001BA5E File Offset: 0x00019C5E
		public void Init()
		{
			this.StartPosition = this.Stream.Position;
			this.Low = 0UL;
			this.Range = uint.MaxValue;
			this._cacheSize = 1u;
			this._cache = 0;
		}

		// Token: 0x060004A2 RID: 1186 RVA: 0x0001BA90 File Offset: 0x00019C90
		public void FlushData()
		{
			for (int i = 0; i < 5; i++)
			{
				this.ShiftLow();
			}
		}

		// Token: 0x060004A3 RID: 1187 RVA: 0x0001BAAF File Offset: 0x00019CAF
		public void FlushStream()
		{
			this.Stream.Flush();
		}

		// Token: 0x060004A4 RID: 1188 RVA: 0x0001BABC File Offset: 0x00019CBC

	    // Token: 0x060004A5 RID: 1189 RVA: 0x0001BACC File Offset: 0x00019CCC

	    // Token: 0x060004A6 RID: 1190 RVA: 0x0001BB2C File Offset: 0x00019D2C
		public void ShiftLow()
		{
			if ((uint)this.Low < 4278190080u || (uint)(this.Low >> 32) == 1u)
			{
				byte temp = this._cache;
				uint num;
				do
				{
					this.Stream.WriteByte((byte)(temp + (this.Low >> 32)));
					temp = byte.MaxValue;
					num = this._cacheSize - 1u;
					this._cacheSize = num;
				}
				while (num != 0u);
				this._cache = (byte)((uint)this.Low >> 24);
			}
			this._cacheSize += 1u;
			this.Low = (ulong)((uint)this.Low) << 8;
		}

		// Token: 0x060004A7 RID: 1191 RVA: 0x0001BBBC File Offset: 0x00019DBC
		public void EncodeDirectBits(uint v, int numTotalBits)
		{
			for (int i = numTotalBits - 1; i >= 0; i--)
			{
				this.Range >>= 1;
				if ((v >> i & 1u) == 1u)
				{
					this.Low += this.Range;
				}
				if (this.Range < 16777216u)
				{
					this.Range <<= 8;
					this.ShiftLow();
				}
			}
		}

		// Token: 0x060004A8 RID: 1192 RVA: 0x0001BC28 File Offset: 0x00019E28

	    // Token: 0x060004A9 RID: 1193 RVA: 0x0001BC8F File Offset: 0x00019E8F
		public long GetProcessedSizeAdd()
		{
			return (long)(this._cacheSize + (ulong)this.Stream.Position - (ulong)this.StartPosition + 4UL);
		}

		// Token: 0x040005FF RID: 1535

	    // Token: 0x04000600 RID: 1536
		private Stream Stream;

		// Token: 0x04000601 RID: 1537
		public ulong Low;

		// Token: 0x04000602 RID: 1538
		public uint Range;

		// Token: 0x04000603 RID: 1539
		private uint _cacheSize;

		// Token: 0x04000604 RID: 1540
		private byte _cache;

		// Token: 0x04000605 RID: 1541
		private long StartPosition;
	}
}
