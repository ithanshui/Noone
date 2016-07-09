using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Xml;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Reflection.Emit;
using System.IO;

namespace Noone
{
    internal class ContainerHelper
    {
        private string xml;
        private string binPath;
        private Container container;
        private MethodInfo regMethodNew;
        private MethodInfo regMethodArgs;

        public ContainerHelper(string xml, Container container, string binPath = null)
        {
            this.container = container;
            if (binPath == null)
            {
                this.binPath = System.AppContext.BaseDirectory + "/";
                this.xml = this.binPath + xml;
            }
            else
            {
                this.binPath = binPath + "/";
                this.xml = this.binPath + xml;
            }
            regMethodNew = typeof(ContainerHelper).GetMethod(nameof(RegContainerNewType), BindingFlags.Instance | BindingFlags.NonPublic);
            regMethodArgs = typeof(ContainerHelper).GetMethod(nameof(RegContainerArgType), BindingFlags.Instance | BindingFlags.NonPublic);
        }

        private void RegContainerType(Type iType, Type moduleType, string name, bool isSingleton, bool isPerThread, string args)
        {
            if (string.IsNullOrWhiteSpace(args))
            {
                var method = regMethodNew.MakeGenericMethod(iType, moduleType);
                method.Invoke(this, new object[] { name, isSingleton, isPerThread });
            }
            else
            {
                var method = regMethodArgs.MakeGenericMethod(iType, moduleType);
                method.Invoke(this, new object[] { name, isSingleton, isPerThread, args });
            }
        }

        private void RegContainerNewType<F, S>(string name, bool isSingleton, bool isPerThread) where F : class where S : F, new()
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                if (isSingleton)
                    container.RegisterSingleton<F, S>(null, isPerThread);
                else
                    container.Register<F, S>();
            }
            else
            {
                if (isSingleton)
                    container.RegisterSingleton<F, S>(name, isPerThread);
                else
                    container.Register<F, S>(name);
            }
        }


        private int mid = 0;
        private string tmpMark = "<@_@" + Guid.NewGuid().ToString() + "@_@>";

        private void RegContainerArgType<F, S>(string name, bool isSingleton, bool isPerThread, string argstr) where F : class where S : F
        {
            var tstr = argstr.Replace("\\,", tmpMark);
            var args = tstr.Split(new char[] { ',' }, StringSplitOptions.None);

            var moduleType = typeof(S);
            var cts = moduleType.GetConstructors(BindingFlags.Public | BindingFlags.Instance);

            ConstructorInfo ctor = null;
            var parObjs = new object[args.Length];
            //var parObjs = new Dictionary<int, object>();
            var parNullables = new Dictionary<int, bool>();
            var parTypes = new Dictionary<int, Type>();
            foreach (var ct in cts)
            {
                var pars = ct.GetParameters();
                var isMatch = false;
                if (pars.Length == args.Length)
                {
                    isMatch = true;
                    for (int i = 0, j = pars.Length; i < j; i++)
                    {
                        var par = pars[i];
                        object parObj;
                        bool isNullable;
                        if (isTypeMatch(par.ParameterType, args[i], out parObj, out isNullable))
                        {
                            parNullables[i] = isNullable;
                            parObjs[i] = parObj;
                            parTypes[i] = par.ParameterType;
                        }
                        else
                        {
                            isMatch = false;
                            break;
                        }
                    }
                }
                if (isMatch)
                {
                    ctor = ct;
                    break;
                }
            }

            if (ctor == null)
                throw new UndefinedParamsConstructorException(argstr);
            var method = new DynamicMethod("func_" + mid++, typeof(F), null, true);
            var il = method.GetILGenerator();

            il.DeclareLocal(moduleType);

            for (int i = 0, j = parObjs.Length; i < j; i++)
            {
                var isNullable = parNullables[i];
                LocalBuilder lb = null;
                if (isNullable)
                {
                    lb = il.DeclareLocal(parTypes[i]);
                }

                var par = parObjs[i];
                if (par == null)
                {
                    if (isNullable)
                    {
                        il.Emit(OpCodes.Ldloca_S, lb);
                        il.Emit(OpCodes.Initobj, parTypes[i]);
                        il.Emit(OpCodes.Ldloc, lb);
                    }
                    else
                        il.Emit(OpCodes.Ldnull);
                }
                else if (par is string)
                    il.Emit(OpCodes.Ldstr, par.ToString());
                else if (par is char)
                {
                    il.Emit(OpCodes.Ldc_I4_S, ((char)par));
                    if (isNullable)
                        il.Emit(OpCodes.Newobj, typeof(char?).GetConstructor(new Type[] { charType }));
                }
                else if (par is int)
                {
                    il.Emit(OpCodes.Ldc_I4, ((int)par));
                    if (isNullable)
                        il.Emit(OpCodes.Newobj, typeof(int?).GetConstructor(new Type[] { intType }));
                }
                else if (par is float)
                {
                    il.Emit(OpCodes.Ldc_R4, ((float)par));
                    if (isNullable)
                        il.Emit(OpCodes.Newobj, typeof(float?).GetConstructor(new Type[] { floatType }));
                }
                else if (par is double)
                {
                    il.Emit(OpCodes.Ldc_R8, ((double)par));
                    if (isNullable)
                        il.Emit(OpCodes.Newobj, typeof(double?).GetConstructor(new Type[] { doubleType }));
                }
                else if (par is short)
                {
                    il.Emit(OpCodes.Ldc_I4, ((short)par));
                    if (isNullable)
                        il.Emit(OpCodes.Newobj, typeof(short?).GetConstructor(new Type[] { shortType }));
                }
                else if (par is long)
                {
                    il.Emit(OpCodes.Ldc_I8, ((long)par));
                    if (isNullable)
                        il.Emit(OpCodes.Newobj, typeof(long?).GetConstructor(new Type[] { longType }));
                }
                else if (par is bool)
                {
                    if ((bool)par)
                        il.Emit(OpCodes.Ldc_I4_0);
                    else
                        il.Emit(OpCodes.Ldc_I4_1);
                    if (isNullable)
                        il.Emit(OpCodes.Newobj, typeof(bool?).GetConstructor(new Type[] { boolType }));
                }
                else if (par is sbyte)
                {
                    il.Emit(OpCodes.Ldc_I4, ((sbyte)par));
                    if (isNullable)
                        il.Emit(OpCodes.Newobj, typeof(sbyte?).GetConstructor(new Type[] { sbyteType }));
                }
                else if (par is byte)
                {
                    il.Emit(OpCodes.Ldc_I4_S, ((byte)par));
                    if (isNullable)
                        il.Emit(OpCodes.Newobj, typeof(byte?).GetConstructor(new Type[] { byteType }));
                }
                else if (par is ushort)
                {
                    il.Emit(OpCodes.Ldc_I4_S, ((ushort)par));
                    if (isNullable)
                        il.Emit(OpCodes.Newobj, typeof(ushort?).GetConstructor(new Type[] { ushortType }));
                }
                else if (par is uint)
                {
                    il.Emit(OpCodes.Ldc_I4_S, ((uint)par));
                    if (isNullable)
                        il.Emit(OpCodes.Newobj, typeof(uint?).GetConstructor(new Type[] { uintType }));
                }
                else if (par is ulong)
                {
                    il.Emit(OpCodes.Ldc_I8, ((ulong)par));
                    if (isNullable)
                        il.Emit(OpCodes.Newobj, typeof(ulong?).GetConstructor(new Type[] { ulongType }));
                }
                else if (par is decimal)
                {
                    il.Emit(OpCodes.Ldstr, par.ToString());//decimal构造太复杂了，取巧，直接转字符再parse回来，损失一点点效率
                    il.Emit(OpCodes.Call, decimalType.GetMethod("Parse", new Type[] { stringType }));

                    if (isNullable)
                        il.Emit(OpCodes.Newobj, typeof(decimal?).GetConstructor(new Type[] { decimalType }));
                }
                else
                {
                    throw new UndefinedParamsConstructorException("Unknow param type:" + par);
                }
            }
            il.Emit(OpCodes.Newobj, ctor);
            il.Emit(OpCodes.Stloc_0);
            il.Emit(OpCodes.Ldloc_0);

            il.Emit(OpCodes.Ret);

            var func = method.CreateDelegate(typeof(Func<F>)) as Func<F>;

            if (isSingleton)
                container.RegisterSingleton<F>(func, name, isPerThread);
            else
                container.Register<F>(func, name);

        }

        private Type intType = typeof(int);
        private Type decimalType = typeof(decimal);
        private Type floatType = typeof(float);
        private Type doubleType = typeof(double);
        private Type shortType = typeof(short);
        private Type longType = typeof(long);
        private Type boolType = typeof(bool);
        private Type sbyteType = typeof(sbyte);
        private Type byteType = typeof(byte);
        private Type ushortType = typeof(ushort);
        private Type uintType = typeof(uint);
        private Type ulongType = typeof(ulong);
        private Type stringType = typeof(string);
        private Type charType = typeof(char);
        private Type nullableType = typeof(Nullable<>);

        private bool isTypeMatch(Type type, string str, out object rstObj, out bool isNullableType)
        {
            TypeInfo tf = null;
            isNullableType = false;
            if (str.Length > 2)
            {
                if ((str == "null" || str == "NULL") && (type == stringType || ((tf = type.GetTypeInfo()).IsGenericType && (isNullableType = tf.GetGenericTypeDefinition() == nullableType))))
                {
                    rstObj = null;
                    return true;
                }
                var len = str.Length - 1;
                if (str[0] == '"' && str[len] == '"')
                {
                    if (type == stringType)
                    {
                        rstObj = str.Substring(1, len - 1).Replace(tmpMark, ",");
                        return true;
                    }
                }
                if (str.Length == 3 && str[0] == '\'' && str[len] == '\'')
                {
                    if (type == charType)
                    {
                        rstObj = str[1];
                        return true;
                    }
                }
            }

            if (type == intType || ((tf = type.GetTypeInfo()).IsGenericType && (isNullableType = tf.GetGenericTypeDefinition() == nullableType) && type.GenericTypeArguments[0] == intType))
            {
                int rst;
                if (int.TryParse(str, out rst))
                {
                    rstObj = rst;
                    return true;
                }
            }
            if (type == decimalType || ((tf = type.GetTypeInfo()).IsGenericType && (isNullableType = tf.GetGenericTypeDefinition() == nullableType) && type.GenericTypeArguments[0] == decimalType))
            {
                decimal rst;
                if (decimal.TryParse(str, out rst))
                {
                    rstObj = rst;
                    return true;
                }
            }
            if (type == floatType || ((tf = type.GetTypeInfo()).IsGenericType && (isNullableType = tf.GetGenericTypeDefinition() == nullableType) && type.GenericTypeArguments[0] == floatType))
            {
                float rst;
                if (float.TryParse(str, out rst))
                {
                    rstObj = rst;
                    return true;
                }
            }
            if (type == doubleType || ((tf = type.GetTypeInfo()).IsGenericType && (isNullableType = tf.GetGenericTypeDefinition() == nullableType) && type.GenericTypeArguments[0] == doubleType))
            {
                double rst;
                if (double.TryParse(str, out rst))
                {
                    rstObj = rst;
                    return true;
                }
            }
            if (type == shortType || ((tf = type.GetTypeInfo()).IsGenericType && (isNullableType = tf.GetGenericTypeDefinition() == nullableType) && type.GenericTypeArguments[0] == shortType))
            {
                short rst;
                if (short.TryParse(str, out rst))
                {
                    rstObj = rst;
                    return true;
                }
            }
            if (type == longType || ((tf = type.GetTypeInfo()).IsGenericType && (isNullableType = tf.GetGenericTypeDefinition() == nullableType) && type.GenericTypeArguments[0] == longType))
            {
                long rst;
                if (long.TryParse(str, out rst))
                {
                    rstObj = rst;
                    return true;
                }
            }
            if (type == boolType || ((tf = type.GetTypeInfo()).IsGenericType && (isNullableType = tf.GetGenericTypeDefinition() == nullableType) && type.GenericTypeArguments[0] == boolType))
            {
                bool rst;
                if (bool.TryParse(str, out rst))
                {
                    rstObj = rst;
                    return true;
                }
            }
            if (type == sbyteType || ((tf = type.GetTypeInfo()).IsGenericType && (isNullableType = tf.GetGenericTypeDefinition() == nullableType) && type.GenericTypeArguments[0] == sbyteType))
            {
                sbyte rst;
                if (sbyte.TryParse(str, out rst))
                {
                    rstObj = rst;
                    return true;
                }
            }
            if (type == byteType || ((tf = type.GetTypeInfo()).IsGenericType && (isNullableType = tf.GetGenericTypeDefinition() == nullableType) && type.GenericTypeArguments[0] == byteType))
            {
                byte rst;
                if (byte.TryParse(str, out rst))
                {
                    rstObj = rst;
                    return true;
                }
            }
            if (type == ushortType || ((tf = type.GetTypeInfo()).IsGenericType && (isNullableType = tf.GetGenericTypeDefinition() == nullableType) && type.GenericTypeArguments[0] == ushortType))
            {
                ushort rst;
                if (ushort.TryParse(str, out rst))
                {
                    rstObj = rst;
                    return true;
                }
            }
            if (type == uintType || ((tf = type.GetTypeInfo()).IsGenericType && (isNullableType = tf.GetGenericTypeDefinition() == nullableType) && type.GenericTypeArguments[0] == uintType))
            {
                uint rst;
                if (uint.TryParse(str, out rst))
                {
                    rstObj = rst;
                    return true;
                }
            }
            if (type == ulongType || ((tf = type.GetTypeInfo()).IsGenericType && (isNullableType = tf.GetGenericTypeDefinition() == nullableType) && type.GenericTypeArguments[0] == ulongType))
            {
                ulong rst;
                if (ulong.TryParse(str, out rst))
                {
                    rstObj = rst;
                    return true;
                }
            }
            rstObj = null;
            return false;
        }


        public void LoadConfig()
        {
            var ns = GetIocNodes(xml);
            if (ns != null)
            {
                foreach (XmlNode n in ns)
                {
                    RegTypeNode(n.Name, n.ChildNodes);
                }
            }
        }
        private void RegTypeNode(string typeName, XmlNodeList nodes)
        {
            if (nodes == null || nodes.Count == 0) return;
            var type = FindType(typeName);
            if (type == null)
                throw new TypeNotFoundException(typeName);
            foreach (XmlNode node in nodes)
            {
                try
                {
                    RegType(type, node);
                }
                catch (Exception ex)
                {

                }
            }

        }

        private void RegType(Type iType, XmlNode node)
        {
            if (node == null) return;
            bool isSingleton = false;
            bool isPerThread = false;
            var atts = node.Attributes;
            if (atts != null && atts.Count > 0)
            {
                foreach (XmlAttribute att in atts)
                {
                    if (att.Name.Trim().ToLower() == "singleton" && att.Value.ToLower() == "true")
                    {
                        isSingleton = true;
                    }
                    else if (att.Name.Trim().ToLower() == "perthread" && att.Value.ToLower() == "true")
                    {
                        isPerThread = true;
                    }
                    if (isPerThread && isSingleton)
                        break;
                }
            }
            var nodes = node.ChildNodes;
            string regName = null;
            string type = null;
            string args = null;
            if (nodes != null && nodes.Count > 0)
            {
                string nodeName, val;
                foreach (XmlNode n in nodes)
                {
                    nodeName = n.Name.ToLower().Trim();
                    val = n.InnerText.Trim();
                    if (nodeName == "name")
                    {
                        if (!string.IsNullOrWhiteSpace(val))
                            regName = val;
                    }
                    else if (nodeName == "type")
                    {
                        if (!string.IsNullOrWhiteSpace(val))
                            type = val;
                    }
                    else if (nodeName == "args")
                    {
                        if (!string.IsNullOrWhiteSpace(val))
                            args = val;
                    }
                }
            }
            if (type != null)
            {
                RegType(iType, type, regName, isSingleton, isPerThread, args);
            }
        }

        private void RegType(Type iType, string typeName, string regName, bool isSingleton, bool isPerThread, string args)
        {
            var moduleType = FindType(typeName);
            if (moduleType == null)
                throw new TypeNotFoundException(typeName);

            RegContainerType(iType, moduleType, regName, isSingleton, isPerThread, args);
        }

        private Type FindType(string typeName)
        {
            {
                var dlls = System.IO.Directory.GetFiles(binPath, "*.dll", System.IO.SearchOption.TopDirectoryOnly);
                if (dlls != null && dlls.Length > 0)
                {
                    foreach (var dll in dlls)
                    {
                        try
                        {
                            var ass = AssemblyLoadContext.Default.LoadFromAssemblyPath(dll);
                            var type = ass.GetType(typeName, false, false);
                            if (type != null)
                                return type;
                        }
                        catch (Exception ex)
                        {

                        }
                    }
                }
            }
            {

                var ass = Assembly.GetEntryAssembly();
                var type = ass.GetType(typeName, false, false);
                if (type != null)
                    return type;

            }
            return null;
        }

        private XmlNodeList GetIocNodes(string xml)
        {
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(File.ReadAllText(xml));
            var cfgs = doc.ChildNodes;
            XmlNode root = null;
            if (cfgs != null && cfgs.Count == 2)
            {
                var cs = cfgs[1].ChildNodes;
                if (cs != null && cs.Count > 0)
                {
                    foreach (XmlNode c in cs)
                    {
                        if (c.Name.ToLower() == "ioc")
                        {
                            root = c;
                            break;
                        }
                    }
                }
            }
            if (root != null)
            {
                return root.ChildNodes;
            }
            return null;
        }
    }
}
