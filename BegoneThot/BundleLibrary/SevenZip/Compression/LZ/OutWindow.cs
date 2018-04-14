using System.IO;

namespace SevenZip.Compression.LZ
{
	// Token: 0x020000EF RID: 239
	public class OutWindow
	{
		// Token: 0x060004F1 RID: 1265 RVA: 0x0001D07E File Offset: 0x0001B27E
		public void Create(uint windowSize)
		{
			if (this._windowSize != windowSize)
			{
				this._buffer = new byte[windowSize];
			}
			this._windowSize = windowSize;
			this._pos = 0u;
			this._streamPos = 0u;
		}

		// Token: 0x060004F2 RID: 1266 RVA: 0x0001D0AA File Offset: 0x0001B2AA
		public void Init(Stream stream, bool solid)
		{
			this.ReleaseStream();
			this._stream = stream;
			if (!solid)
			{
				this._streamPos = 0u;
				this._pos = 0u;
				this.TrainSize = 0u;
			}
		}

		// Token: 0x060004F3 RID: 1267 RVA: 0x0001D0D4 File Offset: 0x0001B2D4
		public bool Train(Stream stream)
		{
			long len = stream.Length;
			uint size = (len < (long)((ulong)this._windowSize)) ? ((uint)len) : this._windowSize;
			this.TrainSize = size;
			stream.Position = len - size;
			this._streamPos = (this._pos = 0u);
			while (size > 0u)
			{
				uint curSize = this._windowSize - this._pos;
				if (size < curSize)
				{
					curSize = size;
				}
				int numReadBytes = stream.Read(this._buffer, (int)this._pos, (int)curSize);
				if (numReadBytes == 0)
				{
					return false;
				}
				size -= (uint)numReadBytes;
				this._pos += (uint)numReadBytes;
				this._streamPos += (uint)numReadBytes;
				if (this._pos == this._windowSize)
				{
					this._streamPos = (this._pos = 0u);
				}
			}
			return true;
		}

		// Token: 0x060004F4 RID: 1268 RVA: 0x0001D195 File Offset: 0x0001B395
		public void ReleaseStream()
		{
			this.Flush();
			this._stream = null;
		}

		// Token: 0x060004F5 RID: 1269 RVA: 0x0001D1A4 File Offset: 0x0001B3A4
		public void Flush()
		{
			uint size = this._pos - this._streamPos;
			if (size == 0u)
			{
				return;
			}
			this._stream.Write(this._buffer, (int)this._streamPos, (int)size);
			if (this._pos >= this._windowSize)
			{
				this._pos = 0u;
			}
			this._streamPos = this._pos;
		}

		// Token: 0x060004F6 RID: 1270 RVA: 0x0001D1FC File Offset: 0x0001B3FC
		public void CopyBlock(uint distance, uint len)
		{
			uint pos = this._pos - distance - 1u;
			if (pos >= this._windowSize)
			{
				pos += this._windowSize;
			}
			while (len > 0u)
			{
				if (pos >= this._windowSize)
				{
					pos = 0u;
				}
				byte[] buffer = this._buffer;
				uint pos2 = this._pos;
				this._pos = pos2 + 1u;
				buffer[(int)pos2] = this._buffer[(int)pos++];
				if (this._pos >= this._windowSize)
				{
					this.Flush();
				}
				len -= 1u;
			}
		}

		// Token: 0x060004F7 RID: 1271 RVA: 0x0001D274 File Offset: 0x0001B474
		public void PutByte(byte b)
		{
			byte[] buffer = this._buffer;
			uint pos = this._pos;
			this._pos = pos + 1u;
			buffer[(int)pos] = b;
			if (this._pos >= this._windowSize)
			{
				this.Flush();
			}
		}

		// Token: 0x060004F8 RID: 1272 RVA: 0x0001D2B0 File Offset: 0x0001B4B0
		public byte GetByte(uint distance)
		{
			uint pos = this._pos - distance - 1u;
			if (pos >= this._windowSize)
			{
				pos += this._windowSize;
			}
			return this._buffer[(int)pos];
		}

		// Token: 0x04000637 RID: 1591
		private byte[] _buffer;

		// Token: 0x04000638 RID: 1592
		private uint _pos;

		// Token: 0x04000639 RID: 1593
		private uint _windowSize;

		// Token: 0x0400063A RID: 1594
		private uint _streamPos;

		// Token: 0x0400063B RID: 1595
		private Stream _stream;

		// Token: 0x0400063C RID: 1596
		public uint TrainSize;
	}
}
