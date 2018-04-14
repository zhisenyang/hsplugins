using System.Collections.Generic;

namespace Unity_Studio
{
	// Token: 0x020000C6 RID: 198
	public class GameObject
	{
		// Token: 0x060003E6 RID: 998 RVA: 0x000116B0 File Offset: 0x0000F8B0
		public GameObject(AssetPreloadData preloadData)
		{
			if (preloadData != null)
			{
				AssetsFile sourceFile = preloadData.sourceFile;
				EndianBinaryReader a_Stream = preloadData.sourceFile.a_Stream;
				a_Stream.Position = preloadData.Offset;
				this.uniqueID = preloadData.uniqueID;
				if (sourceFile.platform == -2)
				{
					a_Stream.ReadUInt32();
					sourceFile.ReadPPtr();
					sourceFile.ReadPPtr();
				}
				int m_Component_size = a_Stream.ReadInt32();
				for (int i = 0; i < m_Component_size; i++)
				{
					if ((sourceFile.version[0] == 5 && sourceFile.version[1] >= 5) || sourceFile.version[0] > 5)
					{
						this.m_Components.Add(sourceFile.ReadPPtr());
					}
					else
					{
						a_Stream.ReadInt32();
						this.m_Components.Add(sourceFile.ReadPPtr());
					}
				}
				a_Stream.ReadInt32();
				this.m_Name = a_Stream.ReadAlignedString(a_Stream.ReadInt32());
				if (this.m_Name == "")
				{
					this.m_Name = "GameObject #" + this.uniqueID;
				}
				a_Stream.ReadUInt16();
				a_Stream.ReadBoolean();
				preloadData.Text = this.m_Name;
			}
		}

		// Token: 0x040004F2 RID: 1266
		public List<PPtr> m_Components = new List<PPtr>();

		// Token: 0x040004F3 RID: 1267
		public PPtr m_Transform;

		// Token: 0x040004F4 RID: 1268

	    // Token: 0x040004F5 RID: 1269

	    // Token: 0x040004F6 RID: 1270

	    // Token: 0x040004F7 RID: 1271

	    // Token: 0x040004F8 RID: 1272
		public string m_Name;

		// Token: 0x040004F9 RID: 1273

	    // Token: 0x040004FA RID: 1274

	    // Token: 0x040004FB RID: 1275
		public string uniqueID = "0";
	}
}
