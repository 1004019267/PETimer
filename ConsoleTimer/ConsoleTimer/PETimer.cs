using System.Collections.Generic;
using System;
using System.Timers;

public class PETimeTask
{
    public int tid;
    public double destTime;//单位:毫秒
    public Action<int> callBack;
    public double delay;
    public int count;//次数

    public PETimeTask(int tid, double destTime, Action<int> callBack, double delay, int count)
    {
        this.tid = tid;
        this.destTime = destTime;
        this.callBack = callBack;
        this.count = count;
        this.delay = delay;
    }
}

public class PEFrameTask
{
    public int tid;
    public int destFrame;//单位:毫秒
    public Action<int> callBack;
    public int delay;
    public int count;//次数

    public PEFrameTask(int tid, int destFrame, Action<int> callBack, int delay, int count)
    {
        this.tid = tid;
        this.destFrame = destFrame;
        this.callBack = callBack;
        this.count = count;
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
    Action<Action<int>, int> taskHandle;
    //声明锁
    static readonly string lockTid = "lockTid";
    //C#的计时 计算机元年
    DateTime startDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0);
    double nowTime;
    Timer srvTimer;
    int tid;
    List<int> tids = new List<int>();
    /// <summary>
    /// tid缓存回收
    /// </summary>
    List<int> recTids = new List<int>();

    static readonly string lockTime = "lockTime";
    /// <summary>
    /// 临时列表 支持多线程操作 错开时间操作 避免使用锁 提升操作效率 
    /// </summary>
    List<PETimeTask> tmpTimes = new List<PETimeTask>();
    List<PETimeTask> taskTimes = new List<PETimeTask>();
    List<int> tmpDelTimes = new List<int>();

    int frameCounter;
    static readonly string lockFrame = "lockFrame";
    List<PEFrameTask> tmpFrames = new List<PEFrameTask>();
    List<PEFrameTask> taskFrames = new List<PEFrameTask>();
    List<int> tmpDelFrames = new List<int>();

    /// <summary>
    /// 
    /// </summary>
    /// <param name="interval">调用运行间隔服务器用</param>
    public PETimer(int interval = 0)
    {


        if (interval != 0)
        {
            srvTimer = new Timer(interval)
            {
                AutoReset = true  //设置是否循环              
            };

            srvTimer.Elapsed += (object sender, ElapsedEventArgs arg) =>
            {
                Update();
            };
            srvTimer.Start();
        }
    }
    public void Update()
    {
        CheckTimeTask();
        CheckFrameTask();
        DelTimeTask();
        DelFrameTask();
        if (recTids.Count > 0)
        {
            lock (lockTid)
                RecycleTid();
        }
    }
    void DelFrameTask()
    {
        if (tmpDelFrames.Count > 0)
        {
            lock (lockFrame)
            {
                for (int i = 0; i < tmpDelFrames.Count; i++)
                {
                    bool isDel = false;
                    int delTid = tmpDelFrames[i];
                    for (int j = 0; j < taskFrames.Count; j++)
                    {
                        if (taskFrames[i].tid == delTid)
                        {
                            taskFrames.RemoveAt(j);
                            recTids.Add(delTid);
                            isDel = true;
                            LogInfo("Del taskTimeList ID:" + System.Threading.Thread.CurrentThread.ManagedThreadId.ToString());
                            break;
                        }
                    }
                    if (isDel)
                    {
                        continue;
                    }

                    for (int j = 0; j < tmpFrames.Count; j++)
                    {
                        if (tmpFrames[i].tid == delTid)
                        {
                            tmpFrames.RemoveAt(j);
                            recTids.Add(delTid);
                            LogInfo("Del tmpTimeList ID:" + System.Threading.Thread.CurrentThread.ManagedThreadId.ToString());
                        }
                    }
                }
                tmpDelFrames.Clear();
            }
        }
    }

    void DelTimeTask()
    {
        if (tmpDelTimes.Count > 0)
        {
            lock (lockTime)
            {
                for (int i = 0; i < tmpDelTimes.Count; i++)
                {
                    bool isDel = false;
                    int delTid = tmpDelTimes[i];
                    for (int j = 0; j < taskTimes.Count; j++)
                    {
                        if (taskTimes[i].tid == delTid)
                        {
                            taskTimes.RemoveAt(j);
                            recTids.Add(delTid);
                            isDel = true;
                            LogInfo("Del taskTimeList ID:" + System.Threading.Thread.CurrentThread.ManagedThreadId.ToString());
                            break;
                        }
                    }
                    if (isDel)
                    {
                        continue;
                    }

                    for (int j = 0; j < tmpTimes.Count; j++)
                    {
                        if (tmpTimes[i].tid == delTid)
                        {
                            tmpTimes.RemoveAt(j);
                            recTids.Add(delTid);
                            LogInfo("Del tmpTimeList ID:" + System.Threading.Thread.CurrentThread.ManagedThreadId.ToString());
                        }
                    }
                }
                tmpDelTimes.Clear();
            }
        }

    }

    void CheckTimeTask()
    {
        //增加临时列表 因为服务器循环比update快 为了数据安全
        if (tmpTimes.Count > 0)
        {
            lock (lockTime)
            {
                //加入缓存区中的定时任务
                for (int i = 0; i < tmpTimes.Count; i++)
                {
                    taskTimes.Add(tmpTimes[i]);
                }
                tmpTimes.Clear();
            }
        }

        nowTime = GetUTCMilliseconds();
        //遍历检测任务是否到达条件
        for (int i = 0; i < taskTimes.Count; i++)
        {
            PETimeTask task = taskTimes[i];
            //nowTime>task.destTime 1 nowTime<task.destTime -1 nowTime=task.destTime 0
            if (nowTime.CompareTo(task.destTime) < 0)
            {
                continue;
            }
            else
            {
                try
                {
                    if (taskHandle != null)
                    {
                        taskHandle(task.callBack, task.tid);
                    }
                    else
                    {
                        //时间到 callBack不为空调用
                        task.callBack?.Invoke(task.tid);
                    }
                }
                catch (Exception e)
                {
                    LogInfo(e.ToString());
                }

                if (task.count == 1)
                {
                    taskTimes.RemoveAt(i);
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
                    task.destTime += task.delay;
                }
            }

        }
    }
    void CheckFrameTask()
    {
        if (tmpFrames.Count > 0)
        {
            lock (lockFrame)
            {
                //加入缓存区中的定时任务
                for (int i = 0; i < tmpFrames.Count; i++)
                {
                    taskFrames.Add(tmpFrames[i]);
                }
                tmpFrames.Clear();
            }
        }


        frameCounter += 1;
        //遍历检测任务是否到达条件
        for (int i = 0; i < taskFrames.Count; i++)
        {
            PEFrameTask task = taskFrames[i];
            if (frameCounter < task.destFrame)
            {
                continue;
            }
            else
            {
                try
                {
                    if (taskHandle != null)
                    {
                        taskHandle(task.callBack, task.tid);
                    }
                    else
                    {
                        //时间到 callBack不为空调用
                        task.callBack?.Invoke(task.tid);
                    }
                }
                catch (Exception e)
                {
                    LogInfo(e.ToString());
                }

                if (task.count == 1)
                {
                    taskFrames.RemoveAt(i);
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
                    task.destFrame += task.delay;
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
    public int AddTimeTask(Action<int> callBack, float delay, int count = 1, EPETimeUnit timeUnit = EPETimeUnit.Millisecond)
    {
        ChangeTimeWithType(ref delay, timeUnit);
        int tid = GetTid();
        //从游戏开始到现在的时间
        nowTime = GetUTCMilliseconds();
        lock (lockTime)
            tmpTimes.Add(new PETimeTask(tid, nowTime + delay, callBack, delay, count));

        return tid;
    }
    /// <summary>
    /// 移除一个计时器
    /// </summary>
    /// <param name="tid"></param>
    /// <returns></returns>
    public void DeleteTimeTask(int tid)
    {
        lock (lockTime)
        {
            tmpDelTimes.Add(tid);
            LogInfo("TmpDel ID:" + System.Threading.Thread.CurrentThread.ManagedThreadId.ToString());
        }
    }
    /// <summary>
    /// 替换 不涉及数据增删不用去改 自己改的慢就完蛋
    /// </summary>
    /// <param name="tid"></param>
    /// <param name="callBack"></param>
    /// <param name="delay"></param>
    /// <param name="count"></param>
    /// <param name="timeUnit"></param>
    /// <returns></returns>
    public bool ReplaceTimeTask(int tid, Action<int> callBack, float delay, int count = 1, EPETimeUnit timeUnit = EPETimeUnit.Millisecond)
    {
        ChangeTimeWithType(ref delay, timeUnit);
        //从游戏开始到现在的时间
        nowTime = GetUTCMilliseconds();
        PETimeTask newTask = new PETimeTask(tid, nowTime + delay, callBack, delay, count);

        bool isRep = false;
        for (int i = 0; i < taskTimes.Count; i++)
        {
            if (taskTimes[i].tid == tid)
            {
                taskTimes[i] = newTask;
                isRep = true;
                break;
            }
        }

        if (!isRep)
        {
            for (int i = 0; i < tmpTimes.Count; i++)
            {
                if (tmpTimes[i].tid == tid)
                {
                    tmpTimes[i] = newTask;
                    isRep = true;
                    break;
                }
            }
        }
        return isRep;
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
    public int AddFrameTask(Action<int> callBack, int delay, int count = 1)
    {
        int tid = GetTid();
        lock (lockFrame)
            taskFrames.Add(new PEFrameTask(tid, frameCounter + delay, callBack, delay, count));
        return tid;
    }
    /// <summary>
    /// 移除一个帧计时器
    /// </summary>
    /// <param name="tid"></param>
    /// <returns></returns>
    public void DeleteFrameTask(int tid)
    {
        lock (lockFrame)
        {
            tmpDelFrames.Add(tid);
        }
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
    public bool ReplaceFrameTask(int tid, Action<int> callBack, int delay, int count = 1)
    {
        PEFrameTask newTask = new PEFrameTask(tid, frameCounter + delay, callBack, delay, count);

        bool isRep = false;
        for (int i = 0; i < taskFrames.Count; i++)
        {
            if (taskFrames[i].tid == tid)
            {
                taskFrames[i] = newTask;
                isRep = true;
                break;
            }
        }

        if (!isRep)
        {
            for (int i = 0; i < tmpFrames.Count; i++)
            {
                if (tmpFrames[i].tid == tid)
                {
                    tmpFrames[i] = newTask;
                    isRep = true;
                    break;
                }
            }
        }
        return isRep;
    }
    #endregion

    public void SetLog(Action<string> log)
    {
        taskLog = log;
    }

    public void SetHandle(Action<Action<int>, int> handle)
    {
        taskHandle = handle;
    }
    /// <summary>
    /// 重置
    /// </summary>
    public void Reset()
    {
        tid = 0;
        tids.Clear();
        recTids.Clear();

        tmpTimes.Clear();
        taskTimes.Clear();

        tmpFrames.Clear();
        taskFrames.Clear();

        taskLog = null;
        srvTimer.Stop();
    }
    //累加而不是 now now的话打断点也会变化
    public DateTime GetLocalDateTime()
    {
        return TimeZone.CurrentTimeZone.ToLocalTime(startDateTime.AddMilliseconds(nowTime));
    }
    public double GetMillisendsTime()
    {
        return nowTime;
    }

    public int GetYear()
    {
        return GetLocalDateTime().Year;
    }

    public int GetMonth()
    {
        return GetLocalDateTime().Month;
    }
    public int GetDay()
    {
        return GetLocalDateTime().Day;
    }
    public int GetDayOfWeek()
    {
        return (int)GetLocalDateTime().DayOfWeek;
    }

    public string GetLocalTimeStr()
    {
        DateTime dt = GetLocalDateTime();
        string str = $"{GetTimeStr(dt.Hour)}:{GetTimeStr(dt.Minute)}:{GetTimeStr(dt.Second)}";
        return str;
    }
    #region Tool Methonds
    int GetTid()
    {
        lock (lockTid)
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


    string GetTimeStr(int time)
    {
        if (time < 10)
        {
            return $"0{time}";
        }
        else
        {
            return time.ToString();
        }
    }
    #endregion
}




