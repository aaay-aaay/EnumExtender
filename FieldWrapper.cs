using System;
using System.Reflection;

namespace PastebinMachine.EnumExtender
{
	// Token: 0x0200000C RID: 12
	public class FieldWrapper : IReceiveEnumValue
	{
		// Token: 0x0600002A RID: 42 RVA: 0x00002E95 File Offset: 0x00001095
		public FieldWrapper(FieldInfo field)
		{
			this.field = field;
		}

		// Token: 0x0600002B RID: 43 RVA: 0x00002EA7 File Offset: 0x000010A7
		public void ReceiveValue(object val)
		{
			this.field.SetValue(null, val);
		}

		// Token: 0x04000011 RID: 17
		public FieldInfo field;
	}
}
