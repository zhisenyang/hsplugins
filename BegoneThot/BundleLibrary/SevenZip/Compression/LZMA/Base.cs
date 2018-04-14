namespace SevenZip.Compression.LZMA
{
	// Token: 0x020000F0 RID: 240
	internal abstract class Base
	{
		// Token: 0x060004FA RID: 1274 RVA: 0x0001D2E2 File Offset: 0x0001B4E2
		public static uint GetLenToPosState(uint len)
		{
			len -= 2u;
			if (len < 4u)
			{
				return len;
			}
			return 3u;
		}

		// Token: 0x0200011E RID: 286
		public struct State
		{
			// Token: 0x0600059D RID: 1437 RVA: 0x0002116B File Offset: 0x0001F36B
			public void Init()
			{
				this.Index = 0u;
			}

			// Token: 0x0600059E RID: 1438 RVA: 0x00021174 File Offset: 0x0001F374
			public void UpdateChar()
			{
				if (this.Index < 4u)
				{
					this.Index = 0u;
					return;
				}
				if (this.Index < 10u)
				{
					this.Index -= 3u;
					return;
				}
				this.Index -= 6u;
			}

			// Token: 0x0600059F RID: 1439 RVA: 0x000211AE File Offset: 0x0001F3AE
			public void UpdateMatch()
			{
				this.Index = ((this.Index < 7u) ? 7u : 10u);
			}

			// Token: 0x060005A0 RID: 1440 RVA: 0x000211C4 File Offset: 0x0001F3C4
			public void UpdateRep()
			{
				this.Index = ((this.Index < 7u) ? 8u : 11u);
			}

			// Token: 0x060005A1 RID: 1441 RVA: 0x000211DA File Offset: 0x0001F3DA
			public void UpdateShortRep()
			{
				this.Index = ((this.Index < 7u) ? 9u : 11u);
			}

			// Token: 0x060005A2 RID: 1442 RVA: 0x000211F1 File Offset: 0x0001F3F1
			public bool IsCharState()
			{
				return this.Index < 7u;
			}

			// Token: 0x04000725 RID: 1829
			public uint Index;
		}
	}
}
