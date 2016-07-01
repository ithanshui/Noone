using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Reflection;
using System.Reflection.Emit;
using System.Diagnostics;

namespace Noone.Test
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            var ctx = new Container();

            ctx.RegisterSingleton<ISMS, XSMS>();
            ctx.Register<ISMS, FriendSMS>("fsms");

            var cs = ctx.GetRegisterList<ISMS>();
            foreach (var c in cs)
            {
                //Console.WriteLine("ctx ISMS注册：" + c);
            }
            
            Console.WriteLine("请输入循环次数");
            int max = int.Parse(Console.ReadLine());
            var sw = new Stopwatch();
            sw.Start();
            for (var i = 0; i < max; i++)
            {
                var x = ctx.Resolve<ISMS>();
                x.Send(null, 0, null, null);
            }
            sw.Stop();
            Console.WriteLine("IoC单例耗时{0}ms,平均每次{1}ns", sw.ElapsedMilliseconds, sw.ElapsedMilliseconds * 1000000M / (decimal)max);
            
            var ctx2 = new Container();
            ctx2.Register<ISMS, AlidayuSMS>();
            ctx2.RegisterSingleton<ISMS, XSMS>("fsms");

            sw.Restart();
            for (var i = 0; i < max; i++)
            {
                var x = ctx2.Resolve<ISMS>();
                x.Send(null, 0, null, null);
            }
            sw.Stop();
            Console.WriteLine("IoC创建耗时{0}ms,平均每次{1}ns", sw.ElapsedMilliseconds, sw.ElapsedMilliseconds * 1000000M / (decimal)max);
            sw.Restart();
            for (var i = 0; i < max; i++)
            {
                var x = new XSMS();
                x.Send(null, 0, null, null);
            }
            sw.Stop();
            Console.WriteLine("直接创建耗时{0}ms,平均每次{1}ns", sw.ElapsedMilliseconds, sw.ElapsedMilliseconds * 1000000M / (decimal)max);
            Console.ReadLine();
        }
    }

    public interface ISMS
    {
        void Init(string key, string pwd);
        void Send(string msg, long phone, string client, string tpl);
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
