using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Unity_Studio
{
	// Token: 0x020000C0 RID: 192
	public class EndianBinaryReader : BinaryReader
	{
		// Token: 0x060003C9 RID: 969 RVA: 0x0000CC55 File Offset: 0x0000AE55
		public EndianBinaryReader(Stream stream, EndianType endian = EndianType.BigEndian) : base(stream)
		{
			this.endian = endian;
		}

		// Token: 0x17000016 RID: 22
		// (get) Token: 0x060003CA RID: 970 RVA: 0x0000CC89 File Offset: 0x0000AE89
		// (set) Token: 0x060003CB RID: 971 RVA: 0x0000CC96 File Offset: 0x0000AE96
		public long Position
		{
			get
			{
				return this.BaseStream.Position;
			}
			set
			{
				this.BaseStream.Position = value;
			}
		}

		// Token: 0x060003CC RID: 972 RVA: 0x0000CCA4 File Offset: 0x0000AEA4
		public override short ReadInt16()
		{
			if (this.endian == EndianType.BigEndian)
			{
				this.a16 = this.ReadBytes(2);
				Array.Reverse(this.a16);
				return BitConverter.ToInt16(this.a16, 0);
			}
			return base.ReadInt16();
		}

		// Token: 0x060003CD RID: 973 RVA: 0x0000CCD9 File Offset: 0x0000AED9
		public override int ReadInt32()
		{
			if (this.endian == EndianType.BigEndian)
			{
				this.a32 = this.ReadBytes(4);
				Array.Reverse(this.a32);
				return BitConverter.ToInt32(this.a32, 0);
			}
			return base.ReadInt32();
		}

		// Token: 0x060003CE RID: 974 RVA: 0x0000CD0E File Offset: 0x0000AF0E
		public override long ReadInt64()
		{
			if (this.endian == EndianType.BigEndian)
			{
				this.a64 = this.ReadBytes(8);
				Array.Reverse(this.a64);
				return BitConverter.ToInt64(this.a64, 0);
			}
			return base.ReadInt64();
		}

		// Token: 0x060003CF RID: 975 RVA: 0x0000CD43 File Offset: 0x0000AF43
		public override ushort ReadUInt16()
		{
			if (this.endian == EndianType.BigEndian)
			{
				this.a16 = this.ReadBytes(2);
				Array.Reverse(this.a16);
				return BitConverter.ToUInt16(this.a16, 0);
			}
			return base.ReadUInt16();
		}

		// Token: 0x060003D0 RID: 976 RVA: 0x0000CD78 File Offset: 0x0000AF78
		public override uint ReadUInt32()
		{
			if (this.endian == EndianType.BigEndian)
			{
				this.a32 = this.ReadBytes(4);
				Array.Reverse(this.a32);
				return BitConverter.ToUInt32(this.a32, 0);
			}
			return base.ReadUInt32();
		}

		// Token: 0x060003D1 RID: 977 RVA: 0x0000CDAD File Offset: 0x0000AFAD
		public override ulong ReadUInt64()
		{
			if (this.endian == EndianType.BigEndian)
			{
				this.a64 = this.ReadBytes(8);
				Array.Reverse(this.a64);
				return BitConverter.ToUInt64(this.a64, 0);
			}
			return base.ReadUInt64();
		}

		// Token: 0x060003D2 RID: 978 RVA: 0x0000CDE2 File Offset: 0x0000AFE2
		public override float ReadSingle()
		{
			if (this.endian == EndianType.BigEndian)
			{
				this.a32 = this.ReadBytes(4);
				Array.Reverse(this.a32);
				return BitConverter.ToSingle(this.a32, 0);
			}
			return base.ReadSingle();
		}

		// Token: 0x060003D3 RID: 979 RVA: 0x0000CE17 File Offset: 0x0000B017
		public override double ReadDouble()
		{
			if (this.endian == EndianType.BigEndian)
			{
				this.a64 = this.ReadBytes(8);
				Array.Reverse(this.a64);
				return BitConverter.ToUInt64(this.a64, 0);
			}
			return base.ReadDouble();
		}

		// Token: 0x060003D4 RID: 980 RVA: 0x0000CE4E File Offset: 0x0000B04E

	    // Token: 0x060003D5 RID: 981 RVA: 0x0000CE64 File Offset: 0x0000B064
		public void AlignStream(int alignment)
		{
			long mod = this.BaseStream.Position % alignment;
			if (mod != 0L)
			{
				this.BaseStream.Position += alignment - mod;
			}
		}

		// Token: 0x060003D6 RID: 982 RVA: 0x0000CE9C File Offset: 0x0000B09C
		public string ReadAlignedString(int length)
		{
			if (length > 0 && length < this.BaseStream.Length - this.BaseStream.Position)
			{
				byte[] stringData = this.ReadBytes(length);
				string @string = Encoding.UTF8.GetString(stringData);
				this.AlignStream(4);
				return @string;
			}
			return "";
		}

		// Token: 0x060003D7 RID: 983 RVA: 0x0000CEE8 File Offset: 0x0000B0E8
		public string ReadStringToNull()
		{
			List<byte> bytes = new List<byte>();
			byte b;
			while (this.BaseStream.Position != this.BaseStream.Length && (b = this.ReadByte()) != 0)
			{
				bytes.Add(b);
			}
			return Encoding.UTF8.GetString(bytes.ToArray());
		}

		// Token: 0x040004B7 RID: 1207
		public EndianType endian;

		// Token: 0x040004B8 RID: 1208
		private byte[] a16 = new byte[2];

		// Token: 0x040004B9 RID: 1209
		private byte[] a32 = new byte[4];

		// Token: 0x040004BA RID: 1210
		private byte[] a64 = new byte[8];
	}
}
