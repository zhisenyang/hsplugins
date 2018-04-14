using System;
using System.IO;

namespace SevenZip.Compression.LZ
{
	// Token: 0x020000EE RID: 238
	public class InWindow
	{
		// Token: 0x060004E4 RID: 1252 RVA: 0x0001CD8C File Offset: 0x0001AF8C
		public void MoveBlock()
		{
			uint offset = this._bufferOffset + this._pos - this._keepSizeBefore;
			if (offset > 0u)
			{
				offset -= 1u;
			}
			uint numBytes = this._bufferOffset + this._streamPos - offset;
			for (uint i = 0u; i < numBytes; i += 1u)
			{
				this._bufferBase[(int)i] = this._bufferBase[(int)(offset + i)];
			}
			this._bufferOffset -= offset;
		}

		// Token: 0x060004E5 RID: 1253 RVA: 0x0001CDF4 File Offset: 0x0001AFF4
		public virtual void ReadBlock()
		{
			if (this._streamEndWasReached)
			{
				return;
			}
			for (;;)
			{
				int size = (int)(0u - this._bufferOffset + this._blockSize - this._streamPos);
				if (size == 0)
				{
					break;
				}
				int numReadBytes = this._stream.Read(this._bufferBase, (int)(this._bufferOffset + this._streamPos), size);
				if (numReadBytes == 0)
				{
					goto Block_3;
				}
				this._streamPos += (uint)numReadBytes;
				if (this._streamPos >= this._pos + this._keepSizeAfter)
				{
					this._posLimit = this._streamPos - this._keepSizeAfter;
				}
			}
			return;
			Block_3:
			this._posLimit = this._streamPos;
			if (this._bufferOffset + this._posLimit > this._pointerToLastSafePosition)
			{
				this._posLimit = this._pointerToLastSafePosition - this._bufferOffset;
			}
			this._streamEndWasReached = true;
		}

		// Token: 0x060004E6 RID: 1254 RVA: 0x0001CEC1 File Offset: 0x0001B0C1
		private void Free()
		{
			this._bufferBase = null;
		}

		// Token: 0x060004E7 RID: 1255 RVA: 0x0001CECC File Offset: 0x0001B0CC
		public void Create(uint keepSizeBefore, uint keepSizeAfter, uint keepSizeReserv)
		{
			this._keepSizeBefore = keepSizeBefore;
			this._keepSizeAfter = keepSizeAfter;
			uint blockSize = keepSizeBefore + keepSizeAfter + keepSizeReserv;
			if (this._bufferBase == null || this._blockSize != blockSize)
			{
				this.Free();
				this._blockSize = blockSize;
				this._bufferBase = new byte[this._blockSize];
			}
			this._pointerToLastSafePosition = this._blockSize - keepSizeAfter;
		}

		// Token: 0x060004E8 RID: 1256 RVA: 0x0001CF2A File Offset: 0x0001B12A
		public void SetStream(Stream stream)
		{
			this._stream = stream;
		}

		// Token: 0x060004E9 RID: 1257 RVA: 0x0001CF33 File Offset: 0x0001B133
		public void ReleaseStream()
		{
			this._stream = null;
		}

		// Token: 0x060004EA RID: 1258 RVA: 0x0001CF3C File Offset: 0x0001B13C
		public void Init()
		{
			this._bufferOffset = 0u;
			this._pos = 0u;
			this._streamPos = 0u;
			this._streamEndWasReached = false;
			this.ReadBlock();
		}

		// Token: 0x060004EB RID: 1259 RVA: 0x0001CF60 File Offset: 0x0001B160
		public void MovePos()
		{
			this._pos += 1u;
			if (this._pos > this._posLimit)
			{
				if (this._bufferOffset + this._pos > this._pointerToLastSafePosition)
				{
					this.MoveBlock();
				}
				this.ReadBlock();
			}
		}

		// Token: 0x060004EC RID: 1260 RVA: 0x0001CF9F File Offset: 0x0001B19F
		public byte GetIndexByte(int index)
		{
			return this._bufferBase[(int)((IntPtr)(unchecked(this._bufferOffset + this._pos + (ulong)index)))];
		}

		// Token: 0x060004ED RID: 1261 RVA: 0x0001CFBC File Offset: 0x0001B1BC
		public uint GetMatchLen(int index, uint distance, uint limit)
		{
			if (this._streamEndWasReached && this._pos + (ulong)index + limit > this._streamPos)
			{
				limit = this._streamPos - (uint)(this._pos + (ulong)index);
			}
			distance += 1u;
			uint pby = this._bufferOffset + this._pos + (uint)index;
			uint i = 0u;
			while (i < limit && this._bufferBase[(int)(pby + i)] == this._bufferBase[(int)(pby + i - distance)])
			{
				i += 1u;
			}
			return i;
		}

		// Token: 0x060004EE RID: 1262 RVA: 0x0001D035 File Offset: 0x0001B235
		public uint GetNumAvailableBytes()
		{
			return this._streamPos - this._pos;
		}

		// Token: 0x060004EF RID: 1263 RVA: 0x0001D044 File Offset: 0x0001B244
		public void ReduceOffsets(int subValue)
		{
			this._bufferOffset += (uint)subValue;
			this._posLimit -= (uint)subValue;
			this._pos -= (uint)subValue;
			this._streamPos -= (uint)subValue;
		}

		// Token: 0x0400062C RID: 1580
		public byte[] _bufferBase;

		// Token: 0x0400062D RID: 1581
		private Stream _stream;

		// Token: 0x0400062E RID: 1582
		private uint _posLimit;

		// Token: 0x0400062F RID: 1583
		private bool _streamEndWasReached;

		// Token: 0x04000630 RID: 1584
		private uint _pointerToLastSafePosition;

		// Token: 0x04000631 RID: 1585
		public uint _bufferOffset;

		// Token: 0x04000632 RID: 1586
		public uint _blockSize;

		// Token: 0x04000633 RID: 1587
		public uint _pos;

		// Token: 0x04000634 RID: 1588
		private uint _keepSizeBefore;

		// Token: 0x04000635 RID: 1589
		private uint _keepSizeAfter;

		// Token: 0x04000636 RID: 1590
		public uint _streamPos;
	}
}
