using System.Collections.Generic;
using System;
using System.Reflection;

public class BasePETask
{
    public int tid;
    public Action callBack;
    public int count;//次数

    public BasePETask(int tid, Action callBack, int count)
    {
        this.tid = tid;
        this.callBack = callBack;
        this.count = count;
    }
}

public class PETimeTask : BasePETask
{
    public double destTime;//单位:毫秒
    public double delay;
    public PETimeTask(int tid, double destTime, Action callBack, double delay, int count):base(tid,callBack,count)
    {
        this.destTime = destTime;
        this.delay = delay;
    }
}

public class PEFrameTask : BasePETask
{
    public int destFrame;//单位:秒
    public int delay;
    public PEFrameTask(int tid, int destFrame, Action callBack, int delay, int count) : base(tid, callBack, count)
    {
        this.destFrame = destFrame; 
        this.delay = delay;
    }
}

public enum EPETimeUnit
{
    Millisecond = 0,
    Second,
    Minute,
    Hour,
    Day
}


/// <summary>
/// 支持时间定时，帧定时
/// 定时任务可循环 取消 替换
/// </summary>
public class PETimer
{
    Action<string> taskLog;
    //声明锁
    static readonly string obj = "lock";
    //C#的计时 计算机元年
    DateTime startDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0);
    double nowTime;
    int tid;
    List<int> tids = new List<int>();
    /// <summary>
    /// tid缓存回收
    /// </summary>
    List<int> recTids = new List<int>();
    /// <summary>
    /// 临时列表 支持多线程操作 错开时间操作 避免使用锁 提升操作效率 
    /// </summary>   
    List<PETimeTask> tmpTimes = new List<PETimeTask>();
    List<PETimeTask> taskTimes = new List<PETimeTask>();

    int frameCounter;
    List<PEFrameTask> tmpFrames = new List<PEFrameTask>();
    List<PEFrameTask> taskFrames = new List<PEFrameTask>();



    public void Update()
    {
        CheckTask(taskTimes, tmpTimes);
        CheckTask(taskFrames, tmpFrames);
        RecycleTid();
    }
    void CheckTask<T>(List<T> tasks, List<T> tmps) where T : BasePETask
    {
        //加入缓存区中的定时任务
        for (int i = 0; i < tmps.Count; i++)
        {
            tasks.Add(tmps[i]);
        }

        tmps.Clear();

        if (typeof(T) == typeof(PETimeTask))
        {
            nowTime = GetUTCMilliseconds();
        }
        else if (typeof(T) == typeof(PEFrameTask))
        {
            frameCounter += 1;
        }

        //遍历检测任务是否到达条件
        for (int i = 0; i < tasks.Count; i++)
        {
            var task= tasks[i];          
            bool isTimeUp = false;
            
            if (typeof(T) == typeof(PETimeTask))
            {          
                isTimeUp = nowTime.CompareTo((task as PETimeTask).destTime) > 0;
              
            }
            else if (typeof(T) == typeof(PEFrameTask))
            {
                isTimeUp = frameCounter.CompareTo((task as PEFrameTask).destFrame) > 0;
                
            }
            //nowTime>task.destTime 1 nowTime<task.destTime -1 nowTime=task.destTime 0
            if (isTimeUp)
            {
                LogInfo("满足");
                try
                {
                    //时间到 callBack不为空调用
                    task.callBack?.Invoke();
                }
                catch (Exception e)
                {
                    LogInfo(e.ToString());
                }

                if (task.count == 1)
                {
                    tasks.RemoveAt(i);
                    i--;
                    recTids.Add(task.tid);
                }
                else
                {
                    if (task.count != 0)
                    {
                        task.count -= 1;
                    }
                    //重新赋值时间
                    if (typeof(T) == typeof(PETimeTask))
                    {
                        (task as PETimeTask).destTime += (task as PETimeTask).delay;
                    }
                    else if (typeof(T) == typeof(PEFrameTask))
                    {
                        (task as PEFrameTask).destFrame += (task as PEFrameTask).delay;
                    }                 
                }
            }

        }
    }

   
    #region TimeTask
    /// <summary>
    /// 添加一个计时器
    /// </summary>
    /// <param name="callBack"></param>
    /// <param name="delay"></param>
    /// <param name="count"></param>
    /// <param name="timeUnit"></param>
    /// <returns></returns>
    public int AddTimeTask(Action callBack, float delay, int count = 1, EPETimeUnit timeUnit = EPETimeUnit.Millisecond)
    {
        int tid = GetTid();

        ChangeTimeWithType(ref delay, timeUnit);
        //从游戏开始到现在的时间
        nowTime = GetUTCMilliseconds();

        tmpTimes.Add(new PETimeTask(tid, nowTime + delay, callBack, delay, count));
        return tid;
    }

    /// <summary>
    /// 移除一个计时器
    /// </summary>
    /// <param name="tid"></param>
    /// <returns></returns>
    public bool DeleteTimeTask(int tid)
    {
        return DeleteTask(tid, taskTimes, tmpTimes);
    }

    /// <summary>
    /// 替换
    /// </summary>
    /// <param name="tid"></param>
    /// <param name="callBack"></param>
    /// <param name="delay"></param>
    /// <param name="count"></param>
    /// <param name="timeUnit"></param>
    /// <returns></returns>
    public bool ReplaceTimeTask(int tid, Action callBack, float delay, int count = 1, EPETimeUnit timeUnit = EPETimeUnit.Millisecond)
    {
        ChangeTimeWithType(ref delay, timeUnit);
        //从游戏开始到现在的时间
        nowTime = GetUTCMilliseconds();
        PETimeTask newTask = new PETimeTask(tid, nowTime + delay, callBack, delay, count);
        return ReplaceTask<PETimeTask>(tid, taskTimes, tmpTimes, newTask);
    }

    #endregion
    #region FrameTask

    /// <summary>
    /// 添加一个帧计时器
    /// </summary>
    /// <param name="callBack"></param>
    /// <param name="delay"></param>
    /// <param name="count"></param>
    /// <param name="timeUnit"></param>
    /// <returns></returns>
    public int AddFrameTask(Action callBack, int delay, int count = 1)
    {
        int tid = GetTid();
        taskFrames.Add(new PEFrameTask(tid, frameCounter + delay, callBack, delay, count));

        return tid;
    }
    /// <summary>
    /// 移除一个帧计时器
    /// </summary>
    /// <param name="tid"></param>
    /// <returns></returns>
    public bool DeleteFrameTask(int tid)
    {
        return DeleteTask(tid, taskFrames, tmpFrames); ;
    }
    /// <summary>
    /// 替换帧计时器
    /// </summary>
    /// <param name="tid"></param>
    /// <param name="callBack"></param>
    /// <param name="delay"></param>
    /// <param name="count"></param>
    /// <param name="timeUnit"></param>
    /// <returns></returns>
    public bool ReplaceFrameTask(int tid, Action callBack, int delay, int count = 1)
    {
        PEFrameTask newTask = new PEFrameTask(tid, frameCounter + delay, callBack, delay, count);
        return ReplaceTask<PEFrameTask>(tid, taskFrames, tmpFrames, newTask);
    }
    #endregion
    /// <summary>
    /// 清空数据
    /// </summary>
    public void Destory()
    {
        tids.Clear();
        recTids.Clear();

        tmpTimes.Clear();
        taskTimes.Clear();

        tmpFrames.Clear();
        taskFrames.Clear();

        tid = 0;
        taskLog = null;
    }
    public void SetLog(Action<string> log)
    {
        taskLog = log;
    }
    #region Tool Methonds
    bool DeleteTask<T>(int tid, List<T> tasks, List<T> tmps) where T : BasePETask
    {

        bool exist = false;

        for (int i = 0; i < tasks.Count; i++)
        {
            T task = tasks[i];

            if (task.tid == tid)
            {
                tasks.RemoveAt(i);
                for (int j = 0; j < tids.Count; j++)
                {
                    if (tids[j] == tid)
                    {
                        recTids.Add(tid);
                        break;
                    }
                }
                exist = true;
                break;
            }
        }

        if (!exist)
        {
            for (int i = 0; i < tmps.Count; i++)
            {
                T task = tmps[i];
                if (task.tid == tid)
                {
                    tmps.RemoveAt(i);

                    for (int j = 0; j < tids.Count; j++)
                    {
                        if (tids[j] == tid)
                        {
                            recTids.Add(tid);
                            break;
                        }
                    }
                    exist = true;
                    break;
                }
            }
        }
        return exist;
    }
    bool ReplaceTask<T>(int tid, List<T> tasks, List<T> tmps, T newTask) where T : BasePETask
    {
        bool isRep = false;
        for (int i = 0; i < tasks.Count; i++)
        {
            if (tasks[i].tid == tid)
            {
                tasks[i] = newTask;
                isRep = true;
                break;
            }
        }

        if (!isRep)
        {
            for (int i = 0; i < tmps.Count; i++)
            {
                if (tmps[i].tid == tid)
                {
                    tmps[i] = newTask;
                    isRep = true;
                    break;
                }
            }
        }
        return isRep;
    }
    int GetTid()
    {
        lock (obj)
        {
            tid += 1;
            //安全代码，以防万一（服务器）
            while (true)
            {
                if (tid == int.MaxValue)
                {
                    tid = 0;
                }

                //最后一个归0后从新赋值唯一id
                bool used = false;
                for (int i = 0; i < tids.Count; i++)
                {
                    if (tid == tids[i])
                    {
                        used = true;
                        break;
                    }
                }
                if (!used)
                {
                    tids.Add(tid);
                    break;
                }
                else
                {
                    tid += 1;
                }
            }
        }
        return tid;
    }
    /// <summary>
    /// tid回收
    /// </summary>
    void RecycleTid()
    {
        if (recTids.Count < 0)
            return;
        for (int i = 0; i < recTids.Count; i++)
        {
            int tid = recTids[i];

            for (int j = 0; j < tids.Count; j++)
            {
                if (tids[j] == tid)
                {
                    tids.RemoveAt(j);
                    break;
                }
            }
        }
        recTids.Clear();
    }

    void LogInfo(string info)
    {
        taskLog?.Invoke(info);
    }
    /// <summary>
    /// 获取时间的方法
    /// </summary>
    /// <returns></returns>
    double GetUTCMilliseconds()
    {
        //Now是本机时间
        //现在世界标准时间-计算机元年时间
        TimeSpan ts = DateTime.UtcNow - startDateTime;
        //返回TimeSpan值表示的毫秒数
        return ts.TotalMilliseconds;
    }

    void ChangeTimeWithType(ref float delay, EPETimeUnit timeUnit)
    {
        //时间单位换算 最小毫秒
        if (timeUnit != EPETimeUnit.Millisecond)
        {
            switch (timeUnit)
            {
                case EPETimeUnit.Second:
                    delay = delay * 1000;
                    break;
                case EPETimeUnit.Minute:
                    delay = delay * 1000 * 60;
                    break;
                case EPETimeUnit.Hour:
                    delay = delay * 1000 * 60 * 60;
                    break;
                case EPETimeUnit.Day:
                    delay = delay * 1000 * 60 * 60 * 24;
                    break;
                default:
                    LogInfo("Add Task TimeUnit Type error");
                    break;
            }
        }
    }
    #endregion
}




