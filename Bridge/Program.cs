using System.Text;
using System.Text.Json;
using Filee.Core.Conversion;
using Filee.Core.Formats;
using Filee.Core.Presets;
using Filee.Engines;
using Filee.Engines.Infrastructure;

Console.OutputEncoding = new UTF8Encoding(false);
var json = new JsonSerializerOptions { PropertyNameCaseInsensitive = true, WriteIndented = false };
var data = Environment.GetEnvironmentVariable("FLOWSHIFT_DATA") ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"FlowShift","CoreData");
Directory.CreateDirectory(data);
EngineEnvironment.DownloadRoot = Path.Combine(data,"engines");
var own = Environment.GetEnvironmentVariable("FLOWSHIFT_ENGINES");
if(!string.IsNullOrEmpty(own)&&File.Exists(own))EngineEnvironment.OwnCopies=JsonSerializer.Deserialize<Dictionary<string,string>>(File.ReadAllText(own),json)??[];
var converters=EngineRegistry.CreateAll(new EngineEnvironment(data));
var catalog=new ConverterCatalog(converters);
try {
 if(args.Length<1)throw new Exception("Expected catalog, convert, packages or install.");
 if(args[0]=="catalog"){
  var planner=catalog.CreatePlanner();
  var formats=FormatRegistry.Known.Select(f=>new {
   f.Id,Name=f.DisplayName,Category=f.Category.ToString(),Extensions=f.Extensions, f.Writable,
   Targets=FormatRegistry.Known.Where(t=>t.Writable&&planner.CanConvert(f.Id,t.Id)).Select(t=>t.Id).Concat(planner.CanConvert(f.Id,FormatRegistry.Folder)?[FormatRegistry.Folder]:Array.Empty<string>()).ToArray()
  }).ToArray();
  var engines=converters.Select(e=>new{e.Id,Name=e.DisplayName,Available=catalog.StatusOf(e).IsAvailable,Reason=catalog.StatusOf(e).ReasonKey,Detail=catalog.StatusOf(e).Detail}).ToArray();
  File.WriteAllText(args[1],JsonSerializer.Serialize(new{Formats=formats,Engines=engines},json),new UTF8Encoding(false));return 0;
 }
 if(args[0]=="packages"){
  File.WriteAllText(args[1],JsonSerializer.Serialize(EngineDownloads.Packages.Select(p=>new{p.Id,Download=EngineDownloads.DownloadSize(p),Disk=p.InstalledSize,Available=EngineDownloads.CanDownload(p),Installed=new EngineInstaller(new HttpClient()).IsInstalled(p)||p.ConverterIds.All(id=>catalog.All.Any(e=>e.Id==id&&catalog.StatusOf(e).IsAvailable))}),json));return 0;
 }
 if(args[0]=="install"){
  var package=EngineDownloads.Packages.Single(p=>p.Id==args[1]);using var http=new HttpClient(){Timeout=TimeSpan.FromMinutes(30)};using var cts=new CancellationTokenSource();using var timer=new Timer(_=>{if(args.Length>2&&File.Exists(args[2]))cts.Cancel();},null,0,250);
  await new EngineInstaller(http).InstallAsync(package,new Progress<EngineInstallProgress>(p=>Console.WriteLine(JsonSerializer.Serialize(new{Stage=p.Stage.ToString(),Value=p.Fraction,Done=p.BytesDone,Total=p.BytesTotal}))),cts.Token);return 0;
 }
 if(args[0]!="convert")throw new Exception("Unknown command.");
 var request=JsonSerializer.Deserialize<Request>(File.ReadAllText(args[1]),json)??throw new Exception("Empty request");
 var preset=new Preset(){TargetFormat=request.Target};
 if(!string.IsNullOrWhiteSpace(request.Output)){preset.Output.Location=OutputLocation.CustomFolder;preset.Output.CustomFolder=request.Output;}else preset.Output.Location=OutputLocation.SameFolder;
 preset.Output.Conflict=ConflictPolicy.Rename;preset.Output.KeepDates=request.KeepDates;preset.Output.FileNamePattern=string.IsNullOrWhiteSpace(request.NamePattern)?"{name}":request.NamePattern;
 preset.Image.Quality=Math.Clamp(request.Quality,1,100);if(request.MaxWidth>0){preset.Image.Resize=ResizeMode.Width;preset.Image.Width=request.MaxWidth;}preset.Image.Grayscale=request.Grayscale;preset.Image.KeepMetadata=!request.StripMetadata;
 if(request.Dpi>0)preset.Image.Dpi=request.Dpi;
 if(request.Rotate==90)preset.Image.Rotation=ImageRotation.Right90;else if(request.Rotate==180)preset.Image.Rotation=ImageRotation.Rotate180;else if(request.Rotate==270)preset.Image.Rotation=ImageRotation.Left90;
 preset.Pdf.MergeIntoSingle=request.Merge;preset.Pdf.PageRange=request.Pages??"";preset.Pdf.SplitPages=request.Split;preset.Pdf.CombinePages=request.LongImage;
 preset.Media.MaxHeight=request.VideoHeight;preset.Media.AudioBitrateKbps=request.AudioBitrate;preset.Media.RemoveAudio=request.RemoveAudio;preset.Media.HardwareEncoding=request.Hardware;preset.Media.SaveEveryFrame=request.Frames;preset.Image.RemoveBackground=request.RemoveBackground;
 preset.Archive.CombineIntoOne=request.Combine || (new[]{"zip","7z","tar","tgz","tbz2","txz"}.Contains(request.Target) && request.Files.Any(f=>FormatRegistry.Detect(f) is not { } source || !catalog.CreatePlanner().CanConvert(source.Id,request.Target)));
 if(request.Compress)preset.Pdf.Compression=PdfCompression.Ebook;
 await using var queue=new JobQueue(catalog,EngineRegistry.FindPdfMerger(converters),combiner:EngineRegistry.FindFileCombiner(converters),tiffMerger:EngineRegistry.FindTiffMerger(converters));
 var job=new ConversionJob(request.Files,preset,"FlowShift");
 using var cancelTimer=new Timer(_=>{if(File.Exists(args[1]+".cancel"))job.Cancel();},null,0,200);
 var sync=new object();double last=-1;
 queue.JobUpdated+=(_,j)=>{lock(sync){if(Math.Abs(j.Progress-last)>0.005||j.State is JobState.Completed or JobState.CompletedWithErrors or JobState.Failed or JobState.Cancelled){last=j.Progress;Console.WriteLine(JsonSerializer.Serialize(new{Value=j.Progress,State=j.State.ToString(),Files=j.Files.Select(f=>new{Path=f.SourcePath,State=f.State.ToString(),f.Progress})}));}}};
 await queue.RunAsync(job);
 var result=new{State=job.State.ToString(),Files=job.Files.Select(f=>new{Source=f.SourcePath,State=f.State.ToString(),Outputs=f.Outputs,Error=f.ErrorKey,Detail=f.ErrorDetail,Notice=f.NoticeKey})};
 File.WriteAllText(args[1]+".result.json",JsonSerializer.Serialize(result,json),new UTF8Encoding(false));
 return job.Files.All(f=>f.State==FileState.Done||f.State==FileState.Skipped)?0:1;
}catch(Exception ex){Console.Error.WriteLine(ex.ToString());return 2;}

class Request {
 public string[] Files{get;set;}=[];public string Target{get;set;}="png";public string Output{get;set;}="";
 public int Quality{get;set;}=90;public int MaxWidth{get;set;}=0;public int Dpi{get;set;}=0;public int Rotate{get;set;}=0;
 public bool Grayscale{get;set;}=false;public bool StripMetadata{get;set;}=false;public bool Merge{get;set;}=false;public bool Combine{get;set;}=false;public bool Compress{get;set;}=false;public bool KeepDates{get;set;}=true;
 public bool Split{get;set;}=false;public bool LongImage{get;set;}=false;public bool RemoveAudio{get;set;}=false;public bool Hardware{get;set;}=false;public bool Frames{get;set;}=false;public bool RemoveBackground{get;set;}=false;public int VideoHeight{get;set;}=0;public int AudioBitrate{get;set;}=0;
 public string Pages{get;set;}="";public string NamePattern{get;set;}="{name}";
}
