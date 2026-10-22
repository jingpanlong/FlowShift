using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Microsoft.Win32;
namespace FlowShift {
public static class DesktopIntegration {
 const string Verb="FlowShift.Convert";
 [DllImport("shell32.dll")]static extern void SHChangeNotify(uint eventId,uint flags,IntPtr item1,IntPtr item2);
 public static string Command(string exe,string target){if(exe.IndexOf('"')>=0)throw new ArgumentException("Invalid executable path");return "\""+Path.GetFullPath(exe)+"\" --shell"+(target==""?"":" --target "+target)+" \"%1\"";}
 public static void Register(string exe){if(!File.Exists(exe))throw new FileNotFoundException("找不到流转程序",exe);using(var classes=Registry.CurrentUser.CreateSubKey(@"Software\Classes"))RegisterIn(classes,exe);SHChangeNotify(0x08000000,0,IntPtr.Zero,IntPtr.Zero);}
 public static void RegisterIn(RegistryKey classes,string exe){foreach(string family in new[]{"*","Directory"})using(var root=classes.CreateSubKey(family+@"\shell\"+Verb)){
  root.SetValue("MUIVerb","流转 · 转换格式");root.SetValue("SubCommands","");root.SetValue("Icon",exe+",0");root.SetValue("MultiSelectModel","Document");root.SetValue("FlowShiftOwner",Path.GetFullPath(exe));
  string[] ids={"","png","jpg","webp","pdf","docx","mp3","mp4","zip","extract"};
  for(int i=0;i<ids.Length;i++)using(var item=root.CreateSubKey(@"shell\"+i.ToString("D2")+"_"+(ids[i]==""?"choose":ids[i]))){item.SetValue("MUIVerb",ids[i]==""?"选择其他格式…":ids[i]=="extract"?"解压文件":ids[i].ToUpperInvariant());item.SetValue("MultiSelectModel","Document");using(var cmd=item.CreateSubKey("command"))cmd.SetValue("",Command(exe,ids[i]));}
 }}
 public static void Unregister(string exe){using(var classes=Registry.CurrentUser.CreateSubKey(@"Software\Classes"))UnregisterIn(classes,exe);SHChangeNotify(0x08000000,0,IntPtr.Zero,IntPtr.Zero);}
 public static void UnregisterIn(RegistryKey classes,string exe){foreach(string family in new[]{"*","Directory"}){string path=family+@"\shell\"+Verb;bool owned=false;using(var key=classes.OpenSubKey(path))owned=key!=null&&String.Equals(key.GetValue("FlowShiftOwner") as string,Path.GetFullPath(exe),StringComparison.OrdinalIgnoreCase);if(owned)classes.DeleteSubKeyTree(path,false);}}
}
}
