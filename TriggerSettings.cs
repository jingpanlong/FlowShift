using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
namespace FlowShift {
public static class HotkeyText {
 public static string Format(uint mods,int key){if(key==0)return "未设置";string s=((mods&2)!=0?"Ctrl + ":"")+((mods&1)!=0?"Alt + ":"")+((mods&4)!=0?"Shift + ":"")+((mods&8)!=0?"Win + ":"");return s+KeyInterop.KeyFromVirtualKey(key);}
 public static bool Valid(uint mods,int key){return key>0&&((mods&11)!=0||(key>=0x70&&key<=0x87));}
}
public partial class MainWindow {
 public void ShowDock(){if(Dock!=null){Dock.Reveal();Dock.Expand();}}
 public void ShowAbout(){new AboutWindow().ShowDialog();}
 public string IntegrationError="";
 void ApplyDesktopMenu(){try{if(Environment.GetEnvironmentVariable("FLOWSHIFT_NO_INTEGRATION")=="1")return;string exe=Assembly.GetExecutingAssembly().Location;if(Pref.ContextMenuEnabled)DesktopIntegration.Register(exe);else DesktopIntegration.Unregister(exe);IntegrationError="";}catch(Exception ex){IntegrationError=ex.Message;}}
 void RegisterCompassHotkey(){var handle=new System.Windows.Interop.WindowInteropHelper(this).Handle;Native.UnregisterHotKey(handle,1);hotkey=Pref.CompassKey!=0&&Native.RegisterHotKey(handle,1,0x4000|Pref.CompassModifiers,(uint)Pref.CompassKey);if(tray!=null)tray.Text="流转 FlowShift · "+(Pref.Gesture&&Pref.MouseButton==2&&Pref.HoldMs>0?"长按右键打开罗盘":"贴边悬浮 / 格式转换");}
 void DesktopSettings(StackPanel page){var p=new StackPanel();p.Children.Add(T("桌面入口与格式罗盘",19,"#7EE9D5"));var edge=new CheckBox(){Content="拖到屏幕边缘自动收成小标签，点击标签弹出",IsChecked=Pref.EdgeAutoHide,Margin=new Thickness(0,16,0,12)};var menu=new CheckBox(){Content="在文件 / 文件夹右键菜单加入“流转 · 转换格式”",IsChecked=Pref.ContextMenuEnabled,Margin=new Thickness(0,0,0,12)};p.Children.Add(edge);p.Children.Add(menu);p.Children.Add(T("Windows 11 的经典菜单位于“显示更多选项”。多选文件会合并交给同一个悬浮坞处理。",11,"#8C9BB2",new Thickness(0,0,0,18)));
  uint mods=Pref.CompassModifiers;int key=Pref.CompassKey;var input=new TextBox(){Text=HotkeyText.Format(mods,key),IsReadOnly=true,Padding=new Thickness(10),Margin=new Thickness(0,7,0,10),ToolTip="点击后按需要的快捷键；Delete 清空"};input.PreviewKeyDown+=(s,e)=>{e.Handled=true;Key actual=e.Key==Key.System?e.SystemKey:e.Key;if(actual==Key.Delete||actual==Key.Back){key=0;mods=0;input.Text="未设置";return;}if(actual==Key.LeftCtrl||actual==Key.RightCtrl||actual==Key.LeftShift||actual==Key.RightShift||actual==Key.LeftAlt||actual==Key.RightAlt||actual==Key.LWin||actual==Key.RWin)return;var m=Keyboard.Modifiers;uint candidate=((m&ModifierKeys.Control)!=0?2U:0)|((m&ModifierKeys.Alt)!=0?1U:0)|((m&ModifierKeys.Shift)!=0?4U:0)|((m&ModifierKeys.Windows)!=0?8U:0);int virtualKey=KeyInterop.VirtualKeyFromKey(actual);if(HotkeyText.Valid(candidate,virtualKey)){mods=candidate;key=virtualKey;input.Text=HotkeyText.Format(mods,key);}};p.Children.Add(Field("格式罗盘键盘快捷键（点击输入框后直接按键）",input));p.Children.Add(T("默认还支持无修饰键长按右键 650 毫秒。普通短按右键保持 Windows 原菜单。\n下面的“鼠标触发与快捷配方”可调整按键、修饰键与长按时长。",11,"#8C9BB2",new Thickness(0,0,0,16)));var state=T(IntegrationError==""?"菜单状态："+(Pref.ContextMenuEnabled?"已启用":"已关闭"):"菜单状态："+IntegrationError,11,"#8C9BB2");p.Children.Add(state);
  p.Children.Add(Btn("保存桌面入口与罗盘快捷键",()=>{Pref.EdgeAutoHide=edge.IsChecked==true;Pref.ContextMenuEnabled=menu.IsChecked==true;Pref.CompassModifiers=mods;Pref.CompassKey=key;Pref.Save();if(Dock!=null)Dock.ApplyEdgePreference();RegisterCompassHotkey();ApplyDesktopMenu();state.Text="菜单："+(IntegrationError==""?(Pref.ContextMenuEnabled?"已启用":"已关闭"):IntegrationError)+"；键盘快捷键："+(key==0?"已关闭":hotkey?"注册成功":"已被占用，请更换组合键");},true));page.Children.Add(Card(p,new Thickness(0,20,0,0)));}
}
}
