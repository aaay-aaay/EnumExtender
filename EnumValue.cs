using System;

namespace PastebinMachine.EnumExtender
{
	// Token: 0x0200000A RID: 10
	public class EnumValue
	{
		// Token: 0x06000028 RID: 40 RVA: 0x00002E6D File Offset: 0x0000106D
		public EnumValue(Type type, string name, object id, IReceiveEnumValue receiver)
		{
			this.type = type;
			this.name = name;
			this.id = id;
			this.receiver = receiver;
		}

		// Token: 0x0400000D RID: 13
		public Type type;

		// Token: 0x0400000E RID: 14
		public string name;

		// Token: 0x0400000F RID: 15
		public object id;

		// Token: 0x04000010 RID: 16
		public IReceiveEnumValue receiver;
	}
}
