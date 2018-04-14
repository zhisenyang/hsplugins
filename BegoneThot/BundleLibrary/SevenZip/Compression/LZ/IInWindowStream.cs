using System.IO;

namespace SevenZip.Compression.LZ
{
	// Token: 0x020000EB RID: 235
	internal interface IInWindowStream
	{
		// Token: 0x060004CC RID: 1228
		void SetStream(Stream inStream);

		// Token: 0x060004CD RID: 1229
		void Init();

		// Token: 0x060004CE RID: 1230
		void ReleaseStream();

		// Token: 0x060004CF RID: 1231
		byte GetIndexByte(int index);

		// Token: 0x060004D0 RID: 1232
		uint GetMatchLen(int index, uint distance, uint limit);

		// Token: 0x060004D1 RID: 1233
		uint GetNumAvailableBytes();
	}
}
