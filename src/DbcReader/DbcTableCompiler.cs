using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace DbcReader
{
    internal static class DbcTableCompiler
    {
        private static MethodInfo BinaryReader_ReadInt32 = typeof(BinaryReader).GetMethod("ReadInt32");
        private static MethodInfo BinaryReader_ReadSingle = typeof(BinaryReader).GetMethod("ReadSingle");
        private static MethodInfo DbcTable_GetString = typeof(DbcTable).GetMethod("GetString");
        private static ConstructorInfo DbcStringReference_ctor = typeof(DbcStringReference).GetConstructor(BindingFlags.NonPublic | BindingFlags.Instance, null, new Type[] { typeof(DbcTable), typeof(int) }, null);

        internal static DbcReaderProducer<T> Compile<T>()
            where T : class
        {
            DynamicMethod method = new DynamicMethod("$DbcTable$" + Regex.Replace(typeof(T).FullName, "\\W+", "$"), typeof(void), new Type[] { typeof(BinaryReader), typeof(int), typeof(DbcTable), typeof(T) }, typeof(DbcTableCompiler).Assembly.ManifestModule);
            ILGenerator gen = method.GetILGenerator();

            var properties = GetTargetInfoForType(typeof(T));
            var propertyMap = properties.ToDictionary(ti => ti.Position);
            var maxPropertyIndex = propertyMap.Keys.Max();
            for (int i = 0; i < maxPropertyIndex; i++) 
            {
                TargetInfo info;
                if (propertyMap.TryGetValue(i, out info))
                {
                    if (info.Property != null)
                    {
                        EmitForProperty(info, gen);
                    }
                    else
                    {
                        EmitForField(info, gen);
                    }
                }
                else
                {
                    gen.Emit(OpCodes.Ldarg_0);
                    gen.EmitCall(OpCodes.Callvirt, BinaryReader_ReadInt32, null);
                    gen.Emit(OpCodes.Pop);
                }
            }

            gen.Emit(OpCodes.Ret);

            return method.CreateDelegate(typeof(DbcReaderProducer<T>)) as DbcReaderProducer<T>;
        }

        private static void EmitForProperty(TargetInfo info, ILGenerator generator) 
        {
            Debug.Assert(info != null);
            Debug.Assert(generator != null);
            Debug.Assert(info.Property != null);
            var setMethod = info.Property.GetSetMethod(true);
            if (setMethod == null)
                throw new InvalidOperationException("Could not find a set method for property " + info.Property.Name);

            generator.Emit(OpCodes.Ldarg_3);

            EmitTypeData(info, generator);

            if (setMethod.IsVirtual)
            {
                generator.EmitCall(OpCodes.Callvirt, setMethod, null);
            }
            else
            {
                generator.EmitCall(OpCodes.Call, setMethod, null);
            }
        }

        private static void EmitTypeData(TargetInfo info, ILGenerator generator)
        {
            switch (info.Type)
            {
                case TargetType.Float32:
                    generator.Emit(OpCodes.Ldarg_0);
                    generator.EmitCall(OpCodes.Callvirt, BinaryReader_ReadSingle, null);
                    break;
                case TargetType.Int32:
                    generator.Emit(OpCodes.Ldarg_0);
                    generator.EmitCall(OpCodes.Callvirt, BinaryReader_ReadInt32, null);
                    break;
                case TargetType.String:
                    generator.Emit(OpCodes.Ldarg_2);
                    generator.Emit(OpCodes.Ldarg_0);
                    generator.EmitCall(OpCodes.Callvirt, BinaryReader_ReadInt32, null);
                    generator.EmitCall(OpCodes.Callvirt, DbcTable_GetString, null);
                    break;
                case TargetType.StringReference:
                    generator.Emit(OpCodes.Ldarg_2);
                    generator.Emit(OpCodes.Ldarg_0);
                    generator.EmitCall(OpCodes.Callvirt, BinaryReader_ReadInt32, null);
                    generator.Emit(OpCodes.Newobj, DbcStringReference_ctor);
                    break;
                default:
                    throw new NotSupportedException("Invalid type for target property.");
            }
        }

        private static void EmitForField(TargetInfo info, ILGenerator generator) 
        {
            Debug.Assert(info != null);
            Debug.Assert(generator != null);
            Debug.Assert(info.Field != null);

            generator.Emit(OpCodes.Ldarg_3);

            EmitTypeData(info, generator);

            generator.Emit(OpCodes.Stfld, info.Field);
        }

        internal static IEnumerable<TargetInfo> GetTargetInfoForType(Type type)
        {
            foreach (FieldInfo fi in type.GetFields(BindingFlags.Public | BindingFlags.Instance))
            {
                var attr = fi.GetCustomAttribute<DbcRecordPositionAttribute>(false);
                if (attr != null)
                {
                    var result = new TargetInfo
                    {
                        Field = fi,
                        Position = attr.Position,
                        Type = GetTargetTypeFromType(fi.FieldType),
                    };
                    yield return result;
                }
            }

            foreach (PropertyInfo pi in type.GetProperties(BindingFlags.Instance | BindingFlags.Public))
            {
                var attr = pi.GetCustomAttribute<DbcRecordPositionAttribute>(false);
                if (attr != null)
                {
                    var result = new TargetInfo
                    {
                        Property = pi,
                        Position = attr.Position,
                        Type = GetTargetTypeFromType(pi.PropertyType),
                    };
                    yield return result;
                }
            }
        }

        internal static TargetType GetTargetTypeFromType(Type type)
        {
            if (type == typeof(int))
                return TargetType.Int32;
            if (type == typeof(float))
                return TargetType.Float32;
            if (type == typeof(DbcStringReference))
                return TargetType.StringReference;
            if (type == typeof(string))
                return TargetType.String;

            throw new InvalidDataException("Invalid data type.");
        }

        internal class TargetInfo
        {
            public PropertyInfo Property;
            public FieldInfo Field;
            public int Position;
            public TargetType Type;

            public void SetValue<TTarget>(TTarget target, int inputVal, DbcTable table)
                where TTarget : class
            {
                switch (Type)
                {
                    case TargetType.Int32:
                        SetValue(target, inputVal);
                        break;
                    case TargetType.Float32:
                        byte[] bits = BitConverter.GetBytes(inputVal);
                        SetValue(target, BitConverter.ToSingle(bits, 0));
                        break;
                    case TargetType.String:
                        string tmp = table.GetString(inputVal);
                        SetValue(target, tmp);
                        break;
                    case TargetType.StringReference:
                        DbcStringReference sref = new DbcStringReference(table, inputVal);
                        SetValue(target, sref);
                        break;
                }
            }

            public void SetValue<TValue, TTarget>(TTarget target, TValue inputVal)
                where TTarget : class
            {
                if (Property != null)
                {
                    Property.SetValue(target, inputVal);
                }
                else // field
                {
                    Field.SetValue(target, inputVal);
                }
            }
        }

        internal enum TargetType
        {
            String,
            Int32,
            Float32,
            StringReference,
        }
    }
}
