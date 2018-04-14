using System;
using System.IO;
using SevenZip.Compression.LZ;
using SevenZip.Compression.RangeCoder;

namespace SevenZip.Compression.LZMA
{
	// Token: 0x020000F1 RID: 241
	public class Decoder
	{
		// Token: 0x060004FC RID: 1276 RVA: 0x0001D2F0 File Offset: 0x0001B4F0
		public Decoder()
		{
			this.m_DictionarySize = uint.MaxValue;
			int i = 0;
			while (i < 4L)
			{
				this.m_PosSlotDecoder[i] = new BitTreeDecoder(6);
				i++;
			}
		}

		// Token: 0x060004FD RID: 1277 RVA: 0x0001D3DC File Offset: 0x0001B5DC
		private void SetDictionarySize(uint dictionarySize)
		{
			if (this.m_DictionarySize != dictionarySize)
			{
				this.m_DictionarySize = dictionarySize;
				this.m_DictionarySizeCheck = Math.Max(this.m_DictionarySize, 1u);
				uint blockSize = Math.Max(this.m_DictionarySizeCheck, 4096u);
				this.m_OutWindow.Create(blockSize);
			}
		}

		// Token: 0x060004FE RID: 1278 RVA: 0x0001D428 File Offset: 0x0001B628
		private void SetLiteralProperties(int lp, int lc)
		{
			if (lp > 8)
			{
				throw new InvalidParamException();
			}
			if (lc > 8)
			{
				throw new InvalidParamException();
			}
			this.m_LiteralDecoder.Create(lp, lc);
		}

		// Token: 0x060004FF RID: 1279 RVA: 0x0001D44C File Offset: 0x0001B64C
		private void SetPosBitsProperties(int pb)
		{
			if (pb > 4)
			{
				throw new InvalidParamException();
			}
			uint numPosStates = 1u << pb;
			this.m_LenDecoder.Create(numPosStates);
			this.m_RepLenDecoder.Create(numPosStates);
			this.m_PosStateMask = numPosStates - 1u;
		}

		// Token: 0x06000500 RID: 1280 RVA: 0x0001D48C File Offset: 0x0001B68C
		private void Init(Stream inStream, Stream outStream)
		{
			this.m_RangeDecoder.Init(inStream);
			this.m_OutWindow.Init(outStream, this._solid);
			for (uint i = 0u; i < 12u; i += 1u)
			{
				for (uint j = 0u; j <= this.m_PosStateMask; j += 1u)
				{
					uint index = (i << 4) + j;
					this.m_IsMatchDecoders[(int)index].Init();
					this.m_IsRep0LongDecoders[(int)index].Init();
				}
				this.m_IsRepDecoders[(int)i].Init();
				this.m_IsRepG0Decoders[(int)i].Init();
				this.m_IsRepG1Decoders[(int)i].Init();
				this.m_IsRepG2Decoders[(int)i].Init();
			}
			this.m_LiteralDecoder.Init();
			for (uint i = 0u; i < 4u; i += 1u)
			{
				this.m_PosSlotDecoder[(int)i].Init();
			}
			for (uint i = 0u; i < 114u; i += 1u)
			{
				this.m_PosDecoders[(int)i].Init();
			}
			this.m_LenDecoder.Init();
			this.m_RepLenDecoder.Init();
			this.m_PosAlignDecoder.Init();
		}

		// Token: 0x06000501 RID: 1281 RVA: 0x0001D5B0 File Offset: 0x0001B7B0
		public void Code(Stream inStream, Stream outStream, long outSize)
		{
			this.Init(inStream, outStream);
			Base.State state = default(Base.State);
			state.Init();
			uint rep0 = 0u;
			uint rep = 0u;
			uint rep2 = 0u;
			uint rep3 = 0u;
			ulong nowPos64 = 0UL;
			if (nowPos64 < (ulong)outSize)
			{
				if (this.m_IsMatchDecoders[(int)state.Index << 4].Decode(this.m_RangeDecoder) != 0u)
				{
					throw new DataErrorException();
				}
				state.UpdateChar();
				byte b = this.m_LiteralDecoder.DecodeNormal(this.m_RangeDecoder, 0u, 0);
				this.m_OutWindow.PutByte(b);
				nowPos64 += 1UL;
			}
			while (nowPos64 < (ulong)outSize)
			{
				uint posState = (uint)nowPos64 & this.m_PosStateMask;
				if (this.m_IsMatchDecoders[(int)((state.Index << 4) + posState)].Decode(this.m_RangeDecoder) == 0u)
				{
					byte prevByte = this.m_OutWindow.GetByte(0u);
					byte b2;
					if (!state.IsCharState())
					{
						b2 = this.m_LiteralDecoder.DecodeWithMatchByte(this.m_RangeDecoder, (uint)nowPos64, prevByte, this.m_OutWindow.GetByte(rep0));
					}
					else
					{
						b2 = this.m_LiteralDecoder.DecodeNormal(this.m_RangeDecoder, (uint)nowPos64, prevByte);
					}
					this.m_OutWindow.PutByte(b2);
					state.UpdateChar();
					nowPos64 += 1UL;
				}
				else
				{
					uint len;
					if (this.m_IsRepDecoders[(int)state.Index].Decode(this.m_RangeDecoder) == 1u)
					{
						if (this.m_IsRepG0Decoders[(int)state.Index].Decode(this.m_RangeDecoder) == 0u)
						{
							if (this.m_IsRep0LongDecoders[(int)((state.Index << 4) + posState)].Decode(this.m_RangeDecoder) == 0u)
							{
								state.UpdateShortRep();
								this.m_OutWindow.PutByte(this.m_OutWindow.GetByte(rep0));
								nowPos64 += 1UL;
								continue;
							}
						}
						else
						{
							uint distance;
							if (this.m_IsRepG1Decoders[(int)state.Index].Decode(this.m_RangeDecoder) == 0u)
							{
								distance = rep;
							}
							else
							{
								if (this.m_IsRepG2Decoders[(int)state.Index].Decode(this.m_RangeDecoder) == 0u)
								{
									distance = rep2;
								}
								else
								{
									distance = rep3;
									rep3 = rep2;
								}
								rep2 = rep;
							}
							rep = rep0;
							rep0 = distance;
						}
						len = this.m_RepLenDecoder.Decode(this.m_RangeDecoder, posState) + 2u;
						state.UpdateRep();
					}
					else
					{
						rep3 = rep2;
						rep2 = rep;
						rep = rep0;
						len = 2u + this.m_LenDecoder.Decode(this.m_RangeDecoder, posState);
						state.UpdateMatch();
						uint posSlot = this.m_PosSlotDecoder[(int)Base.GetLenToPosState(len)].Decode(this.m_RangeDecoder);
						if (posSlot >= 4u)
						{
							int numDirectBits = (int)((posSlot >> 1) - 1u);
							rep0 = (2u | (posSlot & 1u)) << numDirectBits;
							if (posSlot < 14u)
							{
								rep0 += BitTreeDecoder.ReverseDecode(this.m_PosDecoders, rep0 - posSlot - 1u, this.m_RangeDecoder, numDirectBits);
							}
							else
							{
								rep0 += this.m_RangeDecoder.DecodeDirectBits(numDirectBits - 4) << 4;
								rep0 += this.m_PosAlignDecoder.ReverseDecode(this.m_RangeDecoder);
							}
						}
						else
						{
							rep0 = posSlot;
						}
					}
					if (rep0 >= this.m_OutWindow.TrainSize + nowPos64 || rep0 >= this.m_DictionarySizeCheck)
					{
						if (rep0 != 4294967295u)
						{
							throw new DataErrorException();
						}
						break;
					}
				    this.m_OutWindow.CopyBlock(rep0, len);
				    nowPos64 += len;
				}
			}
			this.m_OutWindow.Flush();
			this.m_OutWindow.ReleaseStream();
			this.m_RangeDecoder.ReleaseStream();
		}

		// Token: 0x06000502 RID: 1282 RVA: 0x0001D908 File Offset: 0x0001BB08
		public void SetDecoderProperties(byte[] properties)
		{
			if (properties.Length < 5)
			{
				throw new InvalidParamException();
			}
			int lc = properties[0] % 9;
			byte b = (byte)(properties[0] / 9);
			int lp = b % 5;
			int pb = b / 5;
			if (pb > 4)
			{
				throw new InvalidParamException();
			}
			uint dictionarySize = 0u;
			for (int i = 0; i < 4; i++)
			{
				dictionarySize += (uint)properties[1 + i] << i * 8;
			}
			this.SetDictionarySize(dictionarySize);
			this.SetLiteralProperties(lp, lc);
			this.SetPosBitsProperties(pb);
		}

		// Token: 0x06000503 RID: 1283 RVA: 0x0001D978 File Offset: 0x0001BB78
		public bool Train(Stream stream)
		{
			this._solid = true;
			return this.m_OutWindow.Train(stream);
		}

		// Token: 0x04000658 RID: 1624
		private readonly OutWindow m_OutWindow = new OutWindow();

		// Token: 0x04000659 RID: 1625
		private readonly RangeCoder.Decoder m_RangeDecoder = new RangeCoder.Decoder();

		// Token: 0x0400065A RID: 1626
		private readonly BitDecoder[] m_IsMatchDecoders = new BitDecoder[192];

		// Token: 0x0400065B RID: 1627
		private readonly BitDecoder[] m_IsRepDecoders = new BitDecoder[12];

		// Token: 0x0400065C RID: 1628
		private readonly BitDecoder[] m_IsRepG0Decoders = new BitDecoder[12];

		// Token: 0x0400065D RID: 1629
		private readonly BitDecoder[] m_IsRepG1Decoders = new BitDecoder[12];

		// Token: 0x0400065E RID: 1630
		private readonly BitDecoder[] m_IsRepG2Decoders = new BitDecoder[12];

		// Token: 0x0400065F RID: 1631
		private readonly BitDecoder[] m_IsRep0LongDecoders = new BitDecoder[192];

		// Token: 0x04000660 RID: 1632
		private readonly BitTreeDecoder[] m_PosSlotDecoder = new BitTreeDecoder[4];

		// Token: 0x04000661 RID: 1633
		private readonly BitDecoder[] m_PosDecoders = new BitDecoder[114];

		// Token: 0x04000662 RID: 1634
		private BitTreeDecoder m_PosAlignDecoder = new BitTreeDecoder(4);

		// Token: 0x04000663 RID: 1635
		private readonly LenDecoder m_LenDecoder = new LenDecoder();

		// Token: 0x04000664 RID: 1636
		private readonly LenDecoder m_RepLenDecoder = new LenDecoder();

		// Token: 0x04000665 RID: 1637
		private readonly LiteralDecoder m_LiteralDecoder = new LiteralDecoder();

		// Token: 0x04000666 RID: 1638
		private uint m_DictionarySize;

		// Token: 0x04000667 RID: 1639
		private uint m_DictionarySizeCheck;

		// Token: 0x04000668 RID: 1640
		private uint m_PosStateMask;

		// Token: 0x04000669 RID: 1641
		private bool _solid;

		// Token: 0x0200011F RID: 287
		private class LenDecoder
		{
			// Token: 0x060005A3 RID: 1443 RVA: 0x000211FC File Offset: 0x0001F3FC
			public void Create(uint numPosStates)
			{
				for (uint posState = this.m_NumPosStates; posState < numPosStates; posState += 1u)
				{
					this.m_LowCoder[(int)posState] = new BitTreeDecoder(3);
					this.m_MidCoder[(int)posState] = new BitTreeDecoder(3);
				}
				this.m_NumPosStates = numPosStates;
			}

			// Token: 0x060005A4 RID: 1444 RVA: 0x00021248 File Offset: 0x0001F448
			public void Init()
			{
				this.m_Choice.Init();
				for (uint posState = 0u; posState < this.m_NumPosStates; posState += 1u)
				{
					this.m_LowCoder[(int)posState].Init();
					this.m_MidCoder[(int)posState].Init();
				}
				this.m_Choice2.Init();
				this.m_HighCoder.Init();
			}

			// Token: 0x060005A5 RID: 1445 RVA: 0x000212AC File Offset: 0x0001F4AC
			public uint Decode(RangeCoder.Decoder rangeDecoder, uint posState)
			{
				if (this.m_Choice.Decode(rangeDecoder) == 0u)
				{
					return this.m_LowCoder[(int)posState].Decode(rangeDecoder);
				}
				uint symbol = 8u;
				if (this.m_Choice2.Decode(rangeDecoder) == 0u)
				{
					symbol += this.m_MidCoder[(int)posState].Decode(rangeDecoder);
				}
				else
				{
					symbol += 8u;
					symbol += this.m_HighCoder.Decode(rangeDecoder);
				}
				return symbol;
			}

			// Token: 0x04000726 RID: 1830
			private BitDecoder m_Choice;

			// Token: 0x04000727 RID: 1831
			private BitDecoder m_Choice2;

			// Token: 0x04000728 RID: 1832
			private readonly BitTreeDecoder[] m_LowCoder = new BitTreeDecoder[16];

			// Token: 0x04000729 RID: 1833
			private readonly BitTreeDecoder[] m_MidCoder = new BitTreeDecoder[16];

			// Token: 0x0400072A RID: 1834
			private BitTreeDecoder m_HighCoder = new BitTreeDecoder(8);

			// Token: 0x0400072B RID: 1835
			private uint m_NumPosStates;
		}

		// Token: 0x02000120 RID: 288
		private class LiteralDecoder
		{
			// Token: 0x060005A7 RID: 1447 RVA: 0x00021344 File Offset: 0x0001F544
			public void Create(int numPosBits, int numPrevBits)
			{
				if (this.m_Coders != null && this.m_NumPrevBits == numPrevBits && this.m_NumPosBits == numPosBits)
				{
					return;
				}
				this.m_NumPosBits = numPosBits;
				this.m_PosMask = (1u << numPosBits) - 1u;
				this.m_NumPrevBits = numPrevBits;
				uint numStates = 1u << this.m_NumPrevBits + this.m_NumPosBits;
				this.m_Coders = new Decoder2[numStates];
				for (uint i = 0u; i < numStates; i += 1u)
				{
					this.m_Coders[(int)i].Create();
				}
			}

			// Token: 0x060005A8 RID: 1448 RVA: 0x000213C4 File Offset: 0x0001F5C4
			public void Init()
			{
				uint numStates = 1u << this.m_NumPrevBits + this.m_NumPosBits;
				for (uint i = 0u; i < numStates; i += 1u)
				{
					this.m_Coders[(int)i].Init();
				}
			}

			// Token: 0x060005A9 RID: 1449 RVA: 0x00021401 File Offset: 0x0001F601
			private uint GetState(uint pos, byte prevByte)
			{
				return ((pos & this.m_PosMask) << this.m_NumPrevBits) + (uint)(prevByte >> 8 - this.m_NumPrevBits);
			}

			// Token: 0x060005AA RID: 1450 RVA: 0x00021423 File Offset: 0x0001F623
			public byte DecodeNormal(RangeCoder.Decoder rangeDecoder, uint pos, byte prevByte)
			{
				return this.m_Coders[(int)this.GetState(pos, prevByte)].DecodeNormal(rangeDecoder);
			}

			// Token: 0x060005AB RID: 1451 RVA: 0x0002143E File Offset: 0x0001F63E
			public byte DecodeWithMatchByte(RangeCoder.Decoder rangeDecoder, uint pos, byte prevByte, byte matchByte)
			{
				return this.m_Coders[(int)this.GetState(pos, prevByte)].DecodeWithMatchByte(rangeDecoder, matchByte);
			}

			// Token: 0x0400072C RID: 1836
			private Decoder2[] m_Coders;

			// Token: 0x0400072D RID: 1837
			private int m_NumPrevBits;

			// Token: 0x0400072E RID: 1838
			private int m_NumPosBits;

			// Token: 0x0400072F RID: 1839
			private uint m_PosMask;

			// Token: 0x02000128 RID: 296
			private struct Decoder2
			{
				// Token: 0x060005BF RID: 1471 RVA: 0x0002183B File Offset: 0x0001FA3B
				public void Create()
				{
					this.m_Decoders = new BitDecoder[768];
				}

				// Token: 0x060005C0 RID: 1472 RVA: 0x00021850 File Offset: 0x0001FA50
				public void Init()
				{
					for (int i = 0; i < 768; i++)
					{
						this.m_Decoders[i].Init();
					}
				}

				// Token: 0x060005C1 RID: 1473 RVA: 0x00021880 File Offset: 0x0001FA80
				public byte DecodeNormal(RangeCoder.Decoder rangeDecoder)
				{
					uint symbol = 1u;
					do
					{
						symbol = (symbol << 1 | this.m_Decoders[(int)symbol].Decode(rangeDecoder));
					}
					while (symbol < 256u);
					return (byte)symbol;
				}

				// Token: 0x060005C2 RID: 1474 RVA: 0x000218B0 File Offset: 0x0001FAB0
				public byte DecodeWithMatchByte(RangeCoder.Decoder rangeDecoder, byte matchByte)
				{
					uint symbol = 1u;
					for (;;)
					{
						uint matchBit = (uint)(matchByte >> 7 & 1);
						matchByte = (byte)(matchByte << 1);
						uint bit = this.m_Decoders[(int)((1u + matchBit << 8) + symbol)].Decode(rangeDecoder);
						symbol = (symbol << 1 | bit);
						if (matchBit != bit)
						{
							break;
						}
						if (symbol >= 256u)
						{
							goto IL_5C;
						}
					}
					while (symbol < 256u)
					{
						symbol = (symbol << 1 | this.m_Decoders[(int)symbol].Decode(rangeDecoder));
					}
					IL_5C:
					return (byte)symbol;
				}

				// Token: 0x0400074B RID: 1867
				private BitDecoder[] m_Decoders;
			}
		}
	}
}
