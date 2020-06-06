using System;
using System.Reflection.Emit;

namespace PastebinMachine.EnumExtender
{
	// Token: 0x02000009 RID: 9
	public class EnumExtenderDMHolder
	{
		// Token: 0x06000022 RID: 34 RVA: 0x00002D0B File Offset: 0x00000F0B
		public EnumExtenderDMHolder(DynamicMethod dm)
		{
			this.dm = dm;
		}

		// Token: 0x06000023 RID: 35 RVA: 0x00002D20 File Offset: 0x00000F20
		public TResult Invoke<TResult>(EnumExtender.O<TResult> d)
		{
			return (TResult)((object)this.dm.Invoke(null, new object[]
			{
				d
			}));
		}

		// Token: 0x06000024 RID: 36 RVA: 0x00002D50 File Offset: 0x00000F50
		public TResult Invoke<TResult, T1>(EnumExtender.O<TResult, T1> d, T1 t1)
		{
			return (TResult)((object)this.dm.Invoke(null, new object[]
			{
				d,
				t1
			}));
		}

		// Token: 0x06000025 RID: 37 RVA: 0x00002D88 File Offset: 0x00000F88
		public TResult Invoke<TResult, T1, T2>(EnumExtender.O<TResult, T1, T2> d, T1 t1, T2 t2)
		{
			return (TResult)((object)this.dm.Invoke(null, new object[]
			{
				d,
				t1,
				t2
			}));
		}

		// Token: 0x06000026 RID: 38 RVA: 0x00002DCC File Offset: 0x00000FCC
		public TResult Invoke<TResult, T1, T2, T3>(EnumExtender.O<TResult, T1, T2, T3> d, T1 t1, T2 t2, T3 t3)
		{
			return (TResult)((object)this.dm.Invoke(null, new object[]
			{
				d,
				t1,
				t2,
				t3
			}));
		}

		// Token: 0x06000027 RID: 39 RVA: 0x00002E18 File Offset: 0x00001018
		public TResult Invoke<TResult, T1, T2, T3, T4>(EnumExtender.O<TResult, T1, T2, T3, T4> d, T1 t1, T2 t2, T3 t3, T4 t4)
		{
			return (TResult)((object)this.dm.Invoke(null, new object[]
			{
				d,
				t1,
				t2,
				t3,
				t4
			}));
		}

		// Token: 0x0400000C RID: 12
		public DynamicMethod dm;
	}
}
