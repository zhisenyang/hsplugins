namespace Unity_Studio
{
	// Token: 0x020000B5 RID: 181
	public class BuildSettings
	{
		// Token: 0x060003A0 RID: 928 RVA: 0x00006428 File Offset: 0x00004628
		public BuildSettings(AssetPreloadData preloadData)
		{
			AssetsFile sourceFile = preloadData.sourceFile;
			EndianBinaryReader a_Stream = preloadData.sourceFile.a_Stream;
			a_Stream.Position = preloadData.Offset;
			int levels = a_Stream.ReadInt32();
			for (int i = 0; i < levels; i++)
			{
				a_Stream.ReadAlignedString(a_Stream.ReadInt32());
			}
			if (sourceFile.version[0] == 5)
			{
				int preloadedPlugins = a_Stream.ReadInt32();
				for (int j = 0; j < preloadedPlugins; j++)
				{
					a_Stream.ReadAlignedString(a_Stream.ReadInt32());
				}
			}
			a_Stream.Position += 4L;
			if (sourceFile.fileGen >= 8)
			{
				a_Stream.Position += 4L;
			}
			if (sourceFile.fileGen >= 9)
			{
				a_Stream.Position += 4L;
			}
			if (sourceFile.version[0] == 5 || (sourceFile.version[0] == 4 && (sourceFile.version[1] >= 3 || (sourceFile.version[1] == 2 && sourceFile.buildType[0] != "a"))))
			{
				a_Stream.Position += 4L;
			}
			this.m_Version = a_Stream.ReadAlignedString(a_Stream.ReadInt32());
		}

		// Token: 0x0400043F RID: 1087
		public string m_Version;
	}
}
