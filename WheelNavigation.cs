using System;
using System.Runtime.InteropServices;
using System.Windows.Threading;
namespace FlowShift {
public sealed class WheelScroll {
 int remainder;
 public int Steps(int delta){remainder+=delta;int steps=-(remainder/120);remainder%=120;return steps;}
}
public static class WheelNavigation {
 public static int Direction(int key){return key==0x27||key==0x28||key==0x22?1:key==0x25||key==0x26||key==0x21?-1:0;}
 public static int WheelDelta(uint mouseData){return (short)(mouseData>>16);}
}
public sealed class WheelKeyHook:IDisposable {
 [StructLayout(LayoutKind.Sequential)]internal struct KeyInfo {public uint key,scan,flags,time;public IntPtr extra;}
 readonly MainWindow main;Native.MouseProc callback;IntPtr handle;readonly System.Collections.Generic.HashSet<uint> consumed=new System.Collections.Generic.HashSet<uint>();
 public bool IsInstalled{get{return handle!=IntPtr.Zero;}}
 public WheelKeyHook(MainWindow owner):this(owner,true){}
 internal WheelKeyHook(MainWindow owner,bool install){main=owner;callback=Read;if(install)handle=Native.SetWindowsHookEx(13,callback,Native.GetModuleHandle(null),0);}
 internal bool Route(int message,KeyInfo info){if(message==0x101||message==0x105)return consumed.Remove(info.key);if(message!=0x100&&message!=0x104)return false;if(main.NavigateWheelKey((int)info.key)){consumed.Add(info.key);return true;}return false;}
 IntPtr Read(int code,IntPtr message,IntPtr data){try{if(code>=0&&Route(message.ToInt32(),(KeyInfo)Marshal.PtrToStructure(data,typeof(KeyInfo))))return new IntPtr(1);}catch{}return Native.CallNextHookEx(handle,code,message,data);}
 public void Dispose(){if(handle!=IntPtr.Zero){Native.UnhookWindowsHookEx(handle);handle=IntPtr.Zero;}consumed.Clear();}
}
public partial class MainWindow {
 internal RadialCompass activeWheel{get{return radial; }set{radial=value;}}
 public bool NavigateWheelMouse(int message,uint data){var popup=radial;if(popup==null||!popup.IsLive||(message!=0x20A&&message!=0x20E))return false;int delta=WheelNavigation.WheelDelta(data);if(message==0x20E)delta=-delta;Dispatcher.BeginInvoke(DispatcherPriority.Input,new Action(()=>{if(radial==popup&&popup.IsLive)popup.Scroll(delta);}));return true;}
 public bool NavigateWheelKey(int key){var popup=radial;if(popup==null||!popup.IsLive)return false;int direction=WheelNavigation.Direction(key);if(direction==0&&key!=0x1B)return false;Dispatcher.BeginInvoke(DispatcherPriority.Input,new Action(()=>{if(radial!=popup||!popup.IsLive)return;if(key==0x1B)popup.Cancel();else popup.ChangePage(direction);}));return true;}
}
}
