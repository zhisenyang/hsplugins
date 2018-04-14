namespace Unity_Studio
{
	// Token: 0x020000D6 RID: 214
	public class Transform
	{
		// Token: 0x0600040B RID: 1035 RVA: 0x000140EC File Offset: 0x000122EC
		public Transform(AssetPreloadData preloadData)
		{
			AssetsFile sourceFile = preloadData.sourceFile;
			EndianBinaryReader a_Stream = preloadData.sourceFile.a_Stream;
			a_Stream.Position = preloadData.Offset;
			if (sourceFile.platform == -2)
			{
				a_Stream.ReadUInt32();
				sourceFile.ReadPPtr();
				sourceFile.ReadPPtr();
			}
			this.m_GameObject = sourceFile.ReadPPtr();
			{
				a_Stream.ReadSingle();
				a_Stream.ReadSingle();
				a_Stream.ReadSingle();
                a_Stream.ReadSingle();
			}
		    {
				a_Stream.ReadSingle();
				a_Stream.ReadSingle();
                a_Stream.ReadSingle();
			}
		    {
				a_Stream.ReadSingle();
				a_Stream.ReadSingle();
                a_Stream.ReadSingle();
			}
		    int m_ChildrenCount = a_Stream.ReadInt32();
			for (int i = 0; i < m_ChildrenCount; i++)
			{
				sourceFile.ReadPPtr();
			}
			this.m_Father = sourceFile.ReadPPtr();
		}

		// Token: 0x04000559 RID: 1369
		public readonly PPtr m_GameObject;
		// Token: 0x0400055E RID: 1374
		public readonly PPtr m_Father;
	}
}
