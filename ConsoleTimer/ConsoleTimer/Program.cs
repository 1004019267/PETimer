using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
class TaskPack
{
    public int tid;
    public Action<int> cb;

    public TaskPack(int tid, Action<int> cb)
    {
        this.tid = tid;
        this.cb = cb;
    }
}

class Program
{
    static readonly string obj = "lock";
    static void Main(string[] args)
    {
        //Test1();
        //TimerTest();
        Test2();
        //阻塞让cw显示出来
        Console.ReadKey();
    }
    /// <summary>
    /// 在独立线程检测并处理
    /// </summary>
    private static void Test2()
    {
        //任务包 防止异步同时计时
        Queue<TaskPack> tpQue = new Queue<TaskPack>();
        PETimer pt = new PETimer(50);

        int id = pt.AddTimeTask((int tid) =>
          {
              Console.WriteLine($"Time:{DateTime.Now}");
              Console.WriteLine($"Process线程ID：{Thread.CurrentThread.ManagedThreadId.ToString()}");
          }, 1000, 0);

        pt.SetLog((string info) =>
        {
            Console.WriteLine($"ConsoleLog{info}");
        });
        //pt.SetHandle((Action<int> cb, int tid) =>
        //{
        //    if (cb != null)
        //    {
        //        lock (obj)
        //            tpQue.Enqueue(new TaskPack(tid, cb));
        //    }
        //});
        while (true)
        {
            //输入d测试删除
            string ipt = Console.ReadLine();
            if (ipt=="d")
            {
                pt.DeleteTimeTask(id);
            }
            if (tpQue.Count > 0)
            {
                TaskPack tp;
                lock (obj)
                    tp = tpQue.Dequeue();
                tp.cb(tp.tid);
            }
        }
    }

    private static void TimerTest()
    {
        //不声明会有二义性 每隔50毫秒循环一次 看线程ID 里面封装有一个线程池 看谁空闲调用谁
        System.Timers.Timer t = new System.Timers.Timer(50);
        t.AutoReset = true;//可以一直触发事件
        t.Elapsed += (object sender, ElapsedEventArgs arg) =>
        {
            Console.WriteLine($"Time:{DateTime.Now}");
            Console.WriteLine($"Process线程ID：{Thread.CurrentThread.ManagedThreadId.ToString()}");
        };
        t.Start();
    }
    /// <summary>
    /// 在主线程检测并处理
    /// </summary>
    private static void Test1()
    {
        PETimer pt = new PETimer();
        pt.SetLog((string info) =>
        {
            Console.WriteLine($"ConsoleLog{info}");
        });

        pt.AddTimeTask((int tid) =>
        {
            Console.WriteLine($"Time:{DateTime.Now}");
            Console.WriteLine($"Process线程ID：{Thread.CurrentThread.ManagedThreadId.ToString()}");
        }, 1000, 0);

        while (true)
        {
            pt.Update();
            //休眠20毫秒 不然CPU占用率高
            Thread.Sleep(20);
        }
    }
}

