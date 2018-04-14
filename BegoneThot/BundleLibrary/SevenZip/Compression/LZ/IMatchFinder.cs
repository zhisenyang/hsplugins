namespace SevenZip.Compression.LZ
{
	// Token: 0x020000EC RID: 236
	internal interface IMatchFinder : IInWindowStream
	{
		// Token: 0x060004D2 RID: 1234
		void Create(uint historySize, uint keepAddBufferBefore, uint matchMaxLen, uint keepAddBufferAfter);

		// Token: 0x060004D3 RID: 1235
		uint GetMatches(uint[] distances);

		// Token: 0x060004D4 RID: 1236
		void Skip(uint num);
	}
}
