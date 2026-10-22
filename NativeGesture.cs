using System;
using System.Text;
using System.Runtime.InteropServices;
namespace FlowShift {
public static partial class Native {
 [DllImport("user32.dll")]static extern bool ReleaseCapture();
 public static void ReleaseOurMouseCapture(){System.Windows.Input.Mouse.Capture(null);ReleaseCapture();}
 delegate bool WindowVisitor(IntPtr window,IntPtr state);
 [DllImport("user32.dll")]static extern bool EnumWindows(WindowVisitor visitor,IntPtr state);
 [DllImport("user32.dll",CharSet=CharSet.Unicode)]static extern int GetWindowText(IntPtr window,StringBuilder text,int count);
 [DllImport("user32.dll")]static extern bool IsIconic(IntPtr window);
 [DllImport("user32.dll")]static extern bool ShowWindowAsync(IntPtr window,int command);
 [DllImport("user32.dll")]static extern bool SetForegroundWindow(IntPtr window);
 [DllImport("user32.dll")]public static extern bool AllowSetForegroundWindow(int processId);
 public static bool ShowExistingWorkspace(int processId){IntPtr workspace=IntPtr.Zero;EnumWindows((window,state)=>{uint pid;GetWindowThreadProcessId(window,out pid);if(pid!=(uint)processId)return true;var title=new StringBuilder(256);GetWindowText(window,title,256);if(title.ToString()!="流转 FlowShift · 本地格式工作台")return true;workspace=window;return false;},IntPtr.Zero);if(workspace==IntPtr.Zero)return false;ShowWindowAsync(workspace,IsIconic(workspace)?9:5);SetForegroundWindow(workspace);return true;}
 [DllImport("user32.dll")]static extern IntPtr WindowFromPoint(POINT point);
 [DllImport("user32.dll")]static extern IntPtr GetAncestor(IntPtr window,uint flag);
 [DllImport("user32.dll")]static extern uint GetWindowThreadProcessId(IntPtr window,out uint pid);
 [DllImport("user32.dll")]static extern bool GetGUIThreadInfo(uint thread,ref GuiInfo info);
 [DllImport("user32.dll")]static extern bool PostMessage(IntPtr window,uint message,IntPtr w,IntPtr l);
 [StructLayout(LayoutKind.Sequential)]struct GuiInfo {public uint size,flags;public IntPtr active,focus,capture,menuOwner,moveSize,caret;public int left,top,right,bottom;}
 public static IntPtr ShellAtPoint(POINT point){IntPtr window=WindowFromPoint(point);IntPtr root=GetAncestor(window,2);if(root==IntPtr.Zero)root=window;var name=new StringBuilder(256);GetClassName(root,name,256);string type=name.ToString();return type=="CabinetWClass"||type=="ExploreWClass"||type=="Progman"||type=="WorkerW"?root:IntPtr.Zero;}
 public static void CancelShellMouse(IntPtr window){if(window==IntPtr.Zero)return;uint pid;uint thread=GetWindowThreadProcessId(window,out pid);var info=new GuiInfo(){size=(uint)Marshal.SizeOf(typeof(GuiInfo))};if(GetGUIThreadInfo(thread,ref info)&&info.capture!=IntPtr.Zero)PostMessage(info.capture,0x001F,IntPtr.Zero,IntPtr.Zero);PostMessage(window,0x001F,IntPtr.Zero,IntPtr.Zero);}
}
}
