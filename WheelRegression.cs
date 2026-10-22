using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Threading;
namespace FlowShift {
public static class WheelRegression {
 [System.Runtime.InteropServices.DllImport("user32.dll")]static extern IntPtr SetCapture(IntPtr window);
 [System.Runtime.InteropServices.DllImport("user32.dll")]static extern IntPtr GetCapture();
 static IEnumerable<T> Find<T>(DependencyObject root) where T:DependencyObject {if(root is T)yield return (T)root;for(int i=0;i<VisualTreeHelper.GetChildrenCount(root);i++)foreach(var child in Find<T>(VisualTreeHelper.GetChild(root,i)))yield return child;}
 static void Drain(Window window){window.UpdateLayout();window.Dispatcher.Invoke(new Action(()=>{}),DispatcherPriority.ApplicationIdle);window.UpdateLayout();}
 static void Click(Window window,string text){var button=Find<Button>(window).First(b=>Convert.ToString(b.Content)==text);var peer=new ButtonAutomationPeer(button);((IInvokeProvider)peer.GetPattern(PatternInterface.Invoke)).Invoke();Drain(window);}
 public static int RenderSettings(string[] args){var app=new Application(){ShutdownMode=ShutdownMode.OnExplicitShutdown};var main=new MainWindow(true);Backend.Initialize(main.Pref).Wait();main.Page(1);main.Height=980;main.Show();Drain(main);WheelTests.Save(main,args[1]);app.Shutdown();return 0;}
 public static int Run(string dir){Directory.CreateDirectory(dir);Preferences.Home=Path.Combine(dir,"profile");var log=new List<string>();Action<bool,string> check=(ok,name)=>{if(!ok)throw new Exception("FAIL "+name);log.Add("PASS "+name);};Application app=null;try{
  var pref=new Preferences();pref.WheelLayouts["Image"]=new List<string[]>{new[]{"webp","png","pdf","jpg","ico","bmp","tiff"},new[]{"tiff","bmp","png","","","",""}};pref.Save();var saved=Preferences.Load();check(saved.WheelLayouts["Image"].Count==2&&saved.WheelLayouts["Image"][0][0]=="webp","persist custom formats and pages");
  var routes="png webp pdf jpg ico bmp tiff zip".Split(' ').Select(f=>new Route(f,"","")).ToList();var pages=WheelPresets.Build(saved,routes,"Image");check(pages.Count==2&&pages[0][0]=="webp"&&pages[1][0]=="tiff","custom page order is applied");check(pages.All(p=>p.Last()==WheelModel.More&&p.Length<=8&&p.Distinct().Count()==p.Length),"more retained and duplicate formats excluded");var limited=WheelPresets.Build(saved,new[]{new Route("zip","","")},"Image");check(limited.All(p=>p.SequenceEqual(new[]{"zip",WheelModel.More})),"unsupported presets filtered and usable formats supplied");check(WheelPresets.Read(new Preferences(),"Image").Count==1,"old profiles receive one default page");
  app=new Application(){ShutdownMode=ShutdownMode.OnExplicitShutdown};var main=new MainWindow(true);main.Show();main.Dock=new FloatingDock(main,true);main.Dock.Show();string file=Path.Combine(dir,"test.png");using(var bitmap=new System.Drawing.Bitmap(8,8))bitmap.Save(file,System.Drawing.Imaging.ImageFormat.Png);
  var wheel=new RadialCompass(new[]{file},routes,"Image",0,null,true,saved);wheel.Show();wheel.ChangePage(1);check(wheel.PageIndex==1&&wheel.Targets[0]=="tiff","wheel next page renders configured formats");wheel.ChangePage(1);check(wheel.PageIndex==0,"last page wraps to first");wheel.ChangePage(-1);check(wheel.PageIndex==1,"previous page wraps to last");wheel.Cancel();
  bool committed=false;wheel=new RadialCompass(new[]{file},routes,"Image",0,target=>{committed=true;main.RunWheelChoice(new[]{file},target);},true,saved);wheel.DeferSelection=true;wheel.Show();Drain(wheel);var capturedHandle=new System.Windows.Interop.WindowInteropHelper(wheel).Handle;SetCapture(capturedHandle);check(GetCapture()==capturedHandle,"reproduce a captured wheel before handoff");int more=Array.IndexOf(wheel.Targets,WheelModel.More);wheel.ConfirmAt(WheelModel.Polar(152,-90+more*360.0/wheel.Targets.Length));check(!committed,"action waits until original input dispatch completes");Drain(main.Dock);check(committed&&main.Dock.ShowingAllFormats&&Mouse.Captured==null&&GetCapture()==IntPtr.Zero,"more releases capture before opening dock");var search=Find<TextBox>(main.Dock).First();check(search.IsEnabled&&search.IsHitTestVisible&&search.IsKeyboardFocused,"more search accepts focus");search.Text="zip";Drain(main.Dock);check(Find<Button>(main.Dock).Any(b=>Convert.ToString(b.Content)=="ZIP")&&!Find<Button>(main.Dock).Any(b=>Convert.ToString(b.Content)=="PNG"),"search filters actual format controls after handoff");Click(main.Dock,"清空");check(Find<TextBlock>(main.Dock).Any(t=>t.Text=="把文件交给这里。"),"dock clear button responds after more");
  main.Page(1);Drain(main);var combos=Find<ComboBox>(main).ToArray();Func<string,ComboBox> combo=name=>combos.First(c=>c.Name==name);combo("WheelSlot1").Text="webp";combo("WheelSlot2").Text="png";Click(main,"保存该类型的全部轮盘页面");check(Preferences.Load().WheelLayouts["Image"][0][0]=="webp","editor saves replacement format through UI");Click(main,"新增一页");combos=Find<ComboBox>(main).ToArray();combo("WheelSlot1").Text="tiff";Click(main,"保存该类型的全部轮盘页面");check(Preferences.Load().WheelLayouts["Image"].Count==3&&Preferences.Load().WheelLayouts["Image"][2][0]=="tiff","editor adds and saves a new page");Click(main,"删除当前页");Click(main,"保存该类型的全部轮盘页面");check(Preferences.Load().WheelLayouts["Image"].Count==2,"editor deletes selected page");Click(main,"恢复此类型默认");Click(main,"保存该类型的全部轮盘页面");check(Preferences.Load().WheelLayouts["Image"].Count==1&&Preferences.Load().WheelLayouts["Image"][0][0]=="png","editor restores default preset");
  File.WriteAllLines(Path.Combine(dir,"wheel-regression.txt"),log);main.Dock.Close();main.Close();app.Shutdown();return 0;
 }catch(Exception ex){log.Add(ex.ToString());File.WriteAllLines(Path.Combine(dir,"wheel-regression.txt"),log);if(app!=null)app.Shutdown();return 1;}}
}
}
