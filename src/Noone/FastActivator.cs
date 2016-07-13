using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading;
using System.Threading.Tasks;

namespace Noone
{
    internal class FastActivatorModuleBuilder
    {
        public static readonly ModuleBuilder ModuleBuilder = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("DynamicFastTypeCreaterAssembly"), AssemblyBuilderAccess.Run).DefineDynamicModule("DynamicFastTypeCreaterModuleBuilder");
        public static int CurrId;
    }
    public class FastActivator<T> where T : class, new()
    {
        /*//委托方法
        public static readonly Func<T> createFunc = BuildFunc();
        private static Func<T> BuildFunc()
        {
            var newMethod = new DynamicMethod("CreateFunc", typeof(T), Type.EmptyTypes, true);
            var il = newMethod.GetILGenerator();
            //il.DeclareLocal(typeof(T));
            il.Emit(OpCodes.Newobj, typeof(T).GetConstructor(Type.EmptyTypes));
            //il.Emit(OpCodes.Stloc_0);
            //il.Emit(OpCodes.Ldloc_0);
            il.Emit(OpCodes.Ret);
                        
            return newMethod.CreateDelegate(typeof(Func<T>)) as Func<T>;
        }*/
        public static T CreateInstance()
        {
            //return createFunc();
            return Creater.Create();//调用Creater对象的Create创造T对象
        }

        private static readonly ICreater Creater = BuildCreater();
        public interface ICreater
        {
            T Create();
        }
        private static ICreater BuildCreater()
        {
            var type = typeof(T);
            var typeBuilder = FastActivatorModuleBuilder.ModuleBuilder.DefineType("FastTypeCreater_" + Interlocked.Increment(ref FastActivatorModuleBuilder.CurrId),
                TypeAttributes.Class | TypeAttributes.Public, null, new Type[] { typeof(ICreater) });//创建类型,继承ICreater接口
            
            var ctor = typeBuilder.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard, Type.EmptyTypes);//创建类型的构造方法
            var il = ctor.GetILGenerator();//从构造方法取出ILGenerator
            il.Emit(OpCodes.Ret);//给构造方法加上最基本的代码(空)

            var createMethod = typeBuilder.DefineMethod("Create", MethodAttributes.Public | MethodAttributes.HideBySig |
                MethodAttributes.NewSlot | MethodAttributes.Virtual |
                MethodAttributes.Final, type, Type.EmptyTypes);//创建接口同名方法
            il = createMethod.GetILGenerator();//从方法取出ILGenerator
            il.DeclareLocal(type);//定义临时本地变量

            il.Emit(OpCodes.Newobj, type.GetConstructor(Type.EmptyTypes));//调用当前新建类型的构造方法
            il.Emit(OpCodes.Stloc_0);//栈入变量
            il.Emit(OpCodes.Ldloc_0);//变量压栈
            il.Emit(OpCodes.Ret);//返回栈顶值,方法完成

            typeBuilder.DefineMethodOverride(createMethod, typeof(ICreater).GetMethod("Create"));//跟接口方法根据签名进行绑定

            var createrType = typeBuilder.CreateTypeInfo().AsType();//创建类型

            return (ICreater)Activator.CreateInstance(createrType);//偷懒用Activator.CreateInstance创造刚刚IL代码搞的ICreater对象,有了这个对象就可以调用对象的Create方法调用我们自己搞的IL代码了
        }
    }

}
