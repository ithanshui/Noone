using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Noone
{
    internal class Factory<T> where T : class
    {
        #region 空间换性能
        private static readonly Factory<T> instance0 = new Factory<T>();
        private static readonly Factory<T> instance1 = new Factory<T>();
        private static readonly ConcurrentDictionary<int, Factory<T>> instances = new ConcurrentDictionary<int, Factory<T>>();
        private static Func<int, Factory<T>> newFunc = (cid) => { return new Factory<T>(); };
        public static Factory<T> GetFactory(int id)
        {
            if (id == 0) return instance0;
            if (id == 1) return instance1;
            return instances.GetOrAdd(id, newFunc);
        }
        #endregion

        #region Creaters
        interface ICreater
        {
            T Create();
        }
        class Creater<U> : ICreater where U : T, new()
        {
            public T Create()
            {
                return new U();
            }
        }
        class FuncCreater : ICreater
        {
            Func<T> func;
            public FuncCreater(Func<T> func)
            {
                this.func = func;
            }
            public T Create()
            {
                return func();
            }
        }
        class MagicSingletonCreater<U> : ICreater where U : T, new()
        {
            class InstanceClass
            {
                public static readonly T Instance = new U();
            }
            public T Create()
            {
                return InstanceClass.Instance;
            }
        }
        class SingletonCreater<U> : ICreater where U : T, new()
        {
            //由于整个IoC容器不是静态的,所以不能用内部类static readonly魔法来搞,否则可能会出现多个索引名称注册了单例子,但引用了同一个对象,多个索引名称变成了别名的情况,只能用双检锁了
            object locker = new object();
            T instance;
            public T Create()
            {
                if (instance == null)
                {
                    lock (locker)
                    {
                        if (instance == null)
                        {
                            Interlocked.Exchange(ref instance, new U());
                        }
                    }
                }
                return instance;
            }
        }
        class FuncSingletonCreater : ICreater
        {
            Func<T> func;
            public FuncSingletonCreater(Func<T> func)
            {
                this.func = func;
            }
            //由于整个IoC容器不是静态的,所以不能用内部类static readonly魔法来搞,否则可能会出现多个索引名称注册了单例子,但引用了同一个对象,多个索引名称变成了别名的情况,只能用双检锁了
            private object locker = new object();
            private T instance;
            public T Create()
            {
                if (instance == null)
                {
                    lock (locker)
                    {
                        if (instance == null)
                        {
                            Interlocked.Exchange(ref instance, func());
                        }
                    }
                }
                return instance;
            }
        }
        class MagicFuncSingletonCreater<S> : ICreater where S : T
        {
            static Func<S> magicFunc;
            public MagicFuncSingletonCreater(Func<S> func)
            {
                magicFunc = func;
            }
            class InstanceClass
            {
                public static readonly S Instance = magicFunc();
            }
            public T Create()
            {
                return InstanceClass.Instance;
            }
        }
        #endregion

        ConcurrentBag<string> regs = new ConcurrentBag<string>();
        public IList<string> GetRegisterList()
        {
            return regs.ToList();
        }
        private void AddReg(string name)
        {
            if (regs.Contains(name)) return;
            regs.Add(name);
        }
        #region 无索引的
        private ICreater creater;
        public T Get()
        {
            return creater.Create();
        }
        public void Reg<S>() where S : T, new()
        {
            creater = new Creater<S>();
            AddReg(null);
        }
        public void RegSingleton<S>() where S : T, new()
        {
            creater = new MagicSingletonCreater<S>();
            AddReg(null);
        }
        public void Reg(Func<T> func)
        {
            creater = new FuncCreater(func);
            AddReg(null);
        }
        public void RegSingleton<S>(Func<S> func) where S : T
        {
            creater = new MagicFuncSingletonCreater<S>(func);
            AddReg(null);
        }
        #endregion

        #region 有索引的
        private IDictionary<string, ICreater> creaters = new ConcurrentDictionary<string, ICreater>();
        public T Get(string key)
        {
            ICreater ct;
            if (creaters.TryGetValue(key, out ct))
                return ct.Create();
            throw new Exception("未注册");
        }
        public void Reg<S>(string key) where S : T, new()
        {
            creaters[key] = new Creater<S>();
            AddReg(key);
        }
        public void RegSingleton<S>(string key) where S : T, new()
        {
            creaters[key] = new SingletonCreater<S>();
            AddReg(key);
        }
        public void Reg(Func<T> func, string key)
        {
            creaters[key] = new FuncCreater(func);
            AddReg(key);
        }
        public void RegSingleton(Func<T> func, string key)
        {
            creaters[key] = new FuncSingletonCreater(func);
            AddReg(key);
        }
        #endregion
    }
}
