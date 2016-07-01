using System;
using System.Collections.Generic;
using System.Threading;

/// <summary>
/// no noe无名之辈,我是权力游戏控
/// </summary>
namespace Noone
{
    /// <summary>
    /// IoC容器
    /// </summary>
    public class Container
    {
        private static volatile int currCid = -1;
        private int cid;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="cfg">配置文件，默认为启动目录下"cfg.xml"</param>
        public Container(string cfg = "cfg.xml")
        {
            cid = Interlocked.Increment(ref currCid);
        }
        /// <summary>
        ///  注册
        /// </summary>
        /// <typeparam name="F">接口或父类</typeparam>
        /// <typeparam name="S">继承类</typeparam>
        /// <param name="name">索引名称,默认为空</param>
        public void Register<F, S>(string name = null) where S : F, new() where F : class
        {
            if (name == null)
                Factory<F>.GetFactory(cid).Reg<S>();
            else
                Factory<F>.GetFactory(cid).Reg<S>(name);
        }
        /// <summary>
        /// 注册单例
        /// </summary>
        /// <typeparam name="F">接口或父类</typeparam>
        /// <typeparam name="S">继承类</typeparam>
        /// <param name="name"></param>
        /// <param name="name">索引名称,默认为空</param>
        public void RegisterSingleton<F, S>(string name = null) where S : F, new() where F : class
        {
            if (name == null)
                Factory<F>.GetFactory(cid).RegSingleton<S>();
            else
                Factory<F>.GetFactory(cid).RegSingleton<S>(name);
        }
        /// <summary>
        /// 注册,对象由传入的Func委托创建
        /// </summary>
        /// <typeparam name="F">接口或父类</typeparam>
        /// <param name="func">对象创建委托</param>
        /// <param name="name">索引名称,默认为空</param>
        public void Register<F>(Func<F> func, string name = null) where F : class
        {
            if (name == null)
                Factory<F>.GetFactory(cid).Reg(func);
            else
                Factory<F>.GetFactory(cid).Reg(func, name);
        }
        /// <summary>
        /// 注册单例,对象由传入的Func委托创建
        /// </summary>
        /// <typeparam name="F">接口或父类</typeparam>
        /// <param name="func">对象创建委托</param>
        /// <param name="name">索引名称,默认为空</param>
        public void RegisterSingleton<F>(Func<F> func, string name = null) where F : class
        {
            if (name == null)
                Factory<F>.GetFactory(cid).RegSingleton(func);
            else
                Factory<F>.GetFactory(cid).RegSingleton(func, name);
        }
        /// <summary>
        /// 获取
        /// </summary>
        /// <typeparam name="F">接口或父类</typeparam>
        /// <returns>注册的继承类</returns>
        public F Resolve<F>() where F : class
        {
            return Factory<F>.GetFactory(cid).Get();
        }
        /// <summary>
        /// 获取
        /// </summary>
        /// <typeparam name="F">接口或父类</typeparam>
        /// <param name="name">索引名称</param>
        /// <returns>注册的继承类</returns>
        public F Resolve<F>(string name) where F : class
        {
            return Factory<F>.GetFactory(cid).Get(name);
        }
        /// <summary>
        /// 取出当前所有注册的列表
        /// </summary>
        /// <typeparam name="F">接口或父类</typeparam>
        /// <returns>索引名称列表,null表示无索引注册</returns>
        public IList<string> GetRegisterList<F>() where F : class
        {
            return Factory<F>.GetFactory(cid).GetRegisterList();
        }
    }
}
