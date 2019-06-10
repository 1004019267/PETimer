using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

/// <summary>
/// 支持时间定时，帧定时
/// 定时任务可循环 取消 替换
/// </summary>
public class TimerSys : MonoBehaviour
{
    //单例
    public static TimerSys Instance;
    PETimer pt = new PETimer(); 
    public void Init()
    {
        Instance = this;

        pt.SetLog((string info)=> {
            Debug.Log("PETimerLog" + info);
        });
    }

    private void FixedUpdate()
    {
        pt.Update();
    }
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
        return pt.AddTimeTask(callBack, delay, count, timeUnit);
    }
    /// <summary>
    /// 移除一个计时器
    /// </summary>
    /// <param name="tid"></param>
    /// <returns></returns>
    public bool DeleteTimeTask(int tid)
    {
        return pt.DeleteTimeTask(tid);
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
        return pt.ReplaceTimeTask(tid, callBack, delay, count, timeUnit);
    }
 
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
        return pt.AddFrameTask(callBack, delay, count);
    }
    /// <summary>
    /// 移除一个帧计时器
    /// </summary>
    /// <param name="tid"></param>
    /// <returns></returns>
    public bool DeleteFrameTask(int tid)
    {
        return pt.DeleteFrameTask(tid);
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
        return pt.ReplaceFrameTask(tid, callBack, delay, count);
    }
}




