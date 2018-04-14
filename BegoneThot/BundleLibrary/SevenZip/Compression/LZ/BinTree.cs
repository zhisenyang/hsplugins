using System;
using System.IO;

namespace SevenZip.Compression.LZ
{
	// Token: 0x020000ED RID: 237
	public class BinTree : InWindow, IMatchFinder, IInWindowStream
	{
		// Token: 0x060004D5 RID: 1237 RVA: 0x0001C458 File Offset: 0x0001A658
		public void SetType(int numHashBytes)
		{
			this.HASH_ARRAY = (numHashBytes > 2);
			if (this.HASH_ARRAY)
			{
				this.kNumHashDirectBytes = 0u;
				this.kMinMatchCheck = 4u;
				this.kFixHashSize = 66560u;
				return;
			}
			this.kNumHashDirectBytes = 2u;
			this.kMinMatchCheck = 3u;
			this.kFixHashSize = 0u;
		}

		// Token: 0x060004D6 RID: 1238 RVA: 0x0001C4A6 File Offset: 0x0001A6A6
		public new void SetStream(Stream stream)
		{
			base.SetStream(stream);
		}

		// Token: 0x060004D7 RID: 1239 RVA: 0x0001C4AF File Offset: 0x0001A6AF
		public new void ReleaseStream()
		{
			base.ReleaseStream();
		}

		// Token: 0x060004D8 RID: 1240 RVA: 0x0001C4B8 File Offset: 0x0001A6B8
		public new void Init()
		{
			base.Init();
			for (uint i = 0u; i < this._hashSizeSum; i += 1u)
			{
				this._hash[(int)i] = 0u;
			}
			this._cyclicBufferPos = 0u;
			this.ReduceOffsets(-1);
		}

		// Token: 0x060004D9 RID: 1241 RVA: 0x0001C4F4 File Offset: 0x0001A6F4
		public new void MovePos()
		{
			uint num = this._cyclicBufferPos + 1u;
			this._cyclicBufferPos = num;
			if (num >= this._cyclicBufferSize)
			{
				this._cyclicBufferPos = 0u;
			}
			base.MovePos();
			if (this._pos == 2147483647u)
			{
				this.Normalize();
			}
		}

		// Token: 0x060004DA RID: 1242 RVA: 0x0001C53A File Offset: 0x0001A73A
		public new byte GetIndexByte(int index)
		{
			return base.GetIndexByte(index);
		}

		// Token: 0x060004DB RID: 1243 RVA: 0x0001C543 File Offset: 0x0001A743
		public new uint GetMatchLen(int index, uint distance, uint limit)
		{
			return base.GetMatchLen(index, distance, limit);
		}

		// Token: 0x060004DC RID: 1244 RVA: 0x0001C54E File Offset: 0x0001A74E
		public new uint GetNumAvailableBytes()
		{
			return base.GetNumAvailableBytes();
		}

		// Token: 0x060004DD RID: 1245 RVA: 0x0001C558 File Offset: 0x0001A758
		public void Create(uint historySize, uint keepAddBufferBefore, uint matchMaxLen, uint keepAddBufferAfter)
		{
			if (historySize > 2147483391u)
			{
				throw new Exception();
			}
			this._cutValue = 16u + (matchMaxLen >> 1);
			uint windowReservSize = (historySize + keepAddBufferBefore + matchMaxLen + keepAddBufferAfter) / 2u + 256u;
			base.Create(historySize + keepAddBufferBefore, matchMaxLen + keepAddBufferAfter, windowReservSize);
			this._matchMaxLen = matchMaxLen;
			uint cyclicBufferSize = historySize + 1u;
			if (this._cyclicBufferSize != cyclicBufferSize)
			{
				this._son = new uint[(this._cyclicBufferSize = cyclicBufferSize) * 2u];
			}
			uint hs = 65536u;
			if (this.HASH_ARRAY)
			{
				hs = historySize - 1u;
				hs |= hs >> 1;
				hs |= hs >> 2;
				hs |= hs >> 4;
				hs |= hs >> 8;
				hs >>= 1;
				hs |= 65535u;
				if (hs > 16777216u)
				{
					hs >>= 1;
				}
				this._hashMask = hs;
				hs += 1u;
				hs += this.kFixHashSize;
			}
			if (hs != this._hashSizeSum)
			{
				this._hash = new uint[this._hashSizeSum = hs];
			}
		}

		// Token: 0x060004DE RID: 1246 RVA: 0x0001C640 File Offset: 0x0001A840
		public uint GetMatches(uint[] distances)
		{
			uint lenLimit;
			if (this._pos + this._matchMaxLen <= this._streamPos)
			{
				lenLimit = this._matchMaxLen;
			}
			else
			{
				lenLimit = this._streamPos - this._pos;
				if (lenLimit < this.kMinMatchCheck)
				{
					this.MovePos();
					return 0u;
				}
			}
			uint offset = 0u;
			uint matchMinPos = (this._pos > this._cyclicBufferSize) ? (this._pos - this._cyclicBufferSize) : 0u;
			uint cur = this._bufferOffset + this._pos;
			uint maxLen = 1u;
			uint hash2Value = 0u;
			uint hash3Value = 0u;
			uint hashValue;
			if (this.HASH_ARRAY)
			{
				uint num = CRC.Table[this._bufferBase[(int)cur]] ^ this._bufferBase[(int)(cur + 1u)];
				hash2Value = (num & 1023u);
				uint num2 = num ^ (uint)this._bufferBase[(int)(cur + 2u)] << 8;
				hash3Value = (num2 & 65535u);
				hashValue = ((num2 ^ CRC.Table[this._bufferBase[(int)(cur + 3u)]] << 5) & this._hashMask);
			}
			else
			{
				hashValue = (uint)(this._bufferBase[(int)cur] ^ this._bufferBase[(int)(cur + 1u)] << 8);
			}
			uint curMatch = this._hash[(int)(this.kFixHashSize + hashValue)];
			if (this.HASH_ARRAY)
			{
				uint curMatch2 = this._hash[(int)hash2Value];
				uint curMatch3 = this._hash[(int)(1024u + hash3Value)];
				this._hash[(int)hash2Value] = this._pos;
				this._hash[(int)(1024u + hash3Value)] = this._pos;
				if (curMatch2 > matchMinPos && this._bufferBase[(int)(this._bufferOffset + curMatch2)] == this._bufferBase[(int)cur])
				{
					maxLen = (distances[(int)offset++] = 2u);
					distances[(int)offset++] = this._pos - curMatch2 - 1u;
				}
				if (curMatch3 > matchMinPos && this._bufferBase[(int)(this._bufferOffset + curMatch3)] == this._bufferBase[(int)cur])
				{
					if (curMatch3 == curMatch2)
					{
						offset -= 2u;
					}
					maxLen = (distances[(int)offset++] = 3u);
					distances[(int)offset++] = this._pos - curMatch3 - 1u;
					curMatch2 = curMatch3;
				}
				if (offset != 0u && curMatch2 == curMatch)
				{
					offset -= 2u;
					maxLen = 1u;
				}
			}
			this._hash[(int)(this.kFixHashSize + hashValue)] = this._pos;
			uint ptr0 = (this._cyclicBufferPos << 1) + 1u;
			uint ptr = this._cyclicBufferPos << 1;
			uint len2;
			uint len = len2 = this.kNumHashDirectBytes;
			if (this.kNumHashDirectBytes != 0u && curMatch > matchMinPos && this._bufferBase[(int)(this._bufferOffset + curMatch + this.kNumHashDirectBytes)] != this._bufferBase[(int)(cur + this.kNumHashDirectBytes)])
			{
				maxLen = (distances[(int)offset++] = this.kNumHashDirectBytes);
				distances[(int)offset++] = this._pos - curMatch - 1u;
			}
			uint count = this._cutValue;
			while (curMatch > matchMinPos && count-- != 0u)
			{
				uint delta = this._pos - curMatch;
				uint cyclicPos = ((delta <= this._cyclicBufferPos) ? (this._cyclicBufferPos - delta) : (this._cyclicBufferPos - delta + this._cyclicBufferSize)) << 1;
				uint pby = this._bufferOffset + curMatch;
				uint len3 = Math.Min(len2, len);
				if (this._bufferBase[(int)(pby + len3)] == this._bufferBase[(int)(cur + len3)])
				{
					while ((len3 += 1u) != lenLimit && this._bufferBase[(int)(pby + len3)] == this._bufferBase[(int)(cur + len3)])
					{
					}
					if (maxLen < len3)
					{
						maxLen = (distances[(int)offset++] = len3);
						distances[(int)offset++] = delta - 1u;
						if (len3 == lenLimit)
						{
							this._son[(int)ptr] = this._son[(int)cyclicPos];
							this._son[(int)ptr0] = this._son[(int)(cyclicPos + 1u)];
							this.MovePos();
							return offset;
						}
					}
				}
				if (this._bufferBase[(int)(pby + len3)] < this._bufferBase[(int)(cur + len3)])
				{
					this._son[(int)ptr] = curMatch;
					ptr = cyclicPos + 1u;
					curMatch = this._son[(int)ptr];
					len = len3;
				}
				else
				{
					this._son[(int)ptr0] = curMatch;
					ptr0 = cyclicPos;
					curMatch = this._son[(int)ptr0];
					len2 = len3;
				}
			}
			this._son[(int)ptr0] = (this._son[(int)ptr] = 0u);
		    this.MovePos();
		    return offset;
		}

        // Token: 0x060004DF RID: 1247 RVA: 0x0001CA28 File Offset: 0x0001AC28
        public void Skip(uint num)
		{
			for (;;)
			{
				uint lenLimit;
				if (this._pos + this._matchMaxLen <= this._streamPos)
				{
					lenLimit = this._matchMaxLen;
					goto IL_40;
				}
				lenLimit = this._streamPos - this._pos;
				if (lenLimit >= this.kMinMatchCheck)
				{
					goto IL_40;
				}
				this.MovePos();
				IL_29A:
				if ((num -= 1u) == 0u)
				{
					break;
				}
				continue;
				IL_40:
				uint matchMinPos = (this._pos > this._cyclicBufferSize) ? (this._pos - this._cyclicBufferSize) : 0u;
				uint cur = this._bufferOffset + this._pos;
				uint hashValue;
				if (this.HASH_ARRAY)
				{
					uint num2 = CRC.Table[this._bufferBase[(int)cur]] ^ this._bufferBase[(int)(cur + 1u)];
					uint hash2Value = num2 & 1023u;
					this._hash[(int)hash2Value] = this._pos;
					uint num3 = num2 ^ (uint)this._bufferBase[(int)(cur + 2u)] << 8;
					uint hash3Value = num3 & 65535u;
					this._hash[(int)(1024u + hash3Value)] = this._pos;
					hashValue = ((num3 ^ CRC.Table[this._bufferBase[(int)(cur + 3u)]] << 5) & this._hashMask);
				}
				else
				{
					hashValue = (uint)(this._bufferBase[(int)cur] ^ this._bufferBase[(int)(cur + 1u)] << 8);
				}
				uint curMatch = this._hash[(int)(this.kFixHashSize + hashValue)];
				this._hash[(int)(this.kFixHashSize + hashValue)] = this._pos;
				uint ptr0 = (this._cyclicBufferPos << 1) + 1u;
				uint ptr = this._cyclicBufferPos << 1;
				uint len2;
				uint len = len2 = this.kNumHashDirectBytes;
				uint count = this._cutValue;
				while (curMatch > matchMinPos && count-- != 0u)
				{
					uint delta = this._pos - curMatch;
					uint cyclicPos = ((delta <= this._cyclicBufferPos) ? (this._cyclicBufferPos - delta) : (this._cyclicBufferPos - delta + this._cyclicBufferSize)) << 1;
					uint pby = this._bufferOffset + curMatch;
					uint len3 = Math.Min(len2, len);
					if (this._bufferBase[(int)(pby + len3)] == this._bufferBase[(int)(cur + len3)])
					{
						while ((len3 += 1u) != lenLimit && this._bufferBase[(int)(pby + len3)] == this._bufferBase[(int)(cur + len3)])
						{
						}
						if (len3 == lenLimit)
						{
							this._son[(int)ptr] = this._son[(int)cyclicPos];
							this._son[(int)ptr0] = this._son[(int)(cyclicPos + 1u)];
							this.MovePos();
							goto IL_29A;
						}
					}
					if (this._bufferBase[(int)(pby + len3)] < this._bufferBase[(int)(cur + len3)])
					{
						this._son[(int)ptr] = curMatch;
						ptr = cyclicPos + 1u;
						curMatch = this._son[(int)ptr];
						len = len3;
					}
					else
					{
						this._son[(int)ptr0] = curMatch;
						ptr0 = cyclicPos;
						curMatch = this._son[(int)ptr0];
						len2 = len3;
					}
				}
				this._son[(int)ptr0] = (this._son[(int)ptr] = 0u);
			    this.MovePos();
			    goto IL_29A;
			}
        }

		// Token: 0x060004E0 RID: 1248 RVA: 0x0001CCDC File Offset: 0x0001AEDC
		private void NormalizeLinks(uint[] items, uint numItems, uint subValue)
		{
			for (uint i = 0u; i < numItems; i += 1u)
			{
				uint value = items[(int)i];
				if (value <= subValue)
				{
					value = 0u;
				}
				else
				{
					value -= subValue;
				}
				items[(int)i] = value;
			}
		}

		// Token: 0x060004E1 RID: 1249 RVA: 0x0001CD0C File Offset: 0x0001AF0C
		private void Normalize()
		{
			uint subValue = this._pos - this._cyclicBufferSize;
			this.NormalizeLinks(this._son, this._cyclicBufferSize * 2u, subValue);
			this.NormalizeLinks(this._hash, this._hashSizeSum, subValue);
			this.ReduceOffsets((int)subValue);
		}

		// Token: 0x060004E2 RID: 1250 RVA: 0x0001CD56 File Offset: 0x0001AF56

	    // Token: 0x04000619 RID: 1561
		private uint _cyclicBufferPos;

		// Token: 0x0400061A RID: 1562
		private uint _cyclicBufferSize;

		// Token: 0x0400061B RID: 1563
		private uint _matchMaxLen;

		// Token: 0x0400061C RID: 1564
		private uint[] _son;

		// Token: 0x0400061D RID: 1565
		private uint[] _hash;

		// Token: 0x0400061E RID: 1566
		private uint _cutValue = 255u;

		// Token: 0x0400061F RID: 1567
		private uint _hashMask;

		// Token: 0x04000620 RID: 1568
		private uint _hashSizeSum;

		// Token: 0x04000621 RID: 1569
		private bool HASH_ARRAY = true;

		// Token: 0x04000629 RID: 1577
		private uint kNumHashDirectBytes;

		// Token: 0x0400062A RID: 1578
		private uint kMinMatchCheck = 4u;

		// Token: 0x0400062B RID: 1579
		private uint kFixHashSize = 66560u;
	}
}
