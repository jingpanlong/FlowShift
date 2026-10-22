using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Reflection;
using System.Web.Script.Serialization;

namespace FlowShift {
public class CoreFormat { public string Id,Name,Category;public string[] Extensions,Targets;public bool Writable; }
public class CoreEngine { public string Id,Name,Reason,Detail;public bool Available; }
public class CoreCatalog { public CoreFormat[] Formats;public CoreEngine[] Engines; }
public class CorePackage {public string Id;public long Download,Disk;public bool Available,Installed;}
public class CoreFile {public string Source,State,Error,Detail,Notice;public string[] Outputs;}
public class CoreResult {public string State;public CoreFile[] Files;}
public class Recipe {
 public int Quality=90,MaxWidth=0,Dpi=0,Rotate=0;
 public bool Grayscale=false,StripMetadata=false,Merge=false,Combine=false,Compress=false,KeepDates=true,Split=false,LongImage=false,RemoveAudio=false,Hardware=false,Frames=false,RemoveBackground=false;
 public int VideoHeight=0,AudioBitrate=0;
 public string Pages="",NamePattern="{name}";
}
public static class Backend {
 public static CoreCatalog Catalog;public static string Failure="";public static string Executable;public static bool Ready {get{return Catalog!=null;}}
 static HashSet<string> active=new HashSet<string>();
 public static void Shutdown(){lock(active)foreach(string file in active)try{File.WriteAllText(file,"cancel");}catch{}}
 public static JavaScriptSerializer Json(){return new JavaScriptSerializer(){MaxJsonLength=64*1024*1024,RecursionLimit=100};}
 public static string Home {get{return Path.Combine(Preferences.Home,"CoreData");}}
 public static Task Initialize(Preferences pref){return Task.Run(()=>{
  try{Directory.CreateDirectory(Home);Executable=Environment.GetEnvironmentVariable("FLOWSHIFT_CORE");
   if(!File.Exists(Executable)){
    string version="2.0.0.0";string root=Path.Combine(Preferences.Home,"runtime-"+version);string marker=Path.Combine(root,"ready.txt");Executable=Path.Combine(root,"FlowShift.CoreHost.exe");
    if(!File.Exists(marker)||!File.Exists(Executable)){
     var assembly=Assembly.GetExecutingAssembly();using(var input=assembly.GetManifestResourceStream("FlowShift.CorePayload")){if(input==null)throw new Exception("没有找到 2.0 转换内核缓存。请运行 2.1 全量版一次，或选择完整安装包。");
      string stage=root+"-"+Guid.NewGuid().ToString("N");Directory.CreateDirectory(stage);
      try{using(var zip=new ZipArchive(input,ZipArchiveMode.Read)){long required=zip.Entries.Sum(e=>e.Length)+128L*1024*1024;if(new DriveInfo(Path.GetPathRoot(stage)).AvailableFreeSpace<required)throw new Exception("准备内核需要至少 "+MainWindow.Size(required)+" 可用磁盘空间。");string prefix=Path.GetFullPath(stage)+Path.DirectorySeparatorChar;foreach(var entry in zip.Entries){string path=Path.GetFullPath(Path.Combine(stage,entry.FullName));if(!path.StartsWith(prefix,StringComparison.OrdinalIgnoreCase))throw new Exception("内核归档路径无效。");if(String.IsNullOrEmpty(entry.Name)){Directory.CreateDirectory(path);continue;}Directory.CreateDirectory(Path.GetDirectoryName(path));entry.ExtractToFile(path);}}
       if(Directory.Exists(root)){// An incomplete runtime is replaced only inside FlowShift's own version directory.
        string backup=root+"-incomplete-"+Guid.NewGuid().ToString("N");Directory.Move(root,backup);
       }Directory.Move(stage,root);File.WriteAllText(marker,version);
      }catch{try{if(Directory.Exists(stage))Directory.Delete(stage,true);}catch{}throw;}
     }
    }
   }
   var copies=new Dictionary<string,string>();if(File.Exists(pref.Ffmpeg))copies["ffmpeg"]=pref.Ffmpeg;if(File.Exists(pref.Office))copies["libreoffice"]=pref.Office;if(File.Exists(pref.Pandoc))copies["pandoc"]=pref.Pandoc;if(File.Exists(pref.Calibre))copies["calibre"]=pref.Calibre;
   File.WriteAllText(Path.Combine(Home,"owncopies.json"),Json().Serialize(copies));
   string catalog=Path.Combine(Home,"catalog-"+Guid.NewGuid().ToString("N")+".json");try{Run("catalog "+Converter.Q(catalog),CancellationToken.None,null,null);Catalog=Json().Deserialize<CoreCatalog>(File.ReadAllText(catalog));Failure="";}finally{if(File.Exists(catalog))File.Delete(catalog);}
  }catch(Exception ex){Catalog=null;Failure=ex.Message;}
 });}
 public static CoreFormat Detect(string file){if(!Ready)return null;string name=Path.GetFileName(file).ToLowerInvariant();return Catalog.Formats.OrderByDescending(f=>f.Extensions.Max(e=>e.Length)).FirstOrDefault(f=>f.Extensions.Any(e=>name.EndsWith("."+e)));}
 public static List<Route> Routes(string file){var f=Detect(file);if(f==null)return new List<Route>();return f.Targets.Distinct().Select(id=>new Route(id=="folder"?"extract":id,"Filee",Category(Catalog.Formats.FirstOrDefault(t=>t.Id==id)==null?"Archive":Catalog.Formats.First(t=>t.Id==id).Category))).ToList();}
 public static string Category(string key){var map=new Dictionary<string,string>{{"Image","图片"},{"Vector","矢量"},{"Pdf","PDF"},{"Document","文档"},{"Spreadsheet","表格"},{"Presentation","演示"},{"Hwp","HWP"},{"Text","文本"},{"Video","视频"},{"Audio","音频"},{"Ebook","电子书"},{"Archive","压缩包"},{"Cad","CAD"},{"Font","字体"},{"Subtitle","字幕"},{"Data","数据"},{"Model3D","3D 模型"},{"Geo","地理数据"}};return map.ContainsKey(key)?map[key]:key;}
 public static string Group(string format){if(format=="extract"||format=="gz")return "压缩包";var f=Ready?Catalog.Formats.FirstOrDefault(x=>x.Id==format):null;return f==null?"常用":Category(f.Category);}
 public static CoreResult Convert(string[] files,string target,Preferences pref,Recipe recipe,CancellationToken token,Action<double> progress){
  if(!Ready)throw new Exception("转换内核未就绪："+Failure);
  string request=Path.Combine(Home,"job-"+Guid.NewGuid().ToString("N")+".json");
  var data=Json().Deserialize<Dictionary<string,object>>(Json().Serialize(recipe));data["Files"]=files;data["Target"]=target=="extract"?"folder":target;data["Output"]=pref.Output;
  File.WriteAllText(request,Json().Serialize(data),new UTF8Encoding(false));lock(active)active.Add(request+".cancel");
  try{int code=Run("convert "+Converter.Q(request),token,line=>{try{var d=Json().Deserialize<Dictionary<string,object>>(line);if(d.ContainsKey("Value")&&progress!=null)progress(System.Convert.ToDouble(d["Value"]));}catch{}},request+".cancel");
   token.ThrowIfCancellationRequested();if(!File.Exists(request+".result.json"))throw new Exception("内核没有返回转换结果，退出码 "+code);return Json().Deserialize<CoreResult>(File.ReadAllText(request+".result.json"));
  }finally{lock(active)active.Remove(request+".cancel");foreach(string suffix in new[]{"",".cancel",".result.json"})try{if(File.Exists(request+suffix))File.Delete(request+suffix);}catch{}}
 }
 public static CorePackage[] Packages(){string p=Path.Combine(Home,"packages-"+Guid.NewGuid().ToString("N")+".json");try{Run("packages "+Converter.Q(p),CancellationToken.None,null,null);return Json().Deserialize<CorePackage[]>(File.ReadAllText(p));}finally{if(File.Exists(p))File.Delete(p);}}
 public static void Install(string id,CancellationToken token,Action<double> progress){string cancel=Path.Combine(Home,Guid.NewGuid().ToString("N")+".cancel");lock(active)active.Add(cancel);try{Run("install "+Converter.Q(id)+" "+Converter.Q(cancel),token,line=>{try{var d=Json().Deserialize<Dictionary<string,object>>(line);progress(System.Convert.ToDouble(d["Value"]));}catch{}},cancel);}finally{lock(active)active.Remove(cancel);if(File.Exists(cancel))File.Delete(cancel);}}
 static int Run(string args,CancellationToken token,Action<string> line,string cancelFile){
  var si=new ProcessStartInfo(Executable,args){UseShellExecute=false,CreateNoWindow=true,RedirectStandardError=true,RedirectStandardOutput=true,StandardOutputEncoding=Encoding.UTF8,StandardErrorEncoding=Encoding.UTF8};si.EnvironmentVariables["FLOWSHIFT_DATA"]=Home;si.EnvironmentVariables["FLOWSHIFT_ENGINES"]=Path.Combine(Home,"owncopies.json");
  using(var p=Process.Start(si)){var errors=p.StandardError.ReadToEndAsync();var output=Task.Run(()=>{string s;while((s=p.StandardOutput.ReadLine())!=null){if(line!=null)line(s);}});DateTime? cancelTime=null;var elapsed=Stopwatch.StartNew();while(!p.WaitForExit(100)){if(token.IsCancellationRequested){if(cancelTime==null){cancelTime=DateTime.Now;if(cancelFile!=null)File.WriteAllText(cancelFile,"cancel");}if((DateTime.Now-cancelTime.Value).TotalSeconds>15){try{p.Kill();}catch{}}}if(elapsed.Elapsed.TotalMinutes>45){try{p.Kill();}catch{}throw new Exception("内核超时（45 分钟）。");}}
   output.Wait();string detail=errors.Result;token.ThrowIfCancellationRequested();if(p.ExitCode>1)throw new Exception(detail.Length>2200?detail.Substring(detail.Length-2200):detail);return p.ExitCode;
  }
 }
}
}
