using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using MonoMod.RuntimeDetour;
using Partiality.Modloader;
using UnityEngine;

using System.Diagnostics;
using Debug = UnityEngine.Debug;

namespace PastebinMachine.EnumExtender
{
	// Token: 0x02000002 RID: 2
	public class EnumExtender : PartialityMod
	{
		// Token: 0x06000001 RID: 1 RVA: 0x00002050 File Offset: 0x00000250
		public override void Init()
		{
			this.ModID = "Enum Extender";
			EnumExtender.instance = this;
		}

		// Token: 0x06000002 RID: 2 RVA: 0x00002064 File Offset: 0x00000264
		public override void OnLoad()
		{
            // Debug.Log("Excessive debug logging!");
			List<Type> list = new List<Type>();
			List<KeyValuePair<IReceiveEnumValue, object>> list2 = new List<KeyValuePair<IReceiveEnumValue, object>>();
			EnumExtender.asm = AppDomain.CurrentDomain.DefineDynamicAssembly(new AssemblyName("EnumExtender_Generated"), AssemblyBuilderAccess.RunAndSave);
			EnumExtender.module = EnumExtender.asm.DefineDynamicModule("EnumExtender_Generated", "EnumExtender_Generated.dll");
			foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
			{
				try
				{
					foreach (Type type in assembly.GetTypes())
					{
						if (type.Name.StartsWith("EnumExt_"))
						{
							if (assembly.FullName == "Assembly-CSharp, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null")
							{
								Debug.LogError("WARNING - EnumExt_ type in game .dll (Assembly-CSharp). This does not work and the type will be ignored.");
								Debug.LogError("Type name - " + type.Name);
							}
							else
							{
								list.Add(type);
							}
						}
					}
				}
				catch (Exception ex)
				{
					Debug.LogError(string.Concat(new object[]
					{
						"Failed loading enums from assembly ",
						assembly.FullName,
						": ",
						ex
					}));
				}
			}
			foreach (Type type in list)
			{
				try
				{
					foreach (FieldInfo fieldInfo in type.GetFields(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
					{
						if (!fieldInfo.IsStatic)
						{
							Debug.LogError(type.FullName + "." + fieldInfo.Name + " contains an enum type, but is not static so it cannot be manipulated. It should be fixed, or moved out of the EnumExt_ class.");
							return;
						}
                        Type typ = fieldInfo.FieldType;
						if (typ.IsEnum)
						{
                            EnumExtender.declarations.Add(new EnumValue(fieldInfo.FieldType, fieldInfo.Name, null, new FieldWrapper(fieldInfo)));
                            // Debug.Log("[EE DEBUG]" + type.Assembly + type.FullName + fieldInfo.FieldType + fieldInfo.Name);
						}
                        else if (typ.IsAssignableFrom(typeof(Delegate)))
                        {
                            MethodInfo invoke = typ.GetMethod("Invoke");
                            ParameterInfo[] pms = invoke.GetParameters();
                            if (fieldInfo.Name == "AddDeclaration" && invoke.ReturnType == typeof(void) && pms.Length == 2 && pms[0].ParameterType == typeof(Type) && pms[1].ParameterType == typeof(string))
                            {
                                fieldInfo.SetValue(null, Delegate.CreateDelegate(typ, null, typeof(EnumExtender).GetMethod("AddDeclaration")));
                            }
                            else if (fieldInfo.Name == "ExtendEnumsAgain" && invoke.ReturnType == typeof(void) && pms.Length == 0)
                            {
                                fieldInfo.SetValue(null, Delegate.CreateDelegate(typ, null, typeof(EnumExtender).GetMethod("ExtendEnumsAgain")));
                            }
                            else
                            {
                                Debug.LogError(type.FullName + "." + fieldInfo.Name + " contains a delegate type, but does not have a combination of name and signature matching any known method. It should be fixed, or moved out of the EnumExt_ class.");
                            }
                        }
                        else
                        {
                            Debug.LogError(type.FullName + "." + fieldInfo.Name + " does not contain an enum type or delegate type. It should be fixed, or moved out of the EnumExt_ class.");
                        }
					}
				}
				catch (Exception ex)
				{
					Debug.LogError("EnumExtender - Error while searching fields: " + ex);
				}
			}
			try
			{
				if (EnumExtender.LoadCallback != null)
				{
					EnumExtender.LoadCallback();
				}
			}
			catch (Exception ex)
			{
				Debug.LogError("EnumExtender - Error in LoadCallback: " + ex);
			}
			ExtendEnums(EnumExtender.declarations, EnumExtender.enums, list2);
			Dictionary<Type, Type> dictionary = new Dictionary<Type, Type>();
			foreach (KeyValuePair<Type, Type> keyValuePair in EnumExtender.enums)
			{
				EnumBuilder enumBuilder2 = keyValuePair.Value as EnumBuilder;
				dictionary[keyValuePair.Key] = enumBuilder2.CreateType();
			}
			EnumExtender.enums = dictionary;
			foreach (KeyValuePair<IReceiveEnumValue, object> keyValuePair2 in list2)
			{
				IReceiveEnumValue key = keyValuePair2.Key;
				object obj = keyValuePair2.Value;
				key.ReceiveValue(obj);
			}
            PerformDMHooks();
            EnumExtender.declarations.Clear();
            // TestBehaviour.Test();
        }
        
        public static void ExtendEnums(List<EnumValue> decls, Dictionary<Type, Type> enums, List<KeyValuePair<IReceiveEnumValue, object>> list2)
        {
            Dictionary<Type, Dictionary<string, object>> values = new Dictionary<Type, Dictionary<string, object>>();
			foreach (EnumValue enumValue in decls)
			{
				try
				{
					Type type2;
					EnumBuilder enumBuilder;
					if (!enums.TryGetValue(enumValue.type, out type2))
					{
						enumBuilder = EnumExtender.module.DefineEnum("EnumExtender_" + extendCounter + "." + enumValue.type.Assembly.GetName().Name.Replace("-", "") + "." + enumValue.type.FullName.Replace("+", ""), TypeAttributes.Public, Enum.GetUnderlyingType(enumValue.type));
                        type2 = enumBuilder;
						enums[enumValue.type] = enumBuilder;
						EnumExtender.enumValues[enumValue.type] = new List<object>();
                        values[enumValue.type] = new Dictionary<string, object>();
						foreach (object obj in Enum.GetValues(enumValue.type))
						{
							object obj2 = Convert.ChangeType(obj, Type.GetTypeCode(Enum.GetUnderlyingType(enumValue.type)));
							string name = Enum.GetName(enumValue.type, obj2);
							enumBuilder.DefineLiteral(name, obj2);
							EnumExtender.enumValues[enumValue.type].Add(obj2);
                            values[enumValue.type][name] = obj2;
						}
					}
					else
					{
						enumBuilder = (EnumBuilder)type2;
					}
                    object obj3;
                    if (!values[enumValue.type].ContainsKey(enumValue.name))
                    {
                        Type underlyingType = Enum.GetUnderlyingType(enumValue.type);
                        obj3 = Convert.ChangeType(0, underlyingType);
                        while (EnumExtender.enumValues[enumValue.type].Contains(obj3))
                        {
                            obj3 = Convert.ChangeType((long)Convert.ChangeType(obj3, typeof(long)) + 1L, underlyingType);
                        }
                        enumBuilder.DefineLiteral(enumValue.name, obj3);
                        values[enumValue.type][enumValue.name] = obj3;
                        EnumExtender.enumValues[enumValue.type].Add(obj3);
                    }
                    else
                    {
                        obj3 = values[enumValue.type][enumValue.name];
                    }
					if (list2 != null) list2.Add(new KeyValuePair<IReceiveEnumValue, object>(enumValue.receiver, Enum.ToObject(enumValue.type, obj3)));
				}
				catch (Exception ex)
				{
					Debug.LogError("EnumExtender - Error while extending enums: " + ex);
				}
			}
            extendCounter++;
        }
        
        public static void AddDeclaration(Type enm, string name)
        {
            /*
            Debug.Log("AddDeclaration called!");
            Debug.Log(new StackTrace());
            Debug.Log("Params:");
            Debug.Log(enm);
            Debug.Log(name);
            */
            EnumExtender.declarations.Add(new EnumValue(enm, name, null, new Nope()));
        }
        
        public static void ExtendEnumsAgain()
        {
            /*
            Debug.Log("ExtendEnumsAgain called!");
            Debug.Log(new StackTrace());
            Debug.Log("Pending declarations:");
            Debug.Log(EnumExtender.declarations.Count);
            */
            Dictionary<Type, Type> newEnums = new Dictionary<Type, Type>();
            ExtendEnums(EnumExtender.declarations, newEnums, null);
            foreach (KeyValuePair<Type, Type> keyValuePair in newEnums)
            {
                EnumExtender.enums[keyValuePair.Key] = (keyValuePair.Value as EnumBuilder).CreateType();
            }
            EnumExtender.declarations.Clear();
        }
        
        public static void PerformDMHooks()
        {
			foreach (MethodInfo methodInfo in typeof(Enum).GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public))
			{
				if (methodInfo.GetMethodBody() != null)
				{
					ParameterInfo[] parameters = methodInfo.GetParameters();
					Type[] array = new Type[parameters.Length + (methodInfo.IsStatic ? 1 : 2)];
					array[0] = typeof(Delegate);
					if (!methodInfo.IsStatic)
					{
						array[1] = typeof(Enum);
					}
					for (int k = 0; k < parameters.Length; k++)
					{
						array[k + (array.Length - parameters.Length)] = parameters[k].ParameterType;
					}
					DynamicMethod dynamicMethod = new DynamicMethod("EnumExtenderHook_" + methodInfo.Name, methodInfo.ReturnType, array);
					ILGenerator ilgenerator = dynamicMethod.GetILGenerator();
					ilgenerator.Emit(OpCodes.Ldc_I4_S, array.Length - 1);
					ilgenerator.Emit(OpCodes.Newarr, typeof(object));
					ilgenerator.Emit(OpCodes.Dup);
					for (int k = 0; k < array.Length - 1; k++)
					{
						ilgenerator.Emit(OpCodes.Ldc_I4_S, k);
						ilgenerator.Emit(OpCodes.Ldarg, k + 1);
						if (array[k + 1].IsValueType)
						{
							ilgenerator.Emit(OpCodes.Box, array[k + 1]);
						}
						ilgenerator.EmitCall(OpCodes.Call, typeof(EnumExtender).GetMethod("ValueHook"), null);
						ilgenerator.Emit(OpCodes.Stelem_Ref);
						ilgenerator.Emit(OpCodes.Dup);
					}
					ilgenerator.Emit(OpCodes.Pop);
					ilgenerator.Emit(OpCodes.Ldarg_0);
					ilgenerator.EmitCall(OpCodes.Call, typeof(EnumExtender).GetMethod("CallDelegate"), null);
					if (methodInfo.ReturnType != typeof(void))
					{
						ilgenerator.EmitCall(OpCodes.Call, typeof(EnumExtender).GetMethod("ReturnHook"), null);
						ilgenerator.Emit(OpCodes.Unbox_Any, methodInfo.ReturnType);
					}
					else
					{
						ilgenerator.Emit(OpCodes.Pop);
					}
					ilgenerator.Emit(OpCodes.Ret);
					Type[] array2 = new Type[array.Length];
					array2[0] = methodInfo.ReturnType;
					if (!methodInfo.IsStatic)
					{
						array2[1] = typeof(Enum);
					}
					for (int k = 0; k < parameters.Length; k++)
					{
						array2[k + (array2.Length - parameters.Length)] = parameters[k].ParameterType;
					}
					MethodInfo methodInfo2 = null;
					foreach (MethodInfo methodInfo3 in typeof(EnumExtenderDMHolder).GetMethods())
					{
						if (methodInfo3.Name == "Invoke" && methodInfo3.GetGenericArguments().Length == array2.Length)
						{
							methodInfo2 = methodInfo3;
							break;
						}
					}
					methodInfo2 = methodInfo2.MakeGenericMethod(array2);
					Hook hook = new Hook(methodInfo, methodInfo2, new EnumExtenderDMHolder(dynamicMethod));
				}
			}
        }
        
        public static void Test()
        {
            Debug.Log(":beeloaf:");
            /*
            Debug.Log("1 " + ConvertEnum<TestEnum>(AbstractPhysicalObject.AbstractObjectType.Rock));
            Debug.Log("2 " + ConvertEnum<TestEnum>(AbstractPhysicalObject.AbstractObjectType.Spear));
            Debug.Log("3 " + ConvertEnum<TestEnum>(AbstractPhysicalObject.AbstractObjectType.AttachedBee));
            Debug.Log("4 " + ConvertEnum<TestEnum>(AbstractPhysicalObject.AbstractObjectType.DataPearl));
            */
            /*
            //Debug.Log("1 " + EnumExt_Test.TestValue);
            //Debug.Log("2 " + Enum.Parse(typeof(TestEnum), "TestValue"));
            
            AddDeclaration(typeof(TestEnum), "DynamicValueOne");
            AddDeclaration(typeof(TestEnum), "DynamicValueTwo");
            ExtendEnumsAgain();
            
            Debug.Log("3 " + Enum.Parse(typeof(TestEnum), "DynamicValueOne"));
            Debug.Log("4 " + Enum.Parse(typeof(TestEnum), "DynamicValueTwo"));
            */
            
            AddDeclaration(typeof(Menu.MenuScene.SceneID), "DynamicValue");
            ExtendEnumsAgain();
            Debug.Log("1 " + Enum.Parse(typeof(Menu.MenuScene.SceneID), "DynamicValue"));
            // new List<Menu.MenuScene.SceneID>((Menu.MenuScene.SceneID[])Enum.GetValues(typeof(Menu.MenuScene.SceneID)));
            
            Debug.Log(":beehappyloaf:");
		}

		// Token: 0x06000003 RID: 3 RVA: 0x00002A88 File Offset: 0x00000C88
		public static object ValueHook(object obj)
		{
            if (obj == null)
            {
                return obj;
            }
			if (obj is Type)
			{
				Type type;
				if (EnumExtender.enums.TryGetValue(obj as Type, out type))
				{
					obj = type;
				}
			}
			else if (obj.GetType().IsEnum)
			{
				Type type2 = obj.GetType();
				Type type;
				if (EnumExtender.enums.TryGetValue(type2, out type))
				{
					obj = Enum.ToObject(type, obj.GetType().GetField("value__", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).GetValue(obj));
				}
			}
            else if (obj.GetType().IsArray && obj.GetType().GetElementType().IsEnum)
            {
                Array asArray = obj as Array;
                Type arrayType = obj.GetType();
                Type elementType = arrayType.GetElementType();
                Type converted;
                if (EnumExtender.enums.TryGetValue(elementType, out converted))
                {
                    int[] lengths = new int[asArray.Rank];
                    int[] lowerBounds = new int[asArray.Rank];
                    for (int i = 0; i < asArray.Rank; i++)
                    {
                        lengths[i] = asArray.GetLength(i);
                        lowerBounds[i] = asArray.GetLowerBound(i);
                    }
                    Array newArr = Array.CreateInstance(converted, lengths, lowerBounds);
                    int[] indices = new int[newArr.Rank];
                    for (int i = 0; i < newArr.Rank; i++)
                    {
                        indices[i] = lowerBounds[i];
                    }
                    while (true)
                    {
                        newArr.SetValue(ValueHook(asArray.GetValue(indices)), indices);
                        int i = 0;
                        while (true)
                        {
                            if (i == indices.Length)
                            {
                                goto end;
                            }
                            indices[i]++;
                            if (indices[i] >= lowerBounds[i] + lengths[i])
                            {
                                indices[i] = 0;
                                i++;
                            }
                            else
                                break;
                        }
                    }
                    end:
                    obj = newArr;
                }
            }
			return obj;
		}

		// Token: 0x06000004 RID: 4 RVA: 0x00002B28 File Offset: 0x00000D28
		public static object CallDelegate(object[] objs, Delegate del)
		{
			return del.GetType().GetMethod("Invoke").Invoke(del, objs);
		}

		// Token: 0x06000005 RID: 5 RVA: 0x00002B54 File Offset: 0x00000D54
		public static object ReturnHook(object obj)
		{
			object result;
			if (obj == null)
			{
				result = null;
			}
			else
			{
				if (obj is Type)
				{
					foreach (KeyValuePair<Type, Type> keyValuePair in EnumExtender.enums)
					{
						if (keyValuePair.Value == obj)
						{
							obj = keyValuePair.Key;
						}
					}
				}
				else if (obj.GetType().IsEnum)
				{
					Type type = obj.GetType();
					foreach (KeyValuePair<Type, Type> keyValuePair in EnumExtender.enums)
					{
						if (keyValuePair.Value == type)
						{
							obj = Enum.ToObject(keyValuePair.Key, obj.GetType().GetField("value__", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).GetValue(obj));
						}
					}
				}
                else if (obj.GetType().IsArray && obj.GetType().GetElementType().IsEnum)
                {
                    Array asArray = obj as Array;
                    Type arrayType = obj.GetType();
                    Type elementType = arrayType.GetElementType();
                    Type converted = null;
                    // if (EnumExtender.enums.TryGetValue(elementType, out converted))
                    foreach (KeyValuePair<Type, Type> keyValuePair in EnumExtender.enums)
                    {
                        if (keyValuePair.Value == elementType)
                        {
                            converted = keyValuePair.Key;
                        }
                    }
                    if (converted != null)
                    {
                        int[] lengths = new int[asArray.Rank];
                        int[] lowerBounds = new int[asArray.Rank];
                        for (int i = 0; i < asArray.Rank; i++)
                        {
                            lengths[i] = asArray.GetLength(i);
                            lowerBounds[i] = asArray.GetLowerBound(i);
                        }
                        Array newArr = Array.CreateInstance(converted, lengths, lowerBounds);
                        int[] indices = new int[newArr.Rank];
                        for (int i = 0; i < newArr.Rank; i++)
                        {
                            indices[i] = lowerBounds[i];
                        }
                        while (true)
                        {
                            newArr.SetValue(ValueHook(asArray.GetValue(indices)), indices);
                            int i = 0;
                            while (true)
                            {
                                if (i == indices.Length)
                                {
                                    goto end;
                                }
                                indices[i]++;
                                if (indices[i] >= lowerBounds[i] + lengths[i])
                                {
                                    indices[i] = 0;
                                    i++;
                                }
                                else
                                    break;
                            }
                        }
                        end:
                        obj = newArr;
                    }
                }
				result = obj;
			}
			return result;
		}
        
        public static T ConvertEnum<T>(Enum frm) where T : struct, IComparable, IConvertible, IFormattable
        {
            if (frm == null) return default(T);
            if (!Enum.IsDefined(frm.GetType(), frm)) return GetDefault<T>();
            // if (Enum.TryParse(typeof(T), frm.ToString(), out res)) return res;
            try
            {
                return (T)Enum.Parse(typeof(T), frm.ToString());
            }
            catch
            {
            }
            return GetDefault<T>();
        }
        
        public static T GetDefault<T>() where T : struct, IComparable, IConvertible, IFormattable
        {
            Type underlyingType = Enum.GetUnderlyingType(typeof(T));
            long minValue = Convert.ToInt64(underlyingType.GetField("MinValue").GetValue(null));
            long maxValue = Convert.ToInt64(underlyingType.GetField("MaxValue").GetValue(null));
            for (long i = minValue; i < maxValue; i++)
            {
                if (!Enum.IsDefined(typeof(T), Convert.ChangeType(i, underlyingType)))
                {
                    return (T)Enum.ToObject(typeof(T), Convert.ChangeType(i, underlyingType));
                }
            }
            Debug.LogError("Warning: could not find default for " + typeof(T).FullName + ", no unused types");
            return (T)Enum.ToObject(typeof(T), Convert.ChangeType(minValue, underlyingType));
        }

		// Token: 0x14000001 RID: 1
		// (add) Token: 0x06000006 RID: 6 RVA: 0x00002C8C File Offset: 0x00000E8C
		// (remove) Token: 0x06000007 RID: 7 RVA: 0x00002CA3 File Offset: 0x00000EA3
		public static event EnumExtender.EnumExtCallback LoadCallback;

		// Token: 0x04000001 RID: 1
		public static AssemblyBuilder asm;

		// Token: 0x04000002 RID: 2
		public static ModuleBuilder module;

		// Token: 0x04000003 RID: 3
		public static List<EnumValue> declarations = new List<EnumValue>();

		// Token: 0x04000004 RID: 4
		public static Dictionary<Type, Type> enums = new Dictionary<Type, Type>();

		// Token: 0x04000005 RID: 5
		public static Dictionary<Type, List<object>> enumValues = new Dictionary<Type, List<object>>();

		// Token: 0x04000006 RID: 6
		public static EnumExtender instance;

		// Token: 0x04000008 RID: 8
		public string updateURL = "http://beestuff.pythonanywhere.com/audb/api/mods/0/1";

		// Token: 0x04000009 RID: 9
		public int version = 18;

		// Token: 0x0400000A RID: 10
		public string keyE = "AQAB";

		// Token: 0x0400000B RID: 11
		public string keyN = "yu7XMmICrzuavyZRGWoknFIbJX4N4zh3mFPOyfzmQkil2axVIyWx5ogCdQ3OTdSZ0xpQ3yiZ7zqbguLu+UWZMfLOBKQZOs52A9OyzeYm7iMALmcLWo6OdndcMc1Uc4ZdVtK1CRoPeUVUhdBfk2xwjx+CvZUlQZ26N1MZVV0nq54IOEJzC9qQnVNgeeHxO1lRUTdg5ZyYb7I2BhHfpDWyTvUp6d5m6+HPKoalC4OZSfmIjRAi5UVDXNRWn05zeT+3BJ2GbKttwvoEa6zrkVuFfOOe9eOAWO3thXmq9vJLeF36xCYbUJMkGR2M5kDySfvoC7pzbzyZ204rXYpxxXyWPP5CaaZFP93iprZXlSO3XfIWwws+R1QHB6bv5chKxTZmy/Imo4M3kNLo5B2NR/ZPWbJqjew3ytj0A+2j/RVwV9CIwPlN4P50uwFm+Mr0OF2GZ6vU0s/WM7rE78+8Wwbgcw6rTReKhVezkCCtOdPkBIOYv3qmLK2S71NPN2ulhMHD9oj4t0uidgz8pNGtmygHAm45m2zeJOhs5Q/YDsTv5P7xD19yfVcn5uHpSzRIJwH5/DU1+aiSAIRMpwhF4XTUw73+pBujdghZdbdqe2CL1juw7XCa+XfJNtsUYrg+jPaCEUsbMuNxdFbvS0Jleiu3C8KPNKDQaZ7QQMnEJXeusdU=";

		// Token: 0x02000003 RID: 3
		// (Invoke) Token: 0x0600000B RID: 11
		public delegate void EnumExtCallback();

		// Token: 0x02000004 RID: 4
		// (Invoke) Token: 0x0600000F RID: 15
		public delegate TResult O<TResult>();

		// Token: 0x02000005 RID: 5
		// (Invoke) Token: 0x06000013 RID: 19
		public delegate TResult O<TResult, T1>(T1 t1);

		// Token: 0x02000006 RID: 6
		// (Invoke) Token: 0x06000017 RID: 23
		public delegate TResult O<TResult, T1, T2>(T1 t1, T2 t2);

		// Token: 0x02000007 RID: 7
		// (Invoke) Token: 0x0600001B RID: 27
		public delegate TResult O<TResult, T1, T2, T3>(T1 t1, T2 t2, T3 t3);

		// Token: 0x02000008 RID: 8
		// (Invoke) Token: 0x0600001F RID: 31
		public delegate TResult O<TResult, T1, T2, T3, T4>(T1 t1, T2 t2, T3 t3, T4 t4);
        
        public static int extendCounter;
	}
}
