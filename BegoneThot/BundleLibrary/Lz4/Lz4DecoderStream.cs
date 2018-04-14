using System;
using System.IO;

namespace Lz4
{
	// Token: 0x02000005 RID: 5
	public class Lz4DecoderStream : Stream
	{
		// Token: 0x06000002 RID: 2 RVA: 0x000021F6 File Offset: 0x000003F6
		public Lz4DecoderStream(Stream input, long inputLength = 9223372036854775807L)
		{
			this.Reset(input, inputLength);
		}

		// Token: 0x06000003 RID: 3 RVA: 0x00002218 File Offset: 0x00000418
		public void Reset(Stream input, long inputLength = 9223372036854775807L)
		{
			this.inputLength = inputLength;
			this.input = input;
			this.phase = DecodePhase.ReadToken;
			this.decodeBufferPos = 0;
			this.litLen = 0;
			this.matLen = 0;
			this.matDst = 0;
			this.inBufPos = 65536;
			this.inBufEnd = 65536;
		}

		// Token: 0x06000004 RID: 4 RVA: 0x0000226C File Offset: 0x0000046C
		public override void Close()
		{
			this.input = null;
		}

		// Token: 0x06000005 RID: 5 RVA: 0x00002278 File Offset: 0x00000478
		public override int Read(byte[] buffer, int offset, int count)
		{
			if (buffer == null)
			{
				throw new ArgumentNullException(nameof(buffer));
			}
			if (offset < 0 || count < 0 || buffer.Length - count < offset)
			{
				throw new ArgumentOutOfRangeException();
			}
			if (this.input == null)
			{
				throw new InvalidOperationException();
			}
			int nToRead = count;
			byte[] decBuf = this.decodeBuffer;
            switch (this.phase)
			{
			default:
				goto IL_62;
			case DecodePhase.ReadExLiteralLength:
				break;
			case DecodePhase.CopyLiteral:
				goto IL_135;
			case DecodePhase.ReadOffset:
				goto IL_1D4;
			case DecodePhase.ReadExMatchLength:
				goto IL_243;
			case DecodePhase.CopyMatch:
				goto IL_29A;
			}

			int num;
			int exLitLen;
		    IL_DE:
			do
            {
				if (this.inBufPos < this.inBufEnd)
				{
					byte[] array = decBuf;
					num = this.inBufPos;
					this.inBufPos = num + 1;
					exLitLen = array[num];
				}
				else
				{
					exLitLen = this.ReadByteCore();
					if (exLitLen == -1)
					{
						goto IL_367;
					}
				}
				this.litLen += exLitLen;
			}
			while (exLitLen == 255);
			this.phase = DecodePhase.CopyLiteral;
			int nRead;
		    IL_135:
			do
            {
				int nReadLit = (this.litLen < nToRead) ? this.litLen : nToRead;
				if (nReadLit == 0)
				{
					break;
				}
				if (this.inBufPos + nReadLit <= this.inBufEnd)
				{
					int ofs = offset;
					int c = nReadLit;
					while (c-- != 0)
					{
						int num2 = ofs++;
						byte[] array2 = decBuf;
						num = this.inBufPos;
						this.inBufPos = num + 1;
						buffer[num2] = array2[num];
					}
					nRead = nReadLit;
				}
				else
				{
					nRead = this.ReadCore(buffer, offset, nReadLit);
					if (nRead == 0)
					{
						goto IL_367;
					}
				}
				offset += nRead;
				nToRead -= nRead;
				this.litLen -= nRead;
			}
			while (this.litLen != 0);
			if (nToRead != 0)
			{
				this.phase = DecodePhase.ReadOffset;
				goto IL_1D4;
			}
			goto IL_367;
		    IL_243:
		    int exMatLen;
			do
            {
				if (this.inBufPos < this.inBufEnd)
				{
					byte[] array3 = decBuf;
					num = this.inBufPos;
					this.inBufPos = num + 1;
					exMatLen = array3[num];
				}
				else
				{
					exMatLen = this.ReadByteCore();
					if (exMatLen == -1)
					{
						goto IL_367;
					}
				}
				this.matLen += exMatLen;
			}
			while (exMatLen == 255);
			this.phase = DecodePhase.CopyMatch;
			goto IL_29A;
			IL_62:
			int tok;
			if (this.inBufPos < this.inBufEnd)
			{
				byte[] array4 = decBuf;
				num = this.inBufPos;
				this.inBufPos = num + 1;
				tok = array4[num];
			}
			else
			{
				tok = this.ReadByteCore();
				if (tok == -1)
				{
					goto IL_367;
				}
			}
			this.litLen = tok >> 4;
			this.matLen = (tok & 15) + 4;
			num = this.litLen;
			if (num != 0)
			{
				if (num != 15)
				{
					this.phase = DecodePhase.CopyLiteral;
					goto IL_135;
				}
				this.phase = DecodePhase.ReadExLiteralLength;
				goto IL_DE;
			}
		    this.phase = DecodePhase.ReadOffset;
		    IL_1D4:
			if (this.inBufPos + 1 < this.inBufEnd)
			{
				this.matDst = (decBuf[this.inBufPos + 1] << 8 | decBuf[this.inBufPos]);
				this.inBufPos += 2;
			}
			else
			{
				this.matDst = this.ReadOffsetCore();
				if (this.matDst == -1)
				{
					goto IL_367;
				}
			}
			if (this.matLen == 19)
			{
				this.phase = DecodePhase.ReadExMatchLength;
				goto IL_243;
			}
			this.phase = DecodePhase.CopyMatch;
			IL_29A:
			int nCpyMat = (this.matLen < nToRead) ? this.matLen : nToRead;
			if (nCpyMat != 0)
			{
				nRead = count - nToRead;
				int bufDst = this.matDst - nRead;
				if (bufDst > 0)
				{
					int bufSrc = this.decodeBufferPos - bufDst;
					if (bufSrc < 0)
					{
						bufSrc += 65536;
					}
					int c2 = (bufDst < nCpyMat) ? bufDst : nCpyMat;
					while (c2-- != 0)
					{
						buffer[offset++] = decBuf[bufSrc++ & 65535];
					}
				}
				else
				{
					bufDst = 0;
				}
				int sOfs = offset - this.matDst;
				for (int i = bufDst; i < nCpyMat; i++)
				{
					buffer[offset++] = buffer[sOfs++];
				}
				nToRead -= nCpyMat;
				this.matLen -= nCpyMat;
			}
			if (nToRead != 0)
			{
				this.phase = DecodePhase.ReadToken;
				goto IL_62;
			}
			IL_367:
			nRead = count - nToRead;
			int nToBuf = (nRead < 65536) ? nRead : 65536;
			int repPos = offset - nToBuf;
			if (nToBuf == 65536)
			{
				Buffer.BlockCopy(buffer, repPos, decBuf, 0, 65536);
				this.decodeBufferPos = 0;
			}
			else
			{
				int decPos = this.decodeBufferPos;
				while (nToBuf-- != 0)
				{
					decBuf[decPos++ & 65535] = buffer[repPos++];
				}
				this.decodeBufferPos = (decPos & 65535);
			}
			return nRead;
		}

		// Token: 0x06000006 RID: 6 RVA: 0x00002664 File Offset: 0x00000864
		private int ReadByteCore()
		{
			byte[] buf = this.decodeBuffer;
			if (this.inBufPos == this.inBufEnd)
			{
				int nRead = this.input.Read(buf, 65536, (128L < this.inputLength) ? 128 : ((int)this.inputLength));
				if (nRead == 0)
				{
					return -1;
				}
				this.inputLength -= nRead;
				this.inBufPos = 65536;
				this.inBufEnd = 65536 + nRead;
			}
			byte[] array = buf;
			int num = this.inBufPos;
			this.inBufPos = num + 1;
			return array[num];
		}

		// Token: 0x06000007 RID: 7 RVA: 0x000026F4 File Offset: 0x000008F4
		private int ReadOffsetCore()
		{
			byte[] buf = this.decodeBuffer;
			if (this.inBufPos == this.inBufEnd)
			{
				int nRead = this.input.Read(buf, 65536, (128L < this.inputLength) ? 128 : ((int)this.inputLength));
				if (nRead == 0)
				{
					return -1;
				}
				this.inputLength -= nRead;
				this.inBufPos = 65536;
				this.inBufEnd = 65536 + nRead;
			}
			if (this.inBufEnd - this.inBufPos == 1)
			{
				buf[65536] = buf[this.inBufPos];
				int nRead2 = this.input.Read(buf, 65537, (127L < this.inputLength) ? 127 : ((int)this.inputLength));
				if (nRead2 == 0)
				{
					this.inBufPos = 65536;
					this.inBufEnd = 65537;
					return -1;
				}
				this.inputLength -= nRead2;
				this.inBufPos = 65536;
				this.inBufEnd = 65536 + nRead2 + 1;
			}
			int result = buf[this.inBufPos + 1] << 8 | buf[this.inBufPos];
			this.inBufPos += 2;
			return result;
		}

		// Token: 0x06000008 RID: 8 RVA: 0x00002820 File Offset: 0x00000A20
		private int ReadCore(byte[] buffer, int offset, int count)
		{
			int nToRead = count;
			byte[] buf = this.decodeBuffer;
			int inBufLen = this.inBufEnd - this.inBufPos;
			int fromBuf = (nToRead < inBufLen) ? nToRead : inBufLen;
			if (fromBuf != 0)
			{
				int bufPos = this.inBufPos;
				int c = fromBuf;
				while (c-- != 0)
				{
					buffer[offset++] = buf[bufPos++];
				}
				this.inBufPos = bufPos;
				nToRead -= fromBuf;
			}
			if (nToRead != 0)
			{
				int nRead;
				if (nToRead >= 128)
				{
					nRead = this.input.Read(buffer, offset, ((long)nToRead < this.inputLength) ? nToRead : ((int)this.inputLength));
					nToRead -= nRead;
				}
				else
				{
					nRead = this.input.Read(buf, 65536, (128L < this.inputLength) ? 128 : ((int)this.inputLength));
					this.inBufPos = 65536;
					this.inBufEnd = 65536 + nRead;
					fromBuf = ((nToRead < nRead) ? nToRead : nRead);
					int bufPos2 = this.inBufPos;
					int c2 = fromBuf;
					while (c2-- != 0)
					{
						buffer[offset++] = buf[bufPos2++];
					}
					this.inBufPos = bufPos2;
					nToRead -= fromBuf;
				}
				this.inputLength -= nRead;
			}
			return count - nToRead;
		}

		// Token: 0x17000001 RID: 1
		// (get) Token: 0x06000009 RID: 9 RVA: 0x00002957 File Offset: 0x00000B57
		public override bool CanRead
		{
			get
			{
				return true;
			}
		}

		// Token: 0x17000002 RID: 2
		// (get) Token: 0x0600000A RID: 10 RVA: 0x0000295A File Offset: 0x00000B5A
		public override bool CanSeek
		{
			get
			{
				return false;
			}
		}

		// Token: 0x17000003 RID: 3
		// (get) Token: 0x0600000B RID: 11 RVA: 0x0000295A File Offset: 0x00000B5A
		public override bool CanWrite
		{
			get
			{
				return false;
			}
		}

		// Token: 0x0600000C RID: 12 RVA: 0x0000295D File Offset: 0x00000B5D
		public override void Flush()
		{
		}

		// Token: 0x17000004 RID: 4
		// (get) Token: 0x0600000D RID: 13 RVA: 0x0000295F File Offset: 0x00000B5F
		public override long Length
		{
			get
			{
				throw new NotSupportedException();
			}
		}

		// Token: 0x17000005 RID: 5
		// (get) Token: 0x0600000E RID: 14 RVA: 0x0000295F File Offset: 0x00000B5F
		// (set) Token: 0x0600000F RID: 15 RVA: 0x0000295F File Offset: 0x00000B5F
		public override long Position
		{
			get
			{
				throw new NotSupportedException();
			}
			set
			{
				throw new NotSupportedException();
			}
		}

		// Token: 0x06000010 RID: 16 RVA: 0x0000295F File Offset: 0x00000B5F
		public override long Seek(long offset, SeekOrigin origin)
		{
			throw new NotSupportedException();
		}

		// Token: 0x06000011 RID: 17 RVA: 0x0000295F File Offset: 0x00000B5F
		public override void SetLength(long value)
		{
			throw new NotSupportedException();
		}

		// Token: 0x06000012 RID: 18 RVA: 0x0000295F File Offset: 0x00000B5F
		public override void Write(byte[] buffer, int offset, int count)
		{
			throw new NotSupportedException();
		}

		// Token: 0x04000093 RID: 147
		private long inputLength;

		// Token: 0x04000094 RID: 148
		private Stream input;

		// Token: 0x04000095 RID: 149

	    // Token: 0x04000096 RID: 150

	    // Token: 0x04000097 RID: 151

	    // Token: 0x04000098 RID: 152
		private readonly byte[] decodeBuffer = new byte[65664];

		// Token: 0x04000099 RID: 153
		private int decodeBufferPos;

		// Token: 0x0400009A RID: 154
		private int inBufPos;

		// Token: 0x0400009B RID: 155
		private int inBufEnd;

		// Token: 0x0400009C RID: 156
		private DecodePhase phase;

		// Token: 0x0400009D RID: 157
		private int litLen;

		// Token: 0x0400009E RID: 158
		private int matLen;

		// Token: 0x0400009F RID: 159
		private int matDst;

		// Token: 0x020000FD RID: 253
		private enum DecodePhase
		{
			// Token: 0x040006D3 RID: 1747
			ReadToken,
			// Token: 0x040006D4 RID: 1748
			ReadExLiteralLength,
			// Token: 0x040006D5 RID: 1749
			CopyLiteral,
			// Token: 0x040006D6 RID: 1750
			ReadOffset,
			// Token: 0x040006D7 RID: 1751
			ReadExMatchLength,
			// Token: 0x040006D8 RID: 1752
			CopyMatch
		}
	}
}
