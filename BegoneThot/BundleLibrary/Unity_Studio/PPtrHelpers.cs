namespace Unity_Studio
{
	// Token: 0x020000C8 RID: 200
	public static class PPtrHelpers
	{
		// Token: 0x060003E8 RID: 1000 RVA: 0x0001181C File Offset: 0x0000FA1C
		public static PPtr ReadPPtr(this AssetsFile sourceFile)
		{
			PPtr result = new PPtr();
			EndianBinaryReader a_Stream = sourceFile.a_Stream;
			int FileID = a_Stream.ReadInt32();
			if (FileID >= 0 && FileID < sourceFile.sharedAssetsList.Count)
			{
				result.m_FileID = sourceFile.sharedAssetsList[FileID].Index;
			}
			result.m_PathID = ((sourceFile.fileGen < 14) ? a_Stream.ReadInt32() : a_Stream.ReadInt64());
			return result;
		}

		// Token: 0x060003E9 RID: 1001 RVA: 0x00011888 File Offset: 0x0000FA88

	    // Token: 0x060003EA RID: 1002 RVA: 0x000118D4 File Offset: 0x0000FAD4

	    // Token: 0x060003EB RID: 1003 RVA: 0x00011920 File Offset: 0x0000FB20

	    // Token: 0x060003EC RID: 1004 RVA: 0x0001196C File Offset: 0x0000FB6C
		public static void ParseGameObject(this AssetsFile assetsfileList, GameObject m_GameObject)
		{
			foreach (PPtr m_Component in m_GameObject.m_Components)
			{
				AssetPreloadData asset;
			    if (assetsfileList.preloadTable.TryGetValue(m_Component.m_PathID, out asset) && asset.Type2 == 4)
			        m_GameObject.m_Transform = m_Component;
			}
		}
	}
}
