namespace SevenZip
{
	// Token: 0x020000DC RID: 220
	internal class CRC
	{
		// Token: 0x06000490 RID: 1168 RVA: 0x0001B93C File Offset: 0x00019B3C
		static CRC()
		{
			for (uint i = 0u; i < 256u; i += 1u)
			{
				uint r = i;
				for (int j = 0; j < 8; j++)
				{
					if ((r & 1u) != 0u)
					{
						r = (r >> 1 ^ 3988292384u);
					}
					else
					{
						r >>= 1;
					}
				}
				Table[(int)i] = r;
			}
		}

		// Token: 0x06000491 RID: 1169 RVA: 0x0001B993 File Offset: 0x00019B93

	    // Token: 0x06000492 RID: 1170 RVA: 0x0001B99C File Offset: 0x00019B9C

	    // Token: 0x06000493 RID: 1171 RVA: 0x0001B9BC File Offset: 0x00019BBC
		public void Update(byte[] data, uint offset, uint size)
		{
			for (uint i = 0u; i < size; i += 1u)
			{
				this._value = (Table[(byte)this._value ^ data[(int)(offset + i)]] ^ this._value >> 8);
			}
		}

		// Token: 0x06000494 RID: 1172 RVA: 0x0001B9F7 File Offset: 0x00019BF7
		public uint GetDigest()
		{
			return this._value ^ uint.MaxValue;
		}

		// Token: 0x06000495 RID: 1173 RVA: 0x0001BA01 File Offset: 0x00019C01

	    // Token: 0x06000496 RID: 1174 RVA: 0x0001BA16 File Offset: 0x00019C16

	    // Token: 0x040005ED RID: 1517
		public static readonly uint[] Table = new uint[256];

		// Token: 0x040005EE RID: 1518
		private uint _value = uint.MaxValue;
	}
}
