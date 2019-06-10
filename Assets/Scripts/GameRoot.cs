using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
public class GameRoot : MonoBehaviour
{
    public Button btn;
    public Button btnD;
    public Button btnR;

    public Button btnF;
    public Button btnFD;
    public Button btnFR;
    int tid;
    // Start is called before the first frame update
    void Start()
    {
        //初始化定时类      
        TimerSys timerSys = GetComponent<TimerSys>();
        timerSys.Init();
        btn.onClick.AddListener(ClickAddBtn);
        btnD.onClick.AddListener(ClickDelBtn);
        btnR.onClick.AddListener(ClickRepBtn);

        btnF.onClick.AddListener(ClickAddFBtn);
        btnFD.onClick.AddListener(ClickDelFBtn);
        btnFR.onClick.AddListener(ClickRepFBtn);
    }

    public void ClickAddBtn()
    {
        tid = TimerSys.Instance.AddTimeTask(() => { Debug.Log($"Tid{tid}-{System.DateTime.Now}"); }, 1, 0, EPETimeUnit.Second);   
    }

    public void ClickDelBtn()
    {
        bool ret = TimerSys.Instance.DeleteTimeTask(tid);
        Debug.Log("移除" + ret);
    }

    public void ClickRepBtn()
    {
        bool ret = TimerSys.Instance.ReplaceTimeTask(tid, () => { Debug.Log("替换了"); }, 200);
        Debug.Log("Rep" + ret);
    }

    public void ClickAddFBtn()
    {
        tid = TimerSys.Instance.AddFrameTask(() => { Debug.Log("帧到了" + System.DateTime.Now); }, 50, 0);
    }

    public void ClickDelFBtn()
    {
        bool ret = TimerSys.Instance.DeleteFrameTask(tid);
        Debug.Log("移除" + ret);
    }

    public void ClickRepFBtn()
    {
        bool ret = TimerSys.Instance.ReplaceFrameTask(tid, () => { Debug.Log("替换了帧"); }, 100);
        Debug.Log("Rep" + ret);
    }
}
