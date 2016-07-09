using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Reflection;
using System.Reflection.Emit;
using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

namespace Noone.Test
{

    public class Program
    {
        public static void Main(string[] args)
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            var sw = new Stopwatch();
            Console.WriteLine("请输入循环次数");
            int max = int.Parse(Console.ReadLine());

            /*
            FastDic<object> dic = new FastDic<object>();

            var orgDic = new Dictionary<string, object>();
            for (var i = 0; i < 10; i++)
            {
                orgDic["key_" + i] = "value" + i;
                dic.Add("key_" + i, "value" + i);
            }

            foreach (var k in orgDic)
            {
                Console.WriteLine("{0}原字典结果:{1}", k.Key, k.Value);
                Console.WriteLine("{0}FastDic结果:{1}", k.Key, dic.Get(k.Key));
            }



            string key = "key_0";
            sw.Start();
            for (var i = 0; i < max; i++)
            {
                var x = orgDic[key];
            }

            sw.Stop();
            Console.WriteLine("原字典结果{0}ms,平均每次{1}ns", sw.ElapsedMilliseconds, sw.ElapsedMilliseconds * 1000000M / (decimal)max);
            sw.Restart();
            for (var i = 0; i < max; i++)
            {
                var x = dic.Get(key);
            }

            sw.Stop();
            Console.WriteLine("FastDic耗时{0}ms,平均每次{1}ns", sw.ElapsedMilliseconds, sw.ElapsedMilliseconds * 1000000M / (decimal)max);


            Console.ReadLine();
            */
            var ctx = new Container();
            
            //var mc = new ServiceDescriptor(typeof(ISMS),typeof(XSMS), ServiceLifetime.Transient);
            var serviceCollection = new ServiceCollection();
            
            serviceCollection.AddTransient<ISMS,XSMS>();
            serviceCollection.AddSingleton<ISMS2, SMS2>();

            var mc = serviceCollection.BuildServiceProvider();
            
            mc.GetService<ISMS>();
            ctx.Resolve<ISMS>();
            ctx.Resolve<ISMS>("alidayu");
            //var xc = mc.GetService<ISMS>();
            //ctx.RegisterSingleton<ISMS, XSMS>();
            //ctx.Register<ISMS, FriendSMS>("fsms");

            //Console.WriteLine("M$");

            //var cs = ctx.GetRegisterList<ISMS>();
            //foreach (var c in cs)
            //{
            //    Console.WriteLine("ctx ISMS注册：" + c);
            //}

            sw.Restart();
            for (var i = 0; i < max; i++)
            {
                var x = ctx.Resolve<ISMS>();
                //x.Send(null, 0, null, null);
            }
            sw.Stop();
            Console.WriteLine("IoC Transient 耗时{0}ms,平均每次{1}ns", sw.ElapsedMilliseconds, sw.ElapsedMilliseconds * 1000000M / (decimal)max);


            sw.Restart();
            for (var i = 0; i < max; i++)
            {
                var x = ctx.Resolve<ISMS>("alidayu");
                //x.Send(null, 0, null, null);
            }
            sw.Stop();
            Console.WriteLine("IoC(索引) Transient 耗时{0}ms,平均每次{1}ns", sw.ElapsedMilliseconds, sw.ElapsedMilliseconds * 1000000M / (decimal)max);
            sw.Restart();
            for (var i = 0; i < max; i++)
            {
                var x = mc.GetService<ISMS>();
                //var x = new XSMS();
                //int id = Thread.CurrentThread.ManagedThreadId;
                //x.Send(null, 0, null, null);
            }
            sw.Stop();
            Console.WriteLine("asp.net core DependencyInjection Transient耗时{0}ms,平均每次{1}ns", sw.ElapsedMilliseconds, sw.ElapsedMilliseconds * 1000000M / (decimal)max);
            Console.ReadLine();
        }
        
    }

    public interface ISMS
    {
        void Init(string key, string pwd);
        void Send(string msg, long phone, string client, string tpl);
    }

    public interface ISMS2
    {
    }
    public class SMS2 : ISMS2
    {

    }

    public abstract class BaseSMS : ISMS
    {
        public virtual void Init(string key, string pwd)
        {
            throw new NotImplementedException();
        }

        public virtual void Send(string msg, long phone, string client, string tpl)
        {
            throw new NotImplementedException();
        }
        protected string FormatTpl(string tpl, string msg, long phoneNumber, string client, string sign)
        {
            return tpl.Replace("$sign", sign).Replace("$msg", msg).Replace("$client", client);
        }
    }
    public class AlidayuSMS : BaseSMS
    {
        public AlidayuSMS(string code, int? id, string pwd)
        {
            //Console.WriteLine(pwd);
        }

        public AlidayuSMS()
        {
            //Console.WriteLine("AlidayuSMS创建");
        }
        private string sign;
        public override void Init(string key, string pwd)
        {
            sign = "什么都有的电商公司";
        }
        public override void Send(string msg, long phone, string client, string tpl)
        {
            //签名等等把短信发到XSMS公司的接口上
            //XXX的复杂发送代码
            //Console.WriteLine("从阿里大鱼发短信:" + FormatTpl(tpl, msg, phone, client, sign));
        }
    }
    public class XSMS : BaseSMS
    {
        public XSMS()
        {
            //Console.WriteLine("XSMS创建");
        }
        private string sign;
        public override void Init(string key, string pwd)
        {
            sign = "什么都有的电商公司";
        }
        public override void Send(string msg, long phone, string client, string tpl)
        {
            //签名等等把短信发到XSMS公司的接口上
            //XXX的复杂发送代码
            //Console.WriteLine("从XSMS发短信:" + FormatTpl(tpl, msg, phone, client, sign));
        }
    }
    public class FriendSMS : BaseSMS
    {
        public FriendSMS()
        {
            //Console.WriteLine("FriendSMS创建");
        }
        private string sign;
        public override void Init(string key, string pwd)
        {
            sign = "什么都有的电商公司";
        }
        public override void Send(string msg, long phone, string client, string tpl)
        {
            //签名等等把短信发到老板朋友公司提供的接口上
            //XXX的复杂发送代码
            //Console.WriteLine("从FriendSMS发短信:" + FormatTpl(tpl, msg, phone, client, sign));
        }
    }
}
