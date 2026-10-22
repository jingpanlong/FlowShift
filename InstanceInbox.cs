using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Threading;
namespace FlowShift {
public class LaunchRequest { public string[] Files=new string[0];public string Target="";public string Kind="open"; }
public sealed class InstanceInbox:IDisposable {
 public static string Root{get{return Path.Combine(Preferences.Home,"Inbox-v21");}}
 MainWindow main;DispatcherTimer timer;string identity;bool disposed;
 public static LaunchRequest Parse(string[] args){var files=new List<string>();string target="";for(int i=0;i<args.Length;i++){if(args[i]=="--target"){if(i+1<args.Length)target=args[++i].ToLowerInvariant();continue;}if(args[i].StartsWith("--"))continue;if(File.Exists(args[i])||Directory.Exists(args[i]))files.Add(Path.GetFullPath(args[i]));}return new LaunchRequest(){Files=files.ToArray(),Target=target,Kind=args.Contains("--about")?"about":args.Contains("--shell")||files.Count>0?"files":"open"};}
 public static bool TrySend(LaunchRequest request){try{string state=Path.Combine(Root,"instance");if(!File.Exists(state))return false;var parts=File.ReadAllText(state).Split('|');var process=Process.GetProcessById(Int32.Parse(parts[0]));if(process.HasExited||process.StartTime.ToUniversalTime().Ticks.ToString()!=parts[1])return false;Native.AllowSetForegroundWindow(process.Id);if(request.Kind=="open"&&Native.ShowExistingWorkspace(process.Id))return true;Write(request);return true;}catch{return false;}}
 static void Write(LaunchRequest request){Directory.CreateDirectory(Root);string file=Path.Combine(Root,Guid.NewGuid().ToString("N")+".json");File.WriteAllText(file+".part",Backend.Json().Serialize(request));File.Move(file+".part",file);}
 public InstanceInbox(MainWindow owner,LaunchRequest initial){main=owner;Directory.CreateDirectory(Root);var process=Process.GetCurrentProcess();identity=process.Id+"|"+process.StartTime.ToUniversalTime().Ticks;File.WriteAllText(Path.Combine(Root,"instance"),identity);if(initial.Kind!="open"||initial.Files.Length>0)Write(initial);timer=new DispatcherTimer(){Interval=TimeSpan.FromMilliseconds(250)};timer.Tick+=(s,e)=>Drain();timer.Start();}
 void Drain(){if(disposed||main.Dock==null)return;var paths=Directory.GetFiles(Root,"*.json").OrderBy(File.GetLastWriteTimeUtc).ToArray();if(paths.Length==0)return;
  var first=paths[0];if((DateTime.UtcNow-File.GetLastWriteTimeUtc(first)).TotalMilliseconds<550)return;
  var requests=new List<LaunchRequest>();foreach(string path in paths){try{var request=Backend.Json().Deserialize<LaunchRequest>(File.ReadAllText(path));if(request==null)throw new InvalidDataException();requests.Add(request);}catch{try{File.Delete(path);}catch{}}}
  if(requests.Count==0)return;var head=requests.FirstOrDefault(r=>r.Kind=="open"||r.Kind=="about")??requests[0];if(head.Kind=="files"&&(main.Dock.IsBusy||!Backend.Ready&&head.Target!=""))return;
  var accepted=new List<string>();var files=new List<string>();foreach(string path in paths){try{var request=Backend.Json().Deserialize<LaunchRequest>(File.ReadAllText(path));if(request.Kind!=head.Kind||request.Target!=head.Target)continue;files.AddRange(request.Files??new string[0]);accepted.Add(path);}catch{}}
  foreach(string path in accepted)try{File.Delete(path);}catch{}
  if(head.Kind=="about"){main.ShowAbout();return;}if(head.Kind=="open"){main.ShowMain();return;}
  main.Dock.ShowFiles(files.Distinct(StringComparer.OrdinalIgnoreCase).ToArray());if(head.Target!=""){if(main.Dock.CanConvert(head.Target))main.Dock.Start(head.Target);else Message(head.Target);}
 }
 void Message(string target){System.Windows.MessageBox.Show("所选文件没有到 "+target.ToUpperInvariant()+" 的共同转换路线。请在悬浮坞选择可用格式。","流转 · 选择其他格式");}
 public void Dispose(){disposed=true;timer.Stop();try{string state=Path.Combine(Root,"instance");if(File.Exists(state)&&File.ReadAllText(state)==identity)File.Delete(state);}catch{}}
}
}
