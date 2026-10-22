using System;
using System.Runtime.InteropServices;
namespace FlowShift {
public class Hook:IDisposable {
 public bool IsInstalled{get{return handle!=IntPtr.Zero;}}
 Native.MouseProc callback;IntPtr handle;MainWindow main;Native.POINT down;bool armed,shown,suppressRightUp;string[] selection;IntPtr originalWindow;DateTime last=DateTime.MinValue,pressed;System.Windows.Threading.DispatcherTimer timer;
 public static bool Reached(int holdMs,double elapsed,double dx,double dy,int distance){return holdMs>0?elapsed>=holdMs:dx*dx+dy*dy>=distance*distance;}
 public Hook(MainWindow w){main=w;callback=Handle;timer=new System.Windows.Threading.DispatcherTimer(){Interval=TimeSpan.FromMilliseconds(40)};timer.Tick+=(s,e)=>{if(!armed||!main.Pref.Gesture||!ModifierDown()||(Native.GetAsyncKeyState(main.Pref.MouseButton)&0x8000)==0){armed=false;timer.Stop();return;}double elapsed=(DateTime.Now-pressed).TotalMilliseconds;if(selection==null&&elapsed>=80)selection=Native.Selection();if(main.Pref.HoldMs>0&&Reached(main.Pref.HoldMs,elapsed,0,0,main.Pref.DragDistance))Fire();if(main.Pref.HoldMs==0&&selection!=null)timer.Stop();};handle=Native.SetWindowsHookEx(14,callback,Native.GetModuleHandle(null),0);}
 bool ModifierDown(){if(main.Pref.Modifier=="无"||main.Pref.Modifier=="None")return true;int key=main.Pref.Modifier=="Alt"?0x12:main.Pref.Modifier=="Shift"?0x10:0x11;return (Native.GetAsyncKeyState(key)&0x8000)!=0;}
 void Fire(){if(shown||!armed||!ModifierDown()||(DateTime.Now-last).TotalSeconds<1)return;if(selection==null)selection=Native.Selection();if(selection.Length==0)return;shown=true;last=DateTime.Now;timer.Stop();if(main.Pref.MouseButton==2&&main.Pref.HoldMs>0){suppressRightUp=true;Native.CancelShellMouse(originalWindow);}var files=selection;main.Dispatcher.BeginInvoke(new Action(()=>main.ShowGestureCompass(files,main.Pref.MouseButton)));}
 IntPtr Handle(int code,IntPtr msg,IntPtr data){try{if(code>=0){var info=(Native.MouseInfo)Marshal.PtrToStructure(data,typeof(Native.MouseInfo));int m=msg.ToInt32();if(main.NavigateWheelMouse(m,info.mouseData))return new IntPtr(1);if((info.flags&1)!=0)return Native.CallNextHookEx(handle,code,msg,data);if(m==0x205&&suppressRightUp){suppressRightUp=false;armed=false;timer.Stop();Native.CancelShellMouse(originalWindow);var releasePoint=info.pt;main.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.ContextIdle,new Action(()=>main.ReleaseCompass(releasePoint)));return Native.CallNextHookEx(handle,code,msg,data);}if(main.Pref.Gesture){int button=main.Pref.MouseButton;int downMessage=button==1?0x201:button==2?0x204:button==4?0x207:0x20B;int upMessage=button==1?0x202:button==2?0x205:button==4?0x208:0x20C;bool side=button<5||((info.mouseData>>16)==(button==5?1:2));
  if(m==downMessage&&side){down=info.pt;pressed=DateTime.Now;originalWindow=Native.ShellAtPoint(info.pt);armed=ModifierDown()&&originalWindow!=IntPtr.Zero;shown=false;selection=null;if(armed)timer.Start();}
  if(m==upMessage&&side){armed=false;timer.Stop();if(shown){var releasePoint=info.pt;main.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.ContextIdle,new Action(()=>main.ReleaseCompass(releasePoint)));}}
  if(m==0x200&&armed&&!shown&&ModifierDown()&&main.Pref.HoldMs==0&&(Native.GetAsyncKeyState(button)&0x8000)!=0){double dx=info.pt.x-down.x,dy=info.pt.y-down.y;if(Reached(0,0,dx,dy,main.Pref.DragDistance))Fire();}
 }}}catch{}return Native.CallNextHookEx(handle,code,msg,data);}
 public void Dispose(){timer.Stop();if(handle!=IntPtr.Zero){Native.UnhookWindowsHookEx(handle);handle=IntPtr.Zero;}}
}
}
