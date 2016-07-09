using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading;
using System.Threading.Tasks;

namespace Noone
{
    public class FastDic<T> where T : class
    {
        public interface IDic
        {
            T Get(string key);
            IDictionary<string, T> GetAll();
        }
        class Dic : IDic
        {
            public T Get(string key)
            {
                return null;
            }

            public IDictionary<string, T> GetAll()
            {
                return null;
            }
        }
        private object sync = new object();
        private IDic dic = new Dic();
        public void Add(string key, T val)
        {
            lock (sync)
            {
                var os = dic.GetAll();
                if (os == null) os = new Dictionary<string, T>();
                os[key] = val;
                var ndc = BuildDic(os);
                Interlocked.Exchange(ref dic, ndc);
            }
        }

        public void Remove(string key)
        {
            lock (sync)
            {
                var os = dic.GetAll();
                if (os == null)
                {
                    Interlocked.Exchange(ref dic, new Dic());
                }
                else if (os.ContainsKey(key))
                {
                    os.Remove(key);
                    if (os.Count == 0)
                    {
                        Interlocked.Exchange(ref dic, new Dic());
                    }
                    else
                    {
                        var ndc = BuildDic(os);
                        Interlocked.Exchange(ref dic, ndc);
                    }
                }
            }
        }

        public T Get(string key)
        {
            return dic.Get(key);
        }
        public static int fastMarsk = 5;

        private Type dicType = typeof(IDictionary<string, T>);

        ModuleBuilder mb = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("DynamicAssembly_"), AssemblyBuilderAccess.Run).DefineDynamicModule("DynamicModuleBuilder_");
        private IDic BuildDic(IDictionary<string, T> kvs)
        {
            var bc = Guid.NewGuid().ToString();
            var typeBuilder = mb.DefineType("DynamicType_" + bc, TypeAttributes.Class | TypeAttributes.Public, null, new Type[] { typeof(IDic) });

            var allField = typeBuilder.DefineField("all", dicType, FieldAttributes.Public);
            FieldBuilder dicField = null;
            #region GetAll
            var AllMethod = typeBuilder.DefineMethod("GetAll", MethodAttributes.Private | MethodAttributes.HideBySig |
                MethodAttributes.NewSlot | MethodAttributes.Virtual |
                MethodAttributes.Final, dicType, Type.EmptyTypes);
            var il = AllMethod.GetILGenerator();
            il.DeclareLocal(dicType);
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldfld, allField);
            il.Emit(OpCodes.Stloc_0);
            il.Emit(OpCodes.Ldloc_0);
            il.Emit(OpCodes.Ret);
            #endregion

            var GetMethod = typeBuilder.DefineMethod("Get", MethodAttributes.Private | MethodAttributes.HideBySig |
                MethodAttributes.NewSlot | MethodAttributes.Virtual |
                MethodAttributes.Final, typeof(T), new Type[] { typeof(string) });
            var equals = typeof(string).GetMethod("Equals", new Type[] { typeof(string) });

            il = GetMethod.GetILGenerator();
            il.DeclareLocal(typeof(T));
            il.DeclareLocal(typeof(bool));

            int t = 0;
            IDictionary<string, FieldBuilder> fields = new Dictionary<string, FieldBuilder>();
            IDictionary<string, T> fieldVals = new Dictionary<string, T>();
            IDictionary<string, T> dicVals = new Dictionary<string, T>();
            Label? next = null;
            foreach (var k in kvs)
            {
                if (t < fastMarsk)
                {
                    if (next != null)
                    {
                        il.MarkLabel((Label)next);
                    }
                    next = il.DefineLabel();
                    var field = typeBuilder.DefineField("field_" + k.Key, typeof(T), FieldAttributes.Private);
                    fields[k.Key] = field;

                    il.Emit(OpCodes.Ldarg_1);
                    il.Emit(OpCodes.Ldstr, k.Key);
                    il.Emit(OpCodes.Callvirt, equals);
                    //il.Emit(OpCodes.Stloc_1);
                    //il.Emit(OpCodes.Ldloc_1);
                    il.Emit(OpCodes.Brfalse, (Label)next);


                    il.Emit(OpCodes.Ldarg_0);
                    il.Emit(OpCodes.Ldfld, field);
                    //il.Emit(OpCodes.Stloc_0);
                    //il.Emit(OpCodes.Ldloc_0);
                    il.Emit(OpCodes.Ret);

                    fieldVals[k.Key] = k.Value;
                }
                else
                {
                    dicVals[k.Key] = k.Value;
                }
                t++;
            }
            il.MarkLabel((Label)next);
            if (dicVals.Count == 0)
            {
                il.Emit(OpCodes.Ldnull);
                il.Emit(OpCodes.Ret);
            }
            else
            {
                dicField = typeBuilder.DefineField("dic", dicType, FieldAttributes.Private);

                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ldfld, dicField);
                il.Emit(OpCodes.Ldarg_1);
                il.Emit(OpCodes.Ldloca_S, 0);
                il.Emit(OpCodes.Callvirt, dicType.GetMethod("TryGetValue", BindingFlags.Instance | BindingFlags.Public));
                il.Emit(OpCodes.Pop);
                il.Emit(OpCodes.Ldloc_0);
                il.Emit(OpCodes.Ret);
            }
            typeBuilder.DefineMethodOverride(GetMethod, typeof(IDic).GetMethod("Get"));
            typeBuilder.DefineMethodOverride(AllMethod, typeof(IDic).GetMethod("GetAll"));

            var ctor = typeBuilder.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard, Type.EmptyTypes);
            il = ctor.GetILGenerator();
            il.Emit(OpCodes.Ret);

            var newMethod = typeBuilder.DefineMethod("Create", MethodAttributes.Public | MethodAttributes.Static, typeof(IDic), new Type[] { dicType, dicType });

            var getItem = dicType.GetMethod("get_Item");

            il = newMethod.GetILGenerator();
            il.DeclareLocal(typeBuilder.AsType());
            il.Emit(OpCodes.Newobj, ctor);
            il.Emit(OpCodes.Stloc_0);

            foreach (var f in fieldVals)
            {
                il.Emit(OpCodes.Ldloc_0);
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ldstr, f.Key);
                il.Emit(OpCodes.Callvirt, getItem);
                il.Emit(OpCodes.Stfld, fields[f.Key]);
            }
            il.Emit(OpCodes.Ldloc_0);
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Stfld, allField);

            if (dicField != null)
            {
                il.Emit(OpCodes.Ldloc_0);
                il.Emit(OpCodes.Ldarg_1);
                il.Emit(OpCodes.Stfld, dicField);
            }

            il.Emit(OpCodes.Ldloc_0);
            il.Emit(OpCodes.Ret);

            var type = typeBuilder.CreateTypeInfo();

            var createFunc = type.GetMethod("Create").CreateDelegate(typeof(Func<IDictionary<string, T>, IDictionary<string, T>, IDic>)) as Func<IDictionary<string, T>, IDictionary<string, T>, IDic>;

            return createFunc(kvs, dicVals);
        }

    }
}
