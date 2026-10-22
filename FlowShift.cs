using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Interop;
using System.Windows.Input;
using System.Xml;
using System.Web.Script.Serialization;
using Forms = System.Windows.Forms;
using Drawing = System.Drawing;

namespace FlowShift {
public class Preferences {
 public string Output = "", Ffmpeg = "", Office = "", Pandoc = "", Calibre = "";
 public int Quality = 90, MaxWidth = 0, DragDistance = 70;
 public string Modifier = "无";
 public bool Gesture = true;
 public double DockX=-1,DockY=-1;
 public Recipe SavedRecipe=new Recipe();
 public int MouseButton=2,HoldMs=650;
 public int UXVersion=21,CompassKey=0x20;public uint CompassModifiers=3;
 public bool EdgeAutoHide=true,ContextMenuEnabled=true,DockHasPosition=false;public string DockEdge="";public double DockAlong=.5;
 public Dictionary<string,List<string[]>> WheelLayouts=new Dictionary<string,List<string[]>>();
 public string[] QuickTargets=new[]{"png","jpg","pdf","docx","webp","mp3","mp4","zip","extract"};
 public bool WatchEnabled = false;
 public string WatchSource = "", WatchOutput = "", WatchTarget = "png";
 public static string Home = Environment.GetEnvironmentVariable("FLOWSHIFT_HOME") ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FlowShift");
 public static Preferences Load() { try { string text=File.ReadAllText(Path.Combine(Home,"settings.json"));var p=new JavaScriptSerializer().Deserialize<Preferences>(text)??new Preferences();if(!text.Contains("UXVersion")&&p.MouseButton==1&&p.HoldMs==0&&p.Modifier=="Ctrl"){p.MouseButton=2;p.HoldMs=650;p.Modifier="无";}p.UXVersion=21;return p; } catch { return new Preferences(); } }
 public void Save() { Directory.CreateDirectory(Home); File.WriteAllText(Path.Combine(Home,"settings.json"),new JavaScriptSerializer().Serialize(this)); }
}
public class Route {
 public string Format, Engine, Description;
 public Route(string f,string e,string d) { Format=f;Engine=e;Description=d; }
}
public class Converter {
 public Preferences Settings;
 public Converter(Preferences p) { Settings=p; }
 public static string Ext(string p) { return Path.GetExtension(p).TrimStart('.').ToLowerInvariant(); }
 public static bool In(string e,string list) { return list.Split(' ').Contains(e); }
 public string Find(string key) {
  string custom=key=="ffmpeg"?Settings.Ffmpeg:key=="soffice"?Settings.Office:key=="pandoc"?Settings.Pandoc:Settings.Calibre;
  if(File.Exists(custom)) return custom;
  string name=key+".exe";
  foreach(string dir in (Environment.GetEnvironmentVariable("PATH")??"").Split(';')) { try { string p=Path.Combine(dir.Trim('"'),name); if(File.Exists(p)) return p; } catch {} }
  string[] extra=key=="soffice"?new[]{@"C:\Program Files\LibreOffice\program\soffice.exe"}:key=="ebook-convert"?new[]{@"C:\Program Files\Calibre2\ebook-convert.exe"}:new string[0];
  return extra.FirstOrDefault(File.Exists);
 }
 public List<Route> Routes(string file) {
  string e=Ext(file); var r=new List<Route>();
  if(Backend.Ready){r=Backend.Routes(file);foreach(string f in new[]{"zip","7z","tar","tgz"})if(!r.Any(x=>x.Format==f))r.Add(new Route(f,"Filee","文件打包"));if(e!="gz"&&!r.Any(x=>x.Format=="gz"))r.Add(new Route("gz","内置","GZip 压缩"));return r;}
  if(In(e,"png jpg jpeg bmp gif tif tiff ico")) foreach(string f in "png jpg bmp gif tiff ico pdf".Split(' ')) r.Add(new Route(f,"内置","图片 / 首帧转换"));
  if(In(e,"txt md csv tsv json xml html htm log yaml yml ini")) {
   foreach(string f in "txt html".Split(' ')) r.Add(new Route(f,"内置","UTF-8 文本"));
   if(In(e,"json xml")) r.Add(new Route(e,"内置","校验并美化"));
   if(In(e,"csv tsv")) r.Add(new Route(e=="csv"?"tsv":"csv","内置","保留引号与多行字段"));
  }
  if(e=="zip"||e=="gz") r.Add(new Route("extract","内置","安全解压到新文件夹"));
  if(e!="zip") r.Add(new Route("zip","内置","逐文件 ZIP 打包"));
  if(e!="gz") r.Add(new Route("gz","内置","GZip 压缩"));
  if(Find("ffmpeg")!=null && In(e,"mp4 mkv mov avi webm wmv flv m4v mpg mpeg ts mp3 wav flac aac ogg m4a opus wma aiff png jpg jpeg bmp gif webp avif heic tif tiff")) {
   string formats=In(e,"png jpg jpeg bmp gif webp avif heic tif tiff")?"png jpg webp avif bmp tiff":"mp4 mkv webm mp3 wav flac m4a ogg opus";
   foreach(string f in formats.Split(' ')) if(!r.Any(x=>x.Format==f)) r.Add(new Route(f,"FFmpeg","本机引擎"));
  }
  if(Find("soffice")!=null) {
   string formats=In(e,"doc docx odt rtf txt")?"pdf docx odt rtf txt":In(e,"xls xlsx ods csv")?"pdf xlsx ods csv":In(e,"ppt pptx odp")?"pdf pptx odp":"";
   foreach(string f in formats.Split(new[]{' '},StringSplitOptions.RemoveEmptyEntries)) if(f!=e&&!r.Any(x=>x.Format==f)) r.Add(new Route(f,"LibreOffice","办公文档"));
  }
  if(Find("pandoc")!=null && In(e,"md html htm docx odt rtf epub rst")) foreach(string f in "docx odt html md epub rst".Split(' ')) if(f!=e&&!r.Any(x=>x.Format==f)) r.Add(new Route(f,"Pandoc","结构化文档"));
  if(Find("ebook-convert")!=null && In(e,"epub mobi azw3 fb2 lit pdb txt html docx rtf")) foreach(string f in "epub mobi azw3 fb2 txt pdf".Split(' ')) if(f!=e&&!r.Any(x=>x.Format==f)) r.Add(new Route(f,"calibre","电子书"));
  return r;
 }
 public List<Route> Common(IEnumerable<string> files) { var a=files.ToArray(); if(a.Length==0)return new List<Route>(); return Routes(a[0]).Where(r=>a.All(f=>Routes(f).Any(x=>x.Format==r.Format))).ToList(); }
 public static string Unique(string path) { if(!File.Exists(path)&&!Directory.Exists(path))return path; string d=Path.GetDirectoryName(path),n=Path.GetFileNameWithoutExtension(path),e=Path.GetExtension(path); for(int i=1;;i++){string p=Path.Combine(d,n+" ("+i+")"+e);if(!File.Exists(p)&&!Directory.Exists(p))return p;} }
 public string ConvertFile(string file,string target,CancellationToken token) {
  token.ThrowIfCancellationRequested(); var route=Routes(file).FirstOrDefault(r=>r.Format==target); if(route==null)throw new Exception("该文件没有可用的目标转换路线。");
  if(route.Engine=="Filee"){var result=Backend.Convert(new[]{file},target,Settings,new Recipe(){Quality=Settings.Quality,MaxWidth=Settings.MaxWidth},token,null);var first=result.Files.First();if(first.State!="Done"&&first.State!="Skipped")throw new Exception(first.Error+"："+first.Detail);return first.Outputs.FirstOrDefault()??file;}
  string dir=String.IsNullOrEmpty(Settings.Output)?Path.GetDirectoryName(file):Settings.Output; Directory.CreateDirectory(dir);
  string output=Unique(Path.Combine(dir,(target=="gz"?Path.GetFileName(file):Path.GetFileNameWithoutExtension(file))+ (target=="extract"?"_解压":"."+target)));
  string temp=Path.Combine(dir,".flowshift-"+Guid.NewGuid().ToString("N")+"."+target);
  try {
   if(target=="extract") { Extract(file,temp,token); token.ThrowIfCancellationRequested(); Directory.Move(temp,output); return output; }
   if(target=="zip") { using(var z=ZipFile.Open(temp,ZipArchiveMode.Create)) z.CreateEntryFromFile(file,Path.GetFileName(file),CompressionLevel.Optimal); }
   else if(target=="gz") { using(var source=File.OpenRead(file))using(var dest=File.Create(temp))using(var gz=new GZipStream(dest,CompressionMode.Compress))Copy(source,gz,token); }
   else if(route.Engine=="FFmpeg") {
    string args="-hide_banner -nostdin -y -i "+Q(file)+" ";
    if(In(target,"png jpg webp avif bmp tiff"))args+="-frames:v 1 ";
    if(target=="jpg")args+="-q:v 2 ";
    if(target=="mp4")args+="-c:v libx264 -crf 23 -preset medium -c:a aac -movflags +faststart ";
    if(target=="webm")args+="-c:v libvpx-vp9 -crf 32 -b:v 0 -c:a libopus ";
    if(target=="mp3")args+="-vn -b:a 192k ";
    if(target=="m4a")args+="-vn -c:a aac -b:a 192k ";
    if(In(target,"wav flac ogg opus"))args+="-vn ";
    Run(Find("ffmpeg"),args+Q(temp),token);
   }
   else if(route.Engine=="Pandoc")Run(Find("pandoc"),Q(file)+" -o "+Q(temp),token);
   else if(route.Engine=="calibre")Run(Find("ebook-convert"),Q(file)+" "+Q(temp),token);
   else if(route.Engine=="LibreOffice") {
    string job=Path.Combine(Path.GetTempPath(),"FlowShift-"+Guid.NewGuid().ToString("N")); Directory.CreateDirectory(job);
    try { string profile=new Uri(Path.Combine(job,"profile")).AbsoluteUri; Run(Find("soffice"),"-env:UserInstallation="+Q(profile)+" --headless --convert-to "+target+" --outdir "+Q(job)+" "+Q(file),token); string result=Path.Combine(job,Path.GetFileNameWithoutExtension(file)+"."+target); if(!File.Exists(result))throw new Exception("LibreOffice 未产生目标文件。"); File.Move(result,temp); } finally { try{Directory.Delete(job,true);}catch{} }
   }
   else if(In(Ext(file),"png jpg jpeg bmp gif tif tiff ico"))ImageConvert(file,temp,target);
   else TextConvert(file,temp,target);
   token.ThrowIfCancellationRequested(); if(!File.Exists(temp)||new FileInfo(temp).Length==0)throw new Exception("转换没有生成有效文件。"); File.Move(temp,output); return output;
  } finally { try{if(File.Exists(temp))File.Delete(temp);if(Directory.Exists(temp))Directory.Delete(temp,true);}catch{} }
 }
 static void Copy(Stream input,Stream output,CancellationToken t) { byte[] b=new byte[81920]; int n; while((n=input.Read(b,0,b.Length))>0){t.ThrowIfCancellationRequested();output.Write(b,0,n);} }
 public static string Q(string s) { if(s.Contains("\"")||s.Contains("\r")||s.Contains("\n"))throw new Exception("参数包含无效字符"); return "\""+s.Replace("\\\"","\\\\\"")+"\""; }
 public static void Run(string exe,string args,CancellationToken token) {
  var si=new ProcessStartInfo(exe,args){UseShellExecute=false,CreateNoWindow=true,RedirectStandardError=true,RedirectStandardOutput=true};
  using(var p=Process.Start(si)){var err=p.StandardError.ReadToEndAsync();var stdout=p.StandardOutput.ReadToEndAsync();var timer=Stopwatch.StartNew();
   while(!p.WaitForExit(120)){if(token.IsCancellationRequested||timer.Elapsed.TotalMinutes>30){try{p.Kill();}catch{}p.WaitForExit();token.ThrowIfCancellationRequested();throw new Exception("引擎执行超时（30 分钟）。");}}
   string detail=err.Result+stdout.Result;if(p.ExitCode!=0)throw new Exception("引擎退出 "+p.ExitCode+"："+detail.Substring(Math.Max(0,detail.Length-1600)));
  }
 }
 void ImageConvert(string source,string dest,string target) {
  using(var original=Drawing.Image.FromFile(source)) {
   int width=original.Width,height=original.Height;
   if(Settings.MaxWidth>0&&width>Settings.MaxWidth){height=Math.Max(1,(int)(height*(double)Settings.MaxWidth/width));width=Settings.MaxWidth;}
   if(target=="ico"){double scale=Math.Min(1,256.0/Math.Max(width,height));width=Math.Max(1,(int)(width*scale));height=Math.Max(1,(int)(height*scale));}
   using(var b=new Drawing.Bitmap(width,height)) {
    using(var g=Drawing.Graphics.FromImage(b)){g.Clear(target=="jpg"||target=="pdf"?Drawing.Color.White:Drawing.Color.Transparent);g.InterpolationMode=Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;g.DrawImage(original,0,0,width,height);}
    if(target=="pdf"){using(var ms=new MemoryStream()){b.Save(ms,Drawing.Imaging.ImageFormat.Jpeg);WritePdf(ms.ToArray(),width,height,dest);}return;}
    if(target=="ico"){using(var ms=new MemoryStream()){b.Save(ms,Drawing.Imaging.ImageFormat.Png);byte[] png=ms.ToArray();using(var bw=new BinaryWriter(File.Create(dest))){bw.Write((ushort)0);bw.Write((ushort)1);bw.Write((ushort)1);bw.Write((byte)(width==256?0:width));bw.Write((byte)(height==256?0:height));bw.Write((byte)0);bw.Write((byte)0);bw.Write((ushort)1);bw.Write((ushort)32);bw.Write(png.Length);bw.Write(22);bw.Write(png);}}return;}
    if(target=="jpg"){var enc=Drawing.Imaging.ImageCodecInfo.GetImageEncoders().First(c=>c.MimeType=="image/jpeg");using(var ep=new Drawing.Imaging.EncoderParameters(1)){ep.Param[0]=new Drawing.Imaging.EncoderParameter(Drawing.Imaging.Encoder.Quality,(long)Settings.Quality);b.Save(dest,enc,ep);}return;}
    b.Save(dest,target=="png"?Drawing.Imaging.ImageFormat.Png:target=="bmp"?Drawing.Imaging.ImageFormat.Bmp:target=="gif"?Drawing.Imaging.ImageFormat.Gif:Drawing.Imaging.ImageFormat.Tiff);
   }
  }
 }
 static void WritePdf(byte[] jpeg,int w,int h,string path) {
  using(var f=File.Create(path)){var offsets=new List<long>();Action<string> wr=s=>{byte[] b=Encoding.ASCII.GetBytes(s);f.Write(b,0,b.Length);};Action<int,string> obj=(n,s)=>{offsets.Add(f.Position);wr(n+" 0 obj\n"+s+"\nendobj\n");};
   wr("%PDF-1.4\n");obj(1,"<< /Type /Catalog /Pages 2 0 R >>");obj(2,"<< /Type /Pages /Kids [3 0 R] /Count 1 >>");
   double pw=w*0.75,ph=h*0.75;string size=pw.ToString("0.##",System.Globalization.CultureInfo.InvariantCulture)+" "+ph.ToString("0.##",System.Globalization.CultureInfo.InvariantCulture);
   obj(3,"<< /Type /Page /Parent 2 0 R /MediaBox [0 0 "+size+"] /Resources << /XObject << /Im0 4 0 R >> >> /Contents 5 0 R >>");
   offsets.Add(f.Position);wr("4 0 obj\n<< /Type /XObject /Subtype /Image /Width "+w+" /Height "+h+" /ColorSpace /DeviceRGB /BitsPerComponent 8 /Filter /DCTDecode /Length "+jpeg.Length+" >>\nstream\n");f.Write(jpeg,0,jpeg.Length);wr("\nendstream\nendobj\n");
   string command="q "+size.Replace(" "," 0 0 ")+" 0 0 cm /Im0 Do Q\n";obj(5,"<< /Length "+Encoding.ASCII.GetByteCount(command)+" >>\nstream\n"+command+"endstream");long x=f.Position;wr("xref\n0 6\n0000000000 65535 f \n");foreach(long off in offsets)wr(off.ToString("D10")+" 00000 n \n");wr("trailer\n<< /Size 6 /Root 1 0 R >>\nstartxref\n"+x+"\n%%EOF");
  }
 }
 static string ReadText(string path) { if(new FileInfo(path).Length>32*1024*1024)throw new Exception("文本转换限 32 MiB。");byte[] b=File.ReadAllBytes(path);using(var s=new StreamReader(new MemoryStream(b),new UTF8Encoding(false,true),true))return s.ReadToEnd(); }
 static void TextConvert(string file,string dest,string target) {
  string text=ReadText(file),e=Ext(file);
  if(target=="html")text="<!doctype html><meta charset=\"utf-8\"><title>"+System.Net.WebUtility.HtmlEncode(Path.GetFileName(file))+"</title><pre>"+System.Net.WebUtility.HtmlEncode(text)+"</pre>";
  else if(target=="json") {var j=new JavaScriptSerializer(){MaxJsonLength=32*1024*1024};text=PrettyJson(j.Serialize(j.DeserializeObject(text)));}
  else if(target=="xml"){var doc=new XmlDocument(){XmlResolver=null};using(var sr=new StringReader(text))using(var xr=XmlReader.Create(sr,new XmlReaderSettings(){DtdProcessing=DtdProcessing.Prohibit,XmlResolver=null}))doc.Load(xr);var sb=new StringBuilder();using(var sw=new StringWriter(sb))using(var xw=XmlWriter.Create(sw,new XmlWriterSettings(){Indent=true,OmitXmlDeclaration=true}))doc.Save(xw);text=sb.ToString();}
  else if((e=="csv"&&target=="tsv")||(e=="tsv"&&target=="csv"))text=Delimit(text,e=="csv"?',':'\t',target=="csv"?',':'\t');
  File.WriteAllText(dest,text,new UTF8Encoding(false));
 }
 static string PrettyJson(string s){var b=new StringBuilder();int depth=0;bool quote=false,escape=false;foreach(char c in s){if(quote){b.Append(c);if(escape)escape=false;else if(c=='\\')escape=true;else if(c=='"')quote=false;continue;}if(c=='"'){quote=true;b.Append(c);}else if(c=='{'||c=='['){b.Append(c).AppendLine();depth++;b.Append(' ',depth*2);}else if(c=='}'||c==']'){depth--;b.AppendLine().Append(' ',depth*2).Append(c);}else if(c==',')b.Append(c).AppendLine().Append(' ',depth*2);else if(c==':')b.Append(": ");else b.Append(c);}return b.ToString();}
 static string Delimit(string s,char from,char to){var rows=new List<List<string>>();var row=new List<string>();var field=new StringBuilder();bool quoted=false;for(int i=0;i<s.Length;i++){char c=s[i];if(c=='"'){if(quoted&&i+1<s.Length&&s[i+1]=='"'){field.Append('"');i++;}else if(quoted)quoted=false;else if(field.Length==0)quoted=true;else throw new Exception("分隔文本中的引号不合法。");}else if(!quoted&&(c==from||c=='\r'||c=='\n')){row.Add(field.ToString());field.Clear();if(c!=from){rows.Add(row);row=new List<string>();if(c=='\r'&&i+1<s.Length&&s[i+1]=='\n')i++;}}else field.Append(c);}if(quoted)throw new Exception("分隔文本的引号未闭合。");if(field.Length>0||row.Count>0){row.Add(field.ToString());rows.Add(row);}return String.Join(Environment.NewLine,rows.Select(r=>String.Join(to.ToString(),r.Select(v=>v.IndexOfAny(new[]{to,'"','\r','\n'})>=0?"\""+v.Replace("\"","\"\"")+"\"":v))));}
 static void Extract(string file,string dest,CancellationToken t){Directory.CreateDirectory(dest);if(Ext(file)=="gz"){using(var s=File.OpenRead(file))using(var gz=new GZipStream(s,CompressionMode.Decompress))using(var o=File.Create(Path.Combine(dest,Path.GetFileNameWithoutExtension(file))))LimitedCopy(gz,o,t,2L*1024*1024*1024);return;}
  string root=Path.GetFullPath(dest)+Path.DirectorySeparatorChar;using(var z=ZipFile.OpenRead(file)){if(z.Entries.Count>20000)throw new Exception("压缩包条目超过 20000 个。");long total=0;foreach(var e in z.Entries){t.ThrowIfCancellationRequested();total+=e.Length;if(total>2L*1024*1024*1024)throw new Exception("解压大小超过 2 GiB 安全上限。");string p=Path.GetFullPath(Path.Combine(dest,e.FullName));if(!p.StartsWith(root,StringComparison.OrdinalIgnoreCase)||e.FullName.Contains(":"))throw new Exception("阻止了压缩包中的越界路径。");if(String.IsNullOrEmpty(e.Name)){Directory.CreateDirectory(p);continue;}Directory.CreateDirectory(Path.GetDirectoryName(p));using(var s=e.Open())using(var o=new FileStream(p,FileMode.CreateNew))LimitedCopy(s,o,t,2L*1024*1024*1024);}}}
 static void LimitedCopy(Stream s,Stream o,CancellationToken t,long max){byte[] b=new byte[81920];int n;long size=0;while((n=s.Read(b,0,b.Length))>0){t.ThrowIfCancellationRequested();size+=n;if(size>max)throw new Exception("解压大小超过安全上限。");o.Write(b,0,n);}}
}

public class Job { public string File,Output,State="等待",Error=""; public TextBlock Label; }
public partial class MainWindow : Window {
 public Preferences Pref;public Converter Engine;List<Job> jobs=new List<Job>();List<string> history=new List<string>();
 public FloatingDock Dock;
 StackPanel fileList; TextBlock status,count; ProgressBar progress; ComboBox format,quality,width,modifier; TextBox outbox,distance; Button start,cancel; CancellationTokenSource cts;
 Forms.NotifyIcon tray;Drawing.Icon trayIcon;InstanceInbox inbox; Hook hook; AutoFolder watcher; bool exiting; int finished; string currentPage="";Grid content; Border[] nav;
 public MainWindow(bool preview) {
  try{if(File.Exists(Path.Combine(Preferences.Home,"history.log")))history=File.ReadAllLines(Path.Combine(Preferences.Home,"history.log")).ToList();}catch{}
  Pref=Preferences.Load();Engine=new Converter(Pref);Title="流转 FlowShift · 本地格式工作台";Width=1160;Height=780;MinWidth=950;MinHeight=650;WindowStartupLocation=WindowStartupLocation.CenterScreen;Background=B("#0D111A");Foreground=B("#E9EFF9");FontFamily=new FontFamily("Microsoft YaHei UI");FontSize=13;
  Resources.Add(typeof(Button),StyleButton()); Resources.Add(typeof(ComboBox),StyleCombo());
  var root=new Grid();root.ColumnDefinitions.Add(new ColumnDefinition(){Width=new GridLength(215)});root.ColumnDefinitions.Add(new ColumnDefinition());Content=root;
  var side=new Border(){Background=B("#111722"),BorderBrush=B("#222C3C"),BorderThickness=new Thickness(0,0,1,0)};root.Children.Add(side);var sp=new StackPanel(){Margin=new Thickness(22,30,20,20)};side.Child=sp;
  sp.Children.Add(T("◈  流转",27,"#7EE9D5"));sp.Children.Add(T("F L O W S H I F T",10,"#7C8BA5",new Thickness(2,8,0,34)));
  string[] names={"◫   转换工作台","◎   格式罗盘","◇   引擎中心","↺   转换记录","⚙   偏好设置"};nav=new Border[names.Length];for(int i=0;i<names.Length;i++){int index=i;var n=new Border(){CornerRadius=new CornerRadius(9),Padding=new Thickness(12),Margin=new Thickness(0,4,0,4),Cursor=Cursors.Hand};n.Child=T(names[i],14,"#9BAAC1");n.MouseLeftButtonUp+=(s,e)=>Page(index);nav[i]=n;sp.Children.Add(n);}
  sp.Children.Add(T("LOCAL FIRST",10,"#6E809A",new Thickness(10,80,0,10)));sp.Children.Add(T("●  文件始终留在本机",11,"#7EE9D5",new Thickness(10,0,0,8)));sp.Children.Add(T("原件保留 · 重名自动避让",10,"#72839D",new Thickness(10,0,0,0)));sp.Children.Add(T("v2.4.1  /  Windows",10,"#53647D",new Thickness(10,24,0,0)));
  content=new Grid(){Margin=new Thickness(34,28,34,24)};Grid.SetColumn(content,1);root.Children.Add(content);Page(0);
  AllowDrop=true;DragOver+=(s,e)=>{e.Effects=e.Data.GetDataPresent(DataFormats.FileDrop)?DragDropEffects.Copy:DragDropEffects.None;e.Handled=true;};Drop+=(s,e)=>{if(e.Data.GetDataPresent(DataFormats.FileDrop)){Page(0);AddFiles((string[])e.Data.GetData(DataFormats.FileDrop));}e.Handled=true;};
  if(!preview){trayIcon=AppVisual.LoadIcon();Icon=AppVisual.Image(trayIcon);tray=new Forms.NotifyIcon(){Text="流转 FlowShift · 长按右键打开罗盘",Icon=trayIcon,Visible=true};var menu=new Forms.ContextMenuStrip();menu.Items.Add("显示悬浮坞",null,(s,e)=>ShowDock());menu.Items.Add("打开工作台",null,(s,e)=>ShowMain());menu.Items.Add("触发与右键菜单设置",null,(s,e)=>{ShowMain();Page(4);});menu.Items.Add("关于流转",null,(s,e)=>ShowAbout());menu.Items.Add("退出",null,(s,e)=>Exit());tray.ContextMenuStrip=menu;tray.MouseClick+=(s,e)=>{if(e.Button==Forms.MouseButtons.Left)ShowDock();};
   SourceInitialized+=(s,e)=>{var h=new WindowInteropHelper(this).Handle;HwndSource.FromHwnd(h).AddHook(WndProc);RegisterCompassHotkey();RegisterQuickKeys();hook=new Hook(this);wheelKeys=new WheelKeyHook(this);SetupWatcher();ApplyDesktopMenu();Pref.Save();tray.Visible=false;tray.Visible=true;};Closing+=(s,e)=>{if(!exiting){e.Cancel=true;Hide();tray.ShowBalloonTip(1800,"流转仍在运行","点击托盘图标或贴边标签打开；托盘菜单可退出。",Forms.ToolTipIcon.Info);}};
  }
 }
 bool hotkey;
 bool[] quickRegistered=new bool[9];
 void RegisterQuickKeys(){var hwnd=new WindowInteropHelper(this).Handle;for(int i=0;i<9;i++){Native.UnregisterHotKey(hwnd,11+i);quickRegistered[i]=Pref.QuickTargets!=null&&Pref.QuickTargets.Length>i&&!String.IsNullOrWhiteSpace(Pref.QuickTargets[i])&&Native.RegisterHotKey(hwnd,11+i,0x4003,(uint)(0x31+i));}}
 IntPtr WndProc(IntPtr hwnd,int msg,IntPtr w,IntPtr l,ref bool handled){if(msg==0x312){int id=w.ToInt32();var selected=Native.Selection();if(id>=11&&id<=19&&Dock!=null){int index=id-11;string target=Pref.QuickTargets!=null&&Pref.QuickTargets.Length>index?Pref.QuickTargets[index]:"";if(selected.Length>0&&target!=""){Dock.ShowFiles(selected);if(Engine.Common(selected).Any(r=>r.Format==target))Dock.Start(target);else MessageBox.Show("这批文件没有到 "+target.ToUpperInvariant()+" 的可用路线，请在悬浮坞选择其他格式。");}else ShowCompass(selected);}else ShowCompass(selected);handled=true;}return IntPtr.Zero;}
 public static SolidColorBrush B(string c){var palette=new Dictionary<string,string>{{"#0D111A","#F6F8FC"},{"#111722","#FFFFFF"},{"#151E2C","#FFFFFF"},{"#222C3C","#E5EBF3"},{"#273348","#DFE7F1"},{"#233047","#EDF2FA"},{"#203441","#EAF1FF"},{"#26394B","#F0F5FD"},{"#7EE9D5","#407CE8"},{"#64D8C2","#8BAFF0"},{"#E9EFF9","#31415B"},{"#E6EEF9","#34445E"},{"#F4F7FC","#22324B"},{"#DCE6F5","#475B79"},{"#8C9BB2","#7A8CA7"},{"#9BAAC1","#657B9A"},{"#A9B8CE","#647D9E"},{"#0C302B","#FFFFFF"},{"#FF9A9A","#BE6262"}};string mapped;if(palette.TryGetValue(c,out mapped))c=mapped;return new SolidColorBrush((Color)ColorConverter.ConvertFromString(c));}
 public static TextBlock T(string text,double size,string color,Thickness? margin=null){return new TextBlock(){Text=text,FontSize=size,Foreground=B(color),Margin=margin??new Thickness(0),TextWrapping=TextWrapping.Wrap};}
 Style StyleButton(){var s=new Style(typeof(Button));s.Setters.Add(new Setter(Control.BackgroundProperty,B("#233047")));s.Setters.Add(new Setter(Control.ForegroundProperty,B("#E9EFF9")));s.Setters.Add(new Setter(Control.BorderThicknessProperty,new Thickness(0)));s.Setters.Add(new Setter(Control.PaddingProperty,new Thickness(16,10,16,10)));s.Setters.Add(new Setter(FrameworkElement.MarginProperty,new Thickness(0,0,10,0)));s.Setters.Add(new Setter(Control.CursorProperty,Cursors.Hand));return s;}
 Style StyleCombo(){var s=new Style(typeof(ComboBox));s.Setters.Add(new Setter(Control.ForegroundProperty,B("#263247")));s.Setters.Add(new Setter(Control.PaddingProperty,new Thickness(10,7,10,7)));s.Setters.Add(new Setter(FrameworkElement.MinWidthProperty,100.0));s.Setters.Add(new Setter(FrameworkElement.MarginProperty,new Thickness(0,7,16,0)));return s;}
 Button Btn(string text,Action action,bool accent=false){var b=new Button(){Content=text};if(accent){b.Background=B("#7EE9D5");b.Foreground=B("#0C302B");b.FontWeight=FontWeights.SemiBold;}b.Click+=(s,e)=>action();return b;}
 Border Card(UIElement child,Thickness? margin=null){return new Border(){Child=child,Background=B("#151E2C"),BorderBrush=B("#273348"),BorderThickness=new Thickness(1),CornerRadius=new CornerRadius(12),Padding=new Thickness(20),Margin=margin??new Thickness(0,0,0,16)};}
 StackPanel Base(string eyebrow,string title,string subtitle){content.Children.Clear();var scroll=new ScrollViewer(){VerticalScrollBarVisibility=ScrollBarVisibility.Auto};content.Children.Add(scroll);var p=new StackPanel();scroll.Content=p;p.Children.Add(T(eyebrow,10,"#7EE9D5",new Thickness(0,0,0,9)));p.Children.Add(T(title,30,"#F4F7FC"));p.Children.Add(T(subtitle,12,"#8C9BB2",new Thickness(0,10,0,25)));return p;}
 public void Page(int page){for(int i=0;i<nav.Length;i++)nav[i].Background=B(i==page?"#203441":"#111722");currentPage=page.ToString();
  if(page==0)Workbench();if(page==1)CompassPage();if(page==2)EnginesPage();if(page==3)HistoryPage();if(page==4){SettingsPage();var settingsPanel=(StackPanel)((ScrollViewer)content.Children[0]).Content;DesktopSettings(settingsPanel);GestureSettings(settingsPanel);WatchSettings(settingsPanel);}
 }
 void Workbench(){var p=Base("WORKSPACE  /  01","让文件，顺畅流转。","拖入文件，智能筛选共同目标；一次处理整批任务，结果就放在原件旁。");
  var drop=new StackPanel(){HorizontalAlignment=HorizontalAlignment.Center};drop.Children.Add(T("把文件放到这里  ↧",20,"#E6EEF9",new Thickness(0,0,0,5)));drop.Children.Add(T("图片 · 文本 · 压缩包 · 音视频 · 文档",11,"#8595AD",new Thickness(0,0,0,12)));var adds=new StackPanel(){Orientation=Orientation.Horizontal,HorizontalAlignment=HorizontalAlignment.Center};adds.Children.Add(Btn("＋  选择文件",PickFiles,true));adds.Children.Add(Btn("粘贴文件",()=>{if(Clipboard.ContainsFileDropList())AddFiles(Clipboard.GetFileDropList().Cast<string>());}));drop.Children.Add(adds);p.Children.Add(Card(drop));
  var options=new StackPanel();options.Children.Add(T("转换配方",14,"#E9EFF9"));var row=new WrapPanel(){Margin=new Thickness(0,8,0,0)};
  format=new ComboBox(){Width=155};quality=new ComboBox(){Width=130};foreach(string s in new[]{"高质量 · 90","均衡 · 80","紧凑 · 65"})quality.Items.Add(s);quality.SelectedIndex=Pref.Quality>=90?0:Pref.Quality>=80?1:2;
  width=new ComboBox(){Width=140};foreach(string s in new[]{"保持原始尺寸","最长宽度 1920","最长宽度 1280","最长宽度 800"})width.Items.Add(s);width.SelectedIndex=Pref.MaxWidth==1920?1:Pref.MaxWidth==1280?2:Pref.MaxWidth==800?3:0;
  row.Children.Add(Field("目标格式",format));row.Children.Add(Field("JPEG 质量",quality));row.Children.Add(Field("内置图片宽度",width));options.Children.Add(row);options.Children.Add(T("输出："+(String.IsNullOrEmpty(Pref.Output)?"与原文件同目录":Pref.Output),11,"#8C9BB2",new Thickness(0,12,0,0)));p.Children.Add(Card(options));
  var title=new DockPanel(){LastChildFill=false};count=T("任务队列",14,"#E9EFF9");count.VerticalAlignment=VerticalAlignment.Center;title.Children.Add(count);var clear=Btn("清空",()=>{if(cts!=null)return;jobs.Clear();Refresh();});DockPanel.SetDock(clear,System.Windows.Controls.Dock.Right);title.Children.Add(clear);p.Children.Add(title);
  fileList=new StackPanel();var files=new ScrollViewer(){Content=fileList,Height=110,VerticalScrollBarVisibility=ScrollBarVisibility.Auto,Margin=new Thickness(0,10,0,14)};p.Children.Add(files);
  progress=new ProgressBar(){Height=4,Minimum=0,Maximum=100,Foreground=B("#7EE9D5"),Background=B("#233047"),BorderThickness=new Thickness(0)};p.Children.Add(progress);var bottom=new DockPanel(){Margin=new Thickness(0,16,0,0)};start=Btn("开始转换  →",()=>Start(null),true);cancel=Btn("取消",()=>{if(cts!=null)cts.Cancel();});cancel.IsEnabled=cts!=null;var actions=new StackPanel(){Orientation=Orientation.Horizontal};actions.Children.Add(start);actions.Children.Add(cancel);DockPanel.SetDock(actions,System.Windows.Controls.Dock.Right);bottom.Children.Add(actions);status=T(cts!=null?"正在转换…":"准备就绪 · 所有处理均在本机完成",11,"#8C9BB2",new Thickness(0,11,0,0));bottom.Children.Add(status);p.Children.Add(bottom);Refresh();
 }
 StackPanel Field(string label,UIElement value){var p=new StackPanel();p.Children.Add(T(label,11,"#93A3BA"));p.Children.Add(value);return p;}
 void PickFiles(){var d=new Microsoft.Win32.OpenFileDialog(){Multiselect=true,Title="添加要转换的文件"};if(d.ShowDialog()==true)AddFiles(d.FileNames);}
 public void AddFiles(IEnumerable<string> files){if(cts!=null){MessageBox.Show("请等当前批次完成，或取消后再添加文件。");return;}int skipped=0;foreach(string f in files){if(File.Exists(f)&&!jobs.Any(j=>String.Equals(j.File,f,StringComparison.OrdinalIgnoreCase)))jobs.Add(new Job(){File=Path.GetFullPath(f)});else if(Directory.Exists(f))skipped++;}Refresh();if(skipped>0)status.Text="已跳过文件夹；请选择具体文件。";}
 void Refresh(){if(currentPage!="0"||fileList==null)return;fileList.Children.Clear();count.Text="任务队列  /  "+jobs.Count.ToString("D2");foreach(var j in jobs){var row=new DockPanel(){Margin=new Thickness(0,4,0,4)};var remove=Btn("×",()=>{if(cts==null){jobs.Remove(j);Refresh();}});remove.Padding=new Thickness(8,2,8,2);DockPanel.SetDock(remove,System.Windows.Controls.Dock.Right);row.Children.Add(remove);var label=T(j.State,11,j.State=="完成"?"#7EE9D5":j.State=="失败"?"#FF9A9A":"#8C9BB2");label.Width=80;DockPanel.SetDock(label,System.Windows.Controls.Dock.Right);row.Children.Add(label);j.Label=label;row.Children.Add(T(Path.GetFileName(j.File)+"   ·   "+Size(new FileInfo(j.File).Length),12,"#DCE6F5"));row.ToolTip=j.File+(j.Error==""?"":"\n"+j.Error);fileList.Children.Add(row);}if(jobs.Count==0)fileList.Children.Add(T("队列空空的。拖入一份文件，开始第一次流转。",12,"#5F728E",new Thickness(0,35,0,0)));
  string previous=format.SelectedItem as string;format.Items.Clear();foreach(var r in Engine.Common(jobs.Select(j=>j.File)))format.Items.Add(r.Format.ToUpperInvariant());if(format.Items.Count>0)format.SelectedIndex=0;if(previous!=null&&format.Items.Contains(previous))format.SelectedItem=previous;start.IsEnabled=cts==null&&format.Items.Count>0;}
 public static string Size(long n){return n>1024*1024?(n/1024.0/1024).ToString("0.0")+" MiB":(n/1024.0).ToString("0.0")+" KiB";}
 async void Start(string forced){if(cts!=null||jobs.Count==0)return;string target=forced??(format.SelectedItem as string);if(target==null)return;target=target.ToLowerInvariant();Pref.Quality=quality.SelectedIndex==0?90:quality.SelectedIndex==1?80:65;Pref.MaxWidth=new[]{0,1920,1280,800}[Math.Max(0,width.SelectedIndex)];Pref.Save();cts=new CancellationTokenSource();var token=cts.Token;var batch=jobs.ToArray();finished=0;start.IsEnabled=false;cancel.IsEnabled=true;int errors=0,done=0;
  foreach(var j in batch){if(token.IsCancellationRequested)break;j.State="处理中";UpdateJob(j);try{j.Output=await Task.Run(()=>Engine.ConvertFile(j.File,target,token));j.State="完成";done++;history.Insert(0,DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")+" | "+j.File+" → "+j.Output);SaveHistory();}catch(OperationCanceledException){j.State="已取消";UpdateJob(j);break;}catch(Exception ex){j.State="失败";j.Error=ex.Message;errors++;history.Insert(0,DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")+" | 失败 | "+j.File+" | "+ex.Message);SaveHistory();}finished++;UpdateJob(j);if(progress!=null)progress.Value=100.0*finished/batch.Length;}
  bool canceled=token.IsCancellationRequested;cts.Dispose();cts=null;if(currentPage=="0"){Refresh();cancel.IsEnabled=false;status.Text=String.Format("{0}  ·  成功 {1} / 失败 {2}",canceled?"已取消":"批次完成",done,errors);}if(tray!=null)tray.ShowBalloonTip(2500,"流转 · "+(canceled?"已取消":"处理完成"),"成功 "+done+" 个，失败 "+errors+" 个。可在转换记录中查看路径或错误。",errors>0?Forms.ToolTipIcon.Warning:Forms.ToolTipIcon.Info);
 }
 void UpdateJob(Job j){if(j.Label!=null){j.Label.Text=j.State;j.Label.Foreground=B(j.State=="完成"?"#7EE9D5":j.State=="失败"?"#FF9A9A":"#8C9BB2");j.Label.ToolTip=j.Error;}if(status!=null)status.Text=j.State+" · "+Path.GetFileName(j.File);}
 void SaveHistory(){try{Directory.CreateDirectory(Preferences.Home);File.WriteAllLines(Path.Combine(Preferences.Home,"history.log"),history.Take(300),Encoding.UTF8);}catch{}}
 void CompassPage(){var p=Base("QUICK ACTION  /  02","格式，在指针旁。","长按右键打开轮盘；指向后松开转换。滚动鼠标滚轮翻页，“更多格式”打开悬浮窗口。");var intro=new StackPanel();intro.Children.Add(T(HotkeyText.Format(Pref.CompassModifiers,Pref.CompassKey)+"    /    长按右键 "+Pref.HoldMs+" 毫秒",18,"#7EE9D5"));var actions=new WrapPanel(){Margin=new Thickness(0,12,0,0)};actions.Children.Add(Btn("更改快捷键 / 触发方式",()=>Page(4)));actions.Children.Add(Btn("用当前队列预览轮盘",()=>ShowCompass(jobs.Select(j=>j.File).ToArray()),true));intro.Children.Add(actions);p.Children.Add(Card(intro));WheelPresetEditor(p);}

 public void ShowCompass(string[] files){OpenRadial(files,0);}

 public void FromCompass(string[] files,string target){ShowMain();Page(0);if(cts!=null)return;jobs.Clear();AddFiles(files);format.SelectedItem=target.ToUpperInvariant();Start(target);}
 void EnginesPage(){var p=Base("CAPABILITIES  /  03","能力透明，按需扩展。","内置能力即开即用。高级引擎由你选择本机程序，无后台下载、无捆绑。");
  var builtin=new StackPanel();builtin.Children.Add(T("●  内置引擎  /  已就绪",17,"#7EE9D5"));builtin.Children.Add(T("图片：PNG / JPG / BMP / GIF / TIFF / ICO，图片转单页 PDF。\n文本：UTF-8、HTML 包装、JSON / XML 美化、CSV ↔ TSV。\n压缩：逐文件 ZIP / GZip，ZIP / GZip 安全解压。",12,"#A9B8CE",new Thickness(0,12,0,0)));p.Children.Add(Card(builtin));
  string[,] engines={{"ffmpeg","FFmpeg","图片扩展与音视频 · 官方常见构建约 80–250 MB","https://ffmpeg.org/download.html"},{"soffice","LibreOffice","办公文档 / 表格 / 演示 · 安装通常数百 MB","https://www.libreoffice.org/download/download-libreoffice/"},{"pandoc","Pandoc","Markdown / DOCX / HTML / EPUB · 通常数十至百 MB","https://pandoc.org/installing.html"},{"ebook-convert","calibre","电子书互转 · 安装通常数百 MB","https://calibre-ebook.com/download_windows"}};
  for(int i=0;i<4;i++){string key=engines[i,0],name=engines[i,1],url=engines[i,3];string path=Engine.Find(key);var s=new StackPanel();s.Children.Add(T(name+"   /   "+(path==null?"未配置":"已检测"),16,path==null?"#E9EFF9":"#7EE9D5"));s.Children.Add(T(engines[i,2],11,"#8C9BB2",new Thickness(0,8,0,8)));s.Children.Add(T(path==null?"下载与占用随版本变化；请在官网核对，程序不自动下载。":path+"  ·  "+Size(new FileInfo(path).Length)+"（仅可执行文件）",11,"#6D829F",new Thickness(0,0,0,12)));var row=new StackPanel(){Orientation=Orientation.Horizontal};row.Children.Add(Btn("选择本机 EXE",()=>{var d=new Microsoft.Win32.OpenFileDialog(){Filter="可执行文件|*.exe",Title="选择可信来源的 "+name};if(d.ShowDialog()==true){if(key=="ffmpeg")Pref.Ffmpeg=d.FileName;else if(key=="soffice")Pref.Office=d.FileName;else if(key=="pandoc")Pref.Pandoc=d.FileName;else Pref.Calibre=d.FileName;Pref.Save();EnginesPage();}}));row.Children.Add(Btn("打开官网",()=>Process.Start(new ProcessStartInfo(url){UseShellExecute=true})));s.Children.Add(row);p.Children.Add(Card(s));}
 }
 void HistoryPage(){var p=Base("ACTIVITY  /  04","每一次流转，都有迹可循。","最多保存 300 条本地记录；失败详情保留在这里。记录含原始路径，保存在当前用户配置目录。");try{if(history.Count==0&&File.Exists(Path.Combine(Preferences.Home,"history.log")))history=File.ReadAllLines(Path.Combine(Preferences.Home,"history.log")).ToList();}catch{}var buttons=new StackPanel(){Orientation=Orientation.Horizontal,Margin=new Thickness(0,0,0,18)};buttons.Children.Add(Btn("打开最近输出目录",()=>{var j=jobs.LastOrDefault(x=>File.Exists(x.Output));if(j!=null)Process.Start("explorer.exe","/select,"+Converter.Q(j.Output));}));buttons.Children.Add(Btn("清除记录",()=>{history.Clear();SaveHistory();HistoryPage();}));p.Children.Add(buttons);if(history.Count==0)p.Children.Add(T("还没有转换记录。",14,"#8C9BB2"));foreach(string h in history.Take(300)){var text=new TextBox(){Text=h,IsReadOnly=true,TextWrapping=TextWrapping.Wrap,Background=Brushes.Transparent,Foreground=B("#A9B8CE"),BorderThickness=new Thickness(0)};p.Children.Add(Card(text));}}
 void SettingsPage(){var p=Base("PREFERENCES  /  05","按你的习惯工作。","设置保存到当前用户；不修改系统级配置。");var s=new StackPanel();outbox=new TextBox(){Text=Pref.Output,Padding=new Thickness(10),Margin=new Thickness(0,8,0,12)};s.Children.Add(T("输出目录（留空 = 原文件旁）",13,"#E9EFF9"));s.Children.Add(outbox);s.Children.Add(Btn("浏览目录",()=>{using(var d=new Forms.FolderBrowserDialog()){if(d.ShowDialog()==Forms.DialogResult.OK)outbox.Text=d.SelectedPath;}}));var row=new WrapPanel(){Margin=new Thickness(0,20,0,20)};modifier=new ComboBox();foreach(string m in new[]{"无","Ctrl","Alt","Shift"})modifier.Items.Add(m);modifier.SelectedItem=Pref.Modifier;distance=new TextBox(){Text=Pref.DragDistance.ToString(),Width=100,Padding=new Thickness(10),Margin=new Thickness(0,7,0,0)};row.Children.Add(Field("拖动修饰键",modifier));row.Children.Add(Field("触发距离 / 像素",distance));s.Children.Add(row);var enabled=new CheckBox(){Content="启用全局修饰键拖动手势",IsChecked=Pref.Gesture,Foreground=B("#E9EFF9"),Margin=new Thickness(0,0,0,20)};s.Children.Add(enabled);s.Children.Add(Btn("保存设置",()=>{int n;if(!Int32.TryParse(distance.Text,out n)||n<30||n>500){MessageBox.Show("触发距离须为 30–500 像素。");return;}if(outbox.Text.Contains("\"")){MessageBox.Show("输出目录无效。");return;}Pref.Output=outbox.Text.Trim();Pref.Modifier=modifier.SelectedItem as string??"Ctrl";Pref.DragDistance=n;Pref.Gesture=enabled.IsChecked==true;Pref.Save();MessageBox.Show("设置已保存。");},true));p.Children.Add(Card(s));p.Children.Add(T("图片默认只处理首帧；动画与多页 TIFF 转换会丢失后续帧。\nHTML 导出是转义后的文本包装；TXT 导出保留源文本，不解析 PDF。\n不绕过 DRM、密码或文件损坏限制。更多格式与在线引擎管理在悬浮坞中提供。",12,"#8C9BB2"));}
 void GestureSettings(StackPanel page){var p=new StackPanel();p.Children.Add(T("鼠标触发与快捷配方",19,"#7EE9D5"));var button=new ComboBox(){ItemsSource=new[]{"鼠标左键","鼠标右键","鼠标中键","侧键 X1","侧键 X2"},SelectedIndex=Array.IndexOf(new[]{1,2,4,5,6},Pref.MouseButton),Width=170,HorizontalAlignment=HorizontalAlignment.Left};p.Children.Add(Field("触发按键（配合上面的修饰键）",button));var hold=new TextBox(){Text=Pref.HoldMs.ToString(),Width=170,HorizontalAlignment=HorizontalAlignment.Left,Padding=new Thickness(8),Margin=new Thickness(0,7,0,16)};p.Children.Add(Field("长按毫秒 / 0 为拖动距离触发 / 200–3000 为长按",hold));var boxes=new List<TextBox>();for(int i=0;i<9;i++){var box=new TextBox(){Text=Pref.QuickTargets!=null&&Pref.QuickTargets.Length>i?Pref.QuickTargets[i]:"",Width=150,Padding=new Thickness(7),Margin=new Thickness(0,5,0,8),HorizontalAlignment=HorizontalAlignment.Left};boxes.Add(box);p.Children.Add(Field("Ctrl + Alt + "+(i+1)+(quickRegistered[i]?"  · 已注册":"  · 未注册 / 被占用"),box));}p.Children.Add(T("资源管理器选中文件后按快捷键直接转换。目标填写 png / pdf / docx 等，解压填 extract；留空关闭该配方。",11,"#8C9BB2",new Thickness(0,6,0,16)));p.Children.Add(Btn("保存鼠标与快捷配方",()=>{int ms;if(!Int32.TryParse(hold.Text,out ms)||(ms!=0&&(ms<200||ms>3000))){MessageBox.Show("长按应为 0 或 200–3000 毫秒。");return;}var values=boxes.Select(b=>b.Text.Trim().TrimStart('.').ToLowerInvariant()).ToArray();if(values.Any(v=>v.Any(c=>!Char.IsLetterOrDigit(c)))){MessageBox.Show("目标格式只能包含字母与数字。");return;}if(Backend.Ready&&values.Any(v=>v!=""&&v!="extract"&&v!="gz"&&!Backend.Catalog.Formats.Any(f=>f.Id==v&&f.Writable))){MessageBox.Show("存在未支持的目标格式。");return;}Pref.MouseButton=new[]{1,2,4,5,6}[Math.Max(0,button.SelectedIndex)];Pref.HoldMs=ms;Pref.QuickTargets=values;Pref.Save();RegisterQuickKeys();MessageBox.Show("配置已保存。冲突的热键需先在其他程序解除占用。");},true));page.Children.Add(Card(p,new Thickness(0,20,0,0)));}
 void WatchSettings(StackPanel page){var p=new StackPanel(){Margin=new Thickness(0,18,0,0)};p.Children.Add(T("自动文件夹",19,"#7EE9D5"));p.Children.Add(T("把新文件放进输入目录，稳定后自动转换。只监听顶层新增或重命名文件；不扫描旧文件。",12,"#8C9BB2",new Thickness(0,10,0,16)));
  var source=new TextBox(){Text=Pref.WatchSource,Padding=new Thickness(9),Margin=new Thickness(0,7,0,12)};var output=new TextBox(){Text=Pref.WatchOutput,Padding=new Thickness(9),Margin=new Thickness(0,7,0,12)};var target=new TextBox(){Text=Pref.WatchTarget,Padding=new Thickness(9),Width=120,HorizontalAlignment=HorizontalAlignment.Left,Margin=new Thickness(0,7,0,12)};
  p.Children.Add(Field("输入目录",source));p.Children.Add(Field("独立输出目录（必须与输入目录不同）",output));p.Children.Add(Field("目标扩展名，如 png / mp3 / pdf",target));var enabled=new CheckBox(){Content="启用自动文件夹（程序需保持运行）",IsChecked=Pref.WatchEnabled,Foreground=B("#E9EFF9"),Margin=new Thickness(0,5,0,15)};p.Children.Add(enabled);
  p.Children.Add(Btn("保存自动文件夹",()=>{if(enabled.IsChecked==true){try{AutoFolder.Validate(source.Text.Trim(),output.Text.Trim(),target.Text.Trim().ToLowerInvariant());}catch(Exception ex){MessageBox.Show(ex.Message);return;}}Pref.WatchSource=source.Text.Trim();Pref.WatchOutput=output.Text.Trim();Pref.WatchTarget=target.Text.Trim().TrimStart('.').ToLowerInvariant();Pref.WatchEnabled=enabled.IsChecked==true;Pref.Save();SetupWatcher();MessageBox.Show(Pref.WatchEnabled?"自动文件夹已启用，结果写入转换记录。":"自动文件夹已关闭。");},true));page.Children.Add(Card(p,new Thickness(0,20,0,0)));}
 public void ShowMain(){Show();WindowState=WindowState.Normal;Activate();}
 void SetupWatcher(){if(watcher!=null){watcher.Dispose();watcher=null;}if(!Pref.WatchEnabled)return;try{watcher=new AutoFolder(Pref,message=>Dispatcher.BeginInvoke(new Action(()=>{history.Insert(0,DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")+" | 自动文件夹 | "+message);SaveHistory();})));}catch(Exception ex){history.Insert(0,"自动文件夹未启动："+ex.Message);SaveHistory();}}
 public IEnumerable<string> ReadHistory(){return history.ToArray();}
 public void RecordCore(CoreResult result,string target){foreach(var file in result.Files)history.Insert(0,DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")+" | "+file.Source+" → "+target+" | "+file.State+" | "+String.Join(" ; ",file.Outputs??new string[0])+(file.Error==null?"":" | "+file.Error+" "+file.Detail));SaveHistory();}
 public void StartInbox(LaunchRequest request){inbox=new InstanceInbox(this,request);}
 WheelKeyHook wheelKeys;
 public void Exit(){exiting=true;if(wheelKeys!=null)wheelKeys.Dispose();if(radial!=null)radial.Cancel();if(inbox!=null)inbox.Dispose();if(cts!=null)cts.Cancel();if(Dock!=null)Dock.Stop();if(watcher!=null)watcher.Dispose();if(hook!=null)hook.Dispose();if(tray!=null)tray.Dispose();if(trayIcon!=null)trayIcon.Dispose();Native.UnregisterHotKey(new WindowInteropHelper(this).Handle,1);for(int i=11;i<=19;i++)Native.UnregisterHotKey(new WindowInteropHelper(this).Handle,i);Application.Current.Shutdown();}
}

public class AutoFolder:IDisposable {
 FileSystemWatcher watch;CancellationTokenSource stop=new CancellationTokenSource();SemaphoreSlim gate=new SemaphoreSlim(1,1);HashSet<string> pending=new HashSet<string>(StringComparer.OrdinalIgnoreCase);Converter engine;Action<string> report;string target;
 public static void Validate(string input,string output,string format){if(!Directory.Exists(input))throw new Exception("自动文件夹输入目录不存在。");if(String.IsNullOrWhiteSpace(output))throw new Exception("请选择独立的自动输出目录。");if(Path.GetFullPath(input).TrimEnd('\\','/').Equals(Path.GetFullPath(output).TrimEnd('\\','/'),StringComparison.OrdinalIgnoreCase))throw new Exception("输入与输出目录必须不同，避免反复转换。");if(format.Length==0||format.Any(c=>!Char.IsLetterOrDigit(c)))throw new Exception("目标格式只能包含字母与数字。");}
 public AutoFolder(Preferences p,Action<string> callback){Validate(p.WatchSource,p.WatchOutput,p.WatchTarget);report=callback;target=p.WatchTarget;var copy=new JavaScriptSerializer().Deserialize<Preferences>(new JavaScriptSerializer().Serialize(p));copy.Output=p.WatchOutput;engine=new Converter(copy);Directory.CreateDirectory(copy.Output);watch=new FileSystemWatcher(p.WatchSource){IncludeSubdirectories=false,NotifyFilter=NotifyFilters.FileName,InternalBufferSize=32768};watch.Created+=(s,e)=>Queue(e.FullPath);watch.Renamed+=(s,e)=>Queue(e.FullPath);watch.Error+=(s,e)=>report("监听缓冲区发生错误，请检查遗漏文件并手动添加："+e.GetException().Message);watch.EnableRaisingEvents=true;}
 async void Queue(string path){if(Path.GetFileName(path).StartsWith(".")||InTemp(Converter.Ext(path)))return;lock(pending){if(!pending.Add(path))return;}var token=stop.Token;bool entered=false;try{await gate.WaitAsync(token);entered=true;long last=-1;DateTime stamp=DateTime.MinValue;int stable=0;bool ready=false;
   for(int i=0;i<60;i++){await Task.Delay(1000,token);if(!File.Exists(path))continue;var info=new FileInfo(path);long length=info.Length;DateTime time=info.LastWriteTimeUtc;if(length==last&&time==stamp)stable++;else stable=0;last=length;stamp=time;if(stable<2)continue;try{using(var f=new FileStream(path,FileMode.Open,FileAccess.Read,FileShare.Read)){}ready=true;break;}catch(IOException){}}
   if(!ready)throw new Exception("等待文件写入完成超时。");if(!engine.Routes(path).Any(r=>r.Format==target)){report("跳过无可用路线的文件："+path);return;}string result=await Task.Run(()=>engine.ConvertFile(path,target,token));report(path+" → "+result);
  }catch(OperationCanceledException){}catch(Exception ex){report("失败 | "+path+" | "+ex.Message);}finally{if(entered)gate.Release();lock(pending)pending.Remove(path);}}
 static bool InTemp(string ext){return Converter.In(ext,"tmp part crdownload download");}
 public void Dispose(){watch.Dispose();stop.Cancel();}
}
public static partial class Native {
 public delegate IntPtr MouseProc(int n,IntPtr w,IntPtr l);
 [StructLayout(LayoutKind.Sequential)]public struct POINT{public int x,y;}
 [StructLayout(LayoutKind.Sequential)]public struct MouseInfo{public POINT pt;public uint mouseData,flags,time;public IntPtr extra;}
 [DllImport("user32.dll")]public static extern bool RegisterHotKey(IntPtr h,int id,uint mod,uint key);
 [DllImport("user32.dll")]public static extern bool UnregisterHotKey(IntPtr h,int id);
 [DllImport("user32.dll")]public static extern IntPtr SetWindowsHookEx(int id,MouseProc cb,IntPtr mod,uint thread);
 [DllImport("user32.dll")]public static extern bool UnhookWindowsHookEx(IntPtr h);
 [DllImport("user32.dll")]public static extern IntPtr CallNextHookEx(IntPtr h,int code,IntPtr w,IntPtr l);
 [DllImport("user32.dll")]public static extern short GetAsyncKeyState(int k);
 [DllImport("user32.dll")]public static extern IntPtr GetForegroundWindow();
 [DllImport("user32.dll")]static extern int GetClassName(IntPtr h,StringBuilder s,int n);
 [DllImport("kernel32.dll",CharSet=CharSet.Auto)]public static extern IntPtr GetModuleHandle(string name);
 public static string[] Selection(){object shell=null;try{IntPtr foreground=GetForegroundWindow();shell=Activator.CreateInstance(Type.GetTypeFromProgID("Shell.Application"));dynamic app=shell;var files=new List<string>();foreach(dynamic w in app.Windows()){try{if((long)w.HWND==foreground.ToInt64()){foreach(dynamic item in w.Document.SelectedItems()){string p=item.Path;if(File.Exists(p))files.Add(p);}return files.ToArray();}}catch{}}
  var cls=new StringBuilder(256);GetClassName(foreground,cls,256);if(cls.ToString()=="Progman"||cls.ToString()=="WorkerW"){object location=0,root=0;int hwnd;dynamic desktop=app.Windows().FindWindowSW(ref location,ref root,8,out hwnd,1);if(desktop!=null)foreach(dynamic item in desktop.Document.SelectedItems()){string path=item.Path;if(File.Exists(path))files.Add(path);}}
  return files.ToArray();}catch{return new string[0];}finally{if(shell!=null&&Marshal.IsComObject(shell))Marshal.ReleaseComObject(shell);}}
}
public static class Program {
 [STAThread]public static int Main(string[] args){
  AppContext.SetSwitch("Switch.System.IO.UseLegacyPathHandling",false);AppContext.SetSwitch("Switch.System.IO.BlockLongPaths",false);
  if(args.Contains("--wheel-navigation-test"))return WheelNavigationTests.Run(args[1]);
  if(args.Contains("--render-wheel-settings"))return WheelRegression.RenderSettings(args);
  if(args.Contains("--wheel-regression"))return WheelRegression.Run(args[1]);
  if(args.Contains("--wheel-test"))return WheelTests.Run(args[1]);
  if(args.Contains("--wheel-flow-test"))return WheelTests.Flow(args);
  if(args.Contains("--render-wheel"))return WheelTests.Render(args);
  if(args.Contains("--desktop-test"))return DesktopTests.Run(args[1]);
  if(args.Contains("--render-tray-icon")){using(var icon=AppVisual.LoadIcon())using(var bitmap=icon.ToBitmap())bitmap.Save(args[1],Drawing.Imaging.ImageFormat.Png);return 0;}
  if(args.Contains("--self-test"))return SelfTest.Run(args.Length>1?args[1]:Path.Combine(Path.GetTempPath(),"FlowShift-tests"));
  if(args.Contains("--convert")){try{int index=Array.IndexOf(args,"--to");if(args.Length<4||index<0)throw new Exception("用法：FlowShift.exe --convert 文件 --to 格式 [--output 目录]");var pref=Preferences.Load();int o=Array.IndexOf(args,"--output");if(o>=0)pref.Output=args[o+1];Backend.Initialize(pref).Wait();if(!Backend.Ready)throw new Exception(Backend.Failure);var engine=new Converter(pref);string result=engine.ConvertFile(Path.GetFullPath(args[1]),args[index+1].ToLowerInvariant(),CancellationToken.None);if(o>=0)File.WriteAllText(Path.Combine(pref.Output,"flowshift-cli-result.txt"),result);return 0;}catch(Exception ex){File.WriteAllText(Path.Combine(Path.GetTempPath(),"flowshift-cli-error.txt"),ex.ToString());return 1;}}
  bool floatTest=args.Contains("--float-test");bool floatPreview=args.Contains("--render-float");bool aboutPreview=args.Contains("--render-about");bool preview=args.Contains("--render")||floatPreview||floatTest||aboutPreview;using(var mutex=new Mutex(true,Environment.GetEnvironmentVariable("FLOWSHIFT_INSTANCE_KEY")??"Local\\FlowShift.Desktop.v2")){if(!preview&&!mutex.WaitOne(0)){if(InstanceInbox.TrySend(InstanceInbox.Parse(args)))return 0;MessageBox.Show("检测到旧版流转正在运行。请在旧版悬浮窗右键选择退出，再运行 2.4.1 新版。");return 0;}var app=new Application(){ShutdownMode=ShutdownMode.OnExplicitShutdown};app.DispatcherUnhandledException+=(s,e)=>{MessageBox.Show(e.Exception.Message,"流转 · 操作未完成");e.Handled=true;};var win=new MainWindow(preview);
   if(aboutPreview){var about=new AboutWindow();about.Show();about.UpdateLayout();about.Dispatcher.Invoke(new Action(()=>{}),System.Windows.Threading.DispatcherPriority.ApplicationIdle);var bitmap=new RenderTargetBitmap((int)about.ActualWidth,(int)about.ActualHeight,96,96,PixelFormats.Pbgra32);bitmap.Render(about);var png=new PngBitmapEncoder();png.Frames.Add(BitmapFrame.Create(bitmap));using(var file=File.Create(args[1]))png.Save(file);app.Shutdown();return 0;}
   if(floatTest){Backend.Initialize(win.Pref).Wait();if(!Backend.Ready)return 2;win.Pref.Output=args[3];var dock=new FloatingDock(win,true);dock.ShowFiles(new[]{args[1]});dock.Show();dock.Dispatcher.BeginInvoke(new Action(()=>dock.Start(args[2])));int ticks=0;var timer=new System.Windows.Threading.DispatcherTimer(){Interval=TimeSpan.FromMilliseconds(200)};timer.Tick+=(s,e)=>{if(dock.IsBusy&&ticks++<600)return;timer.Stop();dock.UpdateLayout();dock.Dispatcher.Invoke(new Action(()=>{}),System.Windows.Threading.DispatcherPriority.ApplicationIdle);var bitmap=new RenderTargetBitmap((int)dock.ActualWidth,(int)dock.ActualHeight,96,96,PixelFormats.Pbgra32);bitmap.Render(dock);var png=new PngBitmapEncoder();png.Frames.Add(BitmapFrame.Create(bitmap));using(var stream=File.Create(args[4]))png.Save(stream);File.WriteAllLines(Path.Combine(args[3],"float-test-results.txt"),dock.TestOutputs);app.Shutdown(dock.TestOutputs.Length>0?0:1);};timer.Start();app.Run();return dock.TestOutputs.Length>0?0:1;}
   if(floatPreview){Backend.Initialize(win.Pref).Wait();var floating=new FloatingDock(win,true);if(args.Contains("--expanded"))floating.Expand();int edge=Array.IndexOf(args,"--edge");int sample=Array.IndexOf(args,"--sample");if(sample>=0)floating.ShowFiles(new[]{args[sample+1]});floating.Show();if(edge>=0)floating.HideAtEdge(args[edge+1]);floating.UpdateLayout();floating.Dispatcher.Invoke(new Action(()=>{}),System.Windows.Threading.DispatcherPriority.ApplicationIdle);var bmp=new RenderTargetBitmap((int)floating.ActualWidth,(int)floating.ActualHeight,96,96,PixelFormats.Pbgra32);bmp.Render(floating);var encoder=new PngBitmapEncoder();encoder.Frames.Add(BitmapFrame.Create(bmp));using(var stream=File.Create(args[1]))encoder.Save(stream);app.Shutdown();return 0;}
   if(preview){int page=Array.IndexOf(args,"--page");if(page>=0)win.Page(Int32.Parse(args[page+1]));int sample=Array.IndexOf(args,"--sample");if(sample>=0)win.AddFiles(new[]{args[sample+1]});win.Show();win.UpdateLayout();win.Dispatcher.Invoke(new Action(()=>{}),System.Windows.Threading.DispatcherPriority.ApplicationIdle);var bmp=new RenderTargetBitmap((int)win.ActualWidth,(int)win.ActualHeight,96,96,PixelFormats.Pbgra32);bmp.Render(win);var enc=new PngBitmapEncoder();enc.Frames.Add(BitmapFrame.Create(bmp));using(var stream=File.Create(args[1]))enc.Save(stream);app.Shutdown();return 0;}
   var launch=InstanceInbox.Parse(args);bool workspace=launch.Kind=="open"||args.Contains("--workspace");if(workspace)win.ShowMain();else{win.WindowState=WindowState.Minimized;win.Show();win.Hide();win.WindowState=WindowState.Normal;}win.Dock=new FloatingDock(win,false);win.Dock.Show();win.StartInbox(launch);if(workspace)win.Activate();Task.Run(async()=>{await Backend.Initialize(win.Pref);var notice=win.Dispatcher.BeginInvoke(new Action(()=>{if(win.Dock!=null)win.Dock.CoreReady();}));});app.Run();return 0;
  }
 }
}
public static class SelfTest {
 public static int Run(string dir){var log=new List<string>();try{Directory.CreateDirectory(dir);var pref=new Preferences(){Output=Path.Combine(dir,"results")};var c=new Converter(pref);var t=CancellationToken.None;Directory.CreateDirectory(pref.Output);
  Action<bool,string> check=(ok,name)=>{if(!ok)throw new Exception("FAIL "+name);log.Add("PASS "+name);};
  string image=Path.Combine(dir,"image.png");using(var b=new Drawing.Bitmap(80,40)){using(var g=Drawing.Graphics.FromImage(b))g.Clear(Drawing.Color.Coral);b.Save(image,Drawing.Imaging.ImageFormat.Png);}
  foreach(string f in new[]{"jpg","bmp","gif","tiff","png"}){string p=c.ConvertFile(image,f,t);using(var img=Drawing.Image.FromFile(p))check(img.Width==80&&img.Height==40,"image "+f);}
  pref.MaxWidth=20;string resized=c.ConvertFile(image,"png",t);using(var img=Drawing.Image.FromFile(resized))check(img.Width==20&&img.Height==10,"resize aspect ratio");pref.MaxWidth=0;
  string ico=c.ConvertFile(image,"ico",t);check(File.ReadAllBytes(ico)[2]==1,"ICO header");string pdf=c.ConvertFile(image,"pdf",t);check(File.ReadAllText(pdf).StartsWith("%PDF-1.4")&&File.ReadAllText(pdf).Contains("xref"),"PDF structure");
  string text=Path.Combine(dir,"note.txt");File.WriteAllText(text,"<script>你好</script>",new UTF8Encoding(false));string html=c.ConvertFile(text,"html",t);check(File.ReadAllText(html).Contains("&lt;script&gt;"),"HTML escaping");
  string zip=c.ConvertFile(text,"zip",t);string extracted=c.ConvertFile(zip,"extract",t);check(File.ReadAllText(Path.Combine(extracted,"note.txt"))==File.ReadAllText(text),"ZIP roundtrip");string gz=c.ConvertFile(text,"gz",t);string un=c.ConvertFile(gz,"extract",t);check(File.ReadAllText(Path.Combine(un,"note.txt"))==File.ReadAllText(text),"GZip roundtrip");
  string json=Path.Combine(dir,"test.json");File.WriteAllText(json,"{\"a\":1,\"text\":\"你好\"}");check(File.ReadAllText(c.ConvertFile(json,"json",t)).Contains("\n"),"JSON formatting");
  string csv=Path.Combine(dir,"table.csv");File.WriteAllText(csv,"a,b\r\n\"x,y\",\"line1\nline2\"",new UTF8Encoding(false));string tsv=c.ConvertFile(csv,"tsv",t);string back=c.ConvertFile(tsv,"csv",t);check(File.ReadAllText(back).Contains("\"x,y\"")&&File.ReadAllText(back).Contains("line1\nline2"),"CSV quoted multiline roundtrip");
  string bad=Path.Combine(dir,"unsafe.zip");if(File.Exists(bad))File.Delete(bad);using(var z=ZipFile.Open(bad,ZipArchiveMode.Create))using(var w=new StreamWriter(z.CreateEntry("../escape.txt").Open()))w.Write("bad");bool rejected=false;try{c.ConvertFile(bad,"extract",t);}catch{rejected=true;}check(rejected&&!File.Exists(Path.Combine(pref.Output,"escape.txt")),"ZIP traversal rejected");
  string xml=Path.Combine(dir,"unsafe.xml");File.WriteAllText(xml,"<!DOCTYPE a [<!ENTITY x SYSTEM 'file:///C:/Windows/win.ini'>]><a>&x;</a>");rejected=false;try{c.ConvertFile(xml,"xml",t);}catch{rejected=true;}check(rejected,"XML external entity rejected");
  var canceled=new CancellationTokenSource();canceled.Cancel();rejected=false;try{c.ConvertFile(image,"jpg",canceled.Token);}catch(OperationCanceledException){rejected=true;}check(rejected,"cancellation");check(!Directory.GetFiles(pref.Output,".flowshift-*").Any()&&!Directory.GetDirectories(pref.Output,".flowshift-*").Any(),"temporary output cleanup");
  check(File.Exists(text)&&File.ReadAllText(text)=="<script>你好</script>","original preserved");check(c.Common(new[]{image,text}).All(r=>r.Format=="zip"||r.Format=="gz"),"mixed selection intersection");
  string collision=c.ConvertFile(image,"png",t);check(collision!=resized&&File.Exists(resized),"existing output preserved");
  string malformed=Path.Combine(dir,"malformed.json");File.WriteAllText(malformed,"{bad");rejected=false;try{c.ConvertFile(malformed,"json",t);}catch{rejected=true;}check(rejected,"invalid JSON rejected");
  string safeXml=Path.Combine(dir,"safe.xml");File.WriteAllText(safeXml,"<root><item>你好</item></root>");check(File.ReadAllText(c.ConvertFile(safeXml,"xml",t)).Contains("你好"),"XML valid document");
  rejected=false;try{c.ConvertFile(image,"docx",t);}catch{rejected=true;}check(rejected,"unsupported route rejected");
  if(c.Find("ffmpeg")!=null){string wav=Path.Combine(dir,"tone.wav");Converter.Run(c.Find("ffmpeg"),"-v error -y -f lavfi -i sine=frequency=440:duration=0.3 "+Converter.Q(wav),t);string mp3=c.ConvertFile(wav,"mp3",t);check(new FileInfo(mp3).Length>100,"real FFmpeg audio conversion");Converter.Run(c.Find("ffmpeg"),"-v error -i "+Converter.Q(mp3)+" -f null -",t);check(true,"FFmpeg output decode");}
  string watchSource=Path.Combine(dir,"watch-in"),watchOut=Path.Combine(dir,"watch-out");Directory.CreateDirectory(watchSource);Directory.CreateDirectory(watchOut);File.WriteAllText(Path.Combine(watchSource,"existing.txt"),"existing");var watchPref=new Preferences(){WatchSource=watchSource,WatchOutput=watchOut,WatchTarget="html"};using(var signal=new ManualResetEvent(false))using(var watcher=new AutoFolder(watchPref,message=>signal.Set())){File.WriteAllText(Path.Combine(watchSource,"incoming.txt"),"自动转换",new UTF8Encoding(false));check(signal.WaitOne(12000)&&File.Exists(Path.Combine(watchOut,"incoming.html")),"watch folder real conversion");check(!File.Exists(Path.Combine(watchOut,"existing.html")),"watch folder does not scan existing files");}
  rejected=false;try{AutoFolder.Validate(watchSource,watchSource,"png");}catch{rejected=true;}check(rejected,"watch feedback loop rejected");
  File.WriteAllLines(Path.Combine(dir,"test-results.txt"),log);return 0;
 }catch(Exception ex){log.Add(ex.ToString());File.WriteAllLines(Path.Combine(dir,"test-results.txt"),log);return 1;}}
}
}
