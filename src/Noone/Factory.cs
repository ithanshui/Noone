using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Noone
{
    public class Factory<T> where T : class
    {
        #region 空间换性能
        private static readonly Factory<T> instance0 = new Factory<T>(0);
        private static readonly Factory<T> instance1 = new Factory<T>(1);
        private static readonly ConcurrentDictionary<int, Factory<T>> instances = new ConcurrentDictionary<int, Factory<T>>();
        private static Func<int, Factory<T>> newFunc = (cid) => { return new Factory<T>(cid); };
        public static Factory<T> GetFactory(int id)
        {
            if (id == 0) return instance0;
            if (id == 1) return instance1;
            return instances.GetOrAdd(id, newFunc);
        }
        #endregion

        protected int Id;
        public Factory(int id)
        {
            Id = id;
        }
        #region Creaters
        public interface ICreater
        {
            T Create();
        }
        class Creater<U> : ICreater where U : class, T, new()
        {
            public T Create()
            {
                return FastActivator<U>.CreateInstance();
                //return new U();
            }
        }
        class FuncCreater : ICreater
        {
            private Func<T> func;
            public FuncCreater(Func<T> func)
            {
                this.func = func;
            }
            public T Create()
            {
                return func();
            }
        }

        class SingletonCreater<U> : ICreater where U : class, T, new()
        {
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
                            Interlocked.Exchange(ref instance, FastActivator<U>.CreateInstance());
                            //Interlocked.Exchange(ref instance, new U());
                        }
                    }
                }
                return instance;
            }
        }
        class SingletonPerThreadCreater<U> : ICreater where U : class, T, new()
        {
            //由于整个IoC容器不是静态的,所以不能用内部类static readonly魔法来搞,否则可能会出现多个索引名称注册了单例子,但引用了同一个对象,多个索引名称变成了别名的情况,只能用双检锁了
            private object locker = new object();

            private ThreadLocal<T> instance = new ThreadLocal<T>();
            public T Create()
            {
                var rst = instance.Value;
                if (rst == null)
                {
                    lock (locker)
                    {
                        if ((rst = instance.Value) == null)
                        {
                            //rst = new U();
                            rst = FastActivator<U>.CreateInstance();
                            instance.Value = rst;
                            return rst;
                            //Interlocked.Exchange(ref instance.Value, new U());
                        }
                        else
                            return rst;
                    }
                }
                else
                    return rst;
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
        class FuncSingletonPerThreadCreater : ICreater
        {
            private Func<T> func;
            public FuncSingletonPerThreadCreater(Func<T> func)
            {
                this.func = func;
            }
            //由于整个IoC容器不是静态的,所以不能用内部类static readonly魔法来搞,否则可能会出现多个索引名称注册了单例子,但引用了同一个对象,多个索引名称变成了别名的情况,只能用双检锁了
            private object locker = new object();
            private ThreadLocal<T> instance = new ThreadLocal<T>();
            public T Create()
            {
                var rst = instance.Value;
                if (rst == null)
                {
                    lock (locker)
                    {
                        if ((rst = instance.Value) == null)
                        {
                            rst = func();
                            instance.Value = rst;
                            return rst;
                            //Interlocked.Exchange(ref instance, func());
                        }
                        else
                            return rst;
                    }
                }
                else
                    return rst;
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
        public void Reg<S>() where S : class, T, new()
        {
            creater = new Creater<S>();
            AddReg(null);
        }
        public void RegSingleton<S>(bool isPerThread) where S : class, T, new()
        {
            if (isPerThread)
                creater = new SingletonPerThreadCreater<S>();
            else
                creater = new SingletonCreater<S>();
            AddReg(null);
        }
        public void Reg(Func<T> func)
        {
            creater = new FuncCreater(func);
            AddReg(null);
        }
        public void RegSingleton(Func<T> func, bool isPerThread)
        {
            if (isPerThread)
                creater = new FuncSingletonPerThreadCreater(func);
            else
                creater = new FuncSingletonCreater(func);
            AddReg(null);
        }
        #endregion

        #region 有索引的
        //private IDictionary<string, ICreater> creaters = new ConcurrentDictionary<string, ICreater>();
        private FastDic<ICreater> creaters = new FastDic<ICreater>();
        public T Get(string key)
        {
            //if (creaters.TryGetValue(key, out ct))
            //    return ct.Create();

            ICreater ct = creaters.Get(key);
            if (ct == null)
                throw new Exception("未注册");
            else
                return ct.Create();
        }
        public void Reg<S>(string key) where S : class, T, new()
        {
            creaters.Add(key, new Creater<S>());
            //creaters[key] = new Creater<S>();
            AddReg(key);
        }
        public void RegSingleton<S>(string key, bool isPerThread) where S : class, T, new()
        {
            if (isPerThread)
                creaters.Add(key, new SingletonPerThreadCreater<S>());
            else
                creaters.Add(key, new SingletonCreater<S>());
            //creaters[key] = new SingletonCreater<S>();
            AddReg(key);
        }
        public void Reg(Func<T> func, string key)
        {
            creaters.Add(key, new FuncCreater(func));
            //creaters[key] = new FuncCreater(func);
            AddReg(key);
        }
        public void RegSingleton(Func<T> func, string key, bool isPerThread)
        {
            if (isPerThread)
                creaters.Add(key, new FuncSingletonPerThreadCreater(func));
            else
                creaters.Add(key, new FuncSingletonCreater(func));
            //creaters[key] = new FuncSingletonCreater(func);
            AddReg(key);
        }
        #endregion
    }
}
