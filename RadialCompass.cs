using System;
using System.Linq;
using System.IO;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using Forms=System.Windows.Forms;
namespace FlowShift {
public static class WheelModel {
 public const string More="__more";
 public static string[] Targets(IEnumerable<Route> routes,string category){
  var available=routes.Select(r=>r.Format).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
  var orders=new Dictionary<string,string>{{"Image","png webp pdf jpg ico bmp tiff avif"},{"Pdf","docx png jpg txt pptx html pdf"},{"Video","mp4 mkv mov webm gif mp3 wav"},{"Audio","mp3 wav flac m4a ogg opus aac"},{"Document","pdf docx txt html epub odt png"},{"Spreadsheet","xlsx csv pdf json ods html txt"},{"Presentation","pdf pptx png jpg odp docx"},{"Font","ttf otf woff woff2 eot"},{"Cad","dxf dwg pdf svg png"},{"Archive","extract zip 7z tar tgz"},{"Ebook","epub pdf docx txt mobi azw3"},{"Model3D","glb gltf obj stl ply 3mf"},{"Data","json yaml toml xml csv xlsx"},{"Subtitle","srt ass vtt lrc"},{"Geo","geojson kml kmz gpx csv xlsx"},{"Vector","svg png pdf jpg dxf"}};
  string order=orders.ContainsKey(category??"")?orders[category]:"png webp pdf jpg docx txt zip extract";
  var priority=order.Split(' ');return available.OrderBy(f=>Array.IndexOf(priority,f)<0?100:Array.IndexOf(priority,f)).ThenBy(f=>f,StringComparer.OrdinalIgnoreCase).Take(7).Concat(new[]{More}).ToArray();
 }
 public static Point Polar(double radius,double degrees){double angle=degrees*Math.PI/180;return new Point(230+radius*Math.Cos(angle),230+radius*Math.Sin(angle));}
 public static int Hit(Point point,int count){double x=point.X-230,y=point.Y-230,r=Math.Sqrt(x*x+y*y);if(count<1||r<94||r>220)return -1;double step=360.0/count;double angle=(Math.Atan2(y,x)*180/Math.PI+90+step/2+360)%360;int index=(int)(angle/step);double within=angle-index*step;if(within<1.3||within>step-1.3)return -1;return Math.Min(index,count-1);}
 public static string Summary(string[] files){var types=files.Select(f=>Path.GetExtension(f).TrimStart('.').ToUpperInvariant()).Distinct().ToArray();return files.Length+" "+(types.Length==1&&types[0]!=""?types[0]:"个文件");}
 public static Rect Placement(Rect work,Point cursor,double size){size=Math.Min(size,Math.Min(work.Width-12,work.Height-12));return new Rect(Math.Max(work.Left+6,Math.Min(cursor.X-size/2,work.Right-size-6)),Math.Max(work.Top+6,Math.Min(cursor.Y-size/2,work.Bottom-size-6)),size,size);}
}
public sealed class WheelSurface:FrameworkElement {
 public string[] Targets;public int PageIndex,PageCount=1;public int Hover=-1;readonly string summary;readonly bool holding;readonly int total;
 public WheelSurface(string[] targets,string[] files,int count,bool hold){Targets=targets;summary=WheelModel.Summary(files);holding=hold;total=count;Width=460;Height=460;Focusable=true;}
 static Brush B(string value){return (Brush)new BrushConverter().ConvertFromString(value);}
 void Label(DrawingContext dc,string text,double size,string color,Point center,bool bold){var label=new FormattedText(text,CultureInfo.CurrentUICulture,FlowDirection.LeftToRight,new Typeface(new FontFamily("Microsoft YaHei UI"),FontStyles.Normal,bold?FontWeights.SemiBold:FontWeights.Normal,FontStretches.Normal),size,B(color),VisualTreeHelper.GetDpi(this).PixelsPerDip);dc.DrawText(label,new Point(center.X-label.Width/2,center.Y-label.Height/2));}
 public static Geometry Segment(int index,int count,double outer){double step=360.0/count,start=-90+index*step-step/2+1.3,end=start+step-2.6;var geometry=new StreamGeometry();using(var c=geometry.Open()){c.BeginFigure(WheelModel.Polar(outer,start),true,true);c.ArcTo(WheelModel.Polar(outer,end),new Size(outer,outer),0,end-start>180,SweepDirection.Clockwise,true,false);c.LineTo(WheelModel.Polar(94,end),true,false);c.ArcTo(WheelModel.Polar(94,start),new Size(94,94),0,end-start>180,SweepDirection.Counterclockwise,true,false);}geometry.Freeze();return geometry;}
 protected override void OnRender(DrawingContext dc){
  dc.DrawRectangle(Brushes.Transparent,null,new Rect(0,0,460,460));
  dc.DrawEllipse(B("#E1E5ED"),null,new Point(230,234),211,211);
  for(int i=0;i<Targets.Length;i++){bool active=i==Hover;var fill=active?new LinearGradientBrush(Color.FromRgb(139,125,224),Color.FromRgb(110,104,208),50):new LinearGradientBrush(Color.FromRgb(255,255,255),Color.FromRgb(243,245,249),70);dc.DrawGeometry(fill,new Pen(B(active?"#DFD9FF":"#FFFFFF"),1.8),Segment(i,Targets.Length,active?218:207));Point position=WheelModel.Polar(active?159:152,-90+i*360.0/Targets.Length);string title=Targets[i]==WheelModel.More?"更多格式":Targets[i]=="extract"?"解压":Targets[i].ToUpperInvariant();Label(dc,title,Targets[i]==WheelModel.More?16:19,active?"#FFFFFF":"#303744",position,true);if(Targets[i]==WheelModel.More)Label(dc,"全部 "+total+" 项",10,active?"#EAE6FF":"#8E97A8",new Point(position.X,position.Y+24),false);}
  dc.DrawEllipse(B("#FFFFFF"),new Pen(B("#EEF0F6"),1),new Point(230,230),83,83);
  Label(dc,PageCount>1?"‹    "+(PageIndex+1)+" / "+PageCount+"    ›":"1 / 1",14,"#8076D8",new Point(230,191),true);
  Label(dc,summary.Length>17?summary.Substring(0,15)+"…":summary,summary.Length>12?14:19,"#313947",new Point(230,226),true);
  Label(dc,Hover<0?(holding?"指向格式 · 松开转换":"点击格式开始转换"):Targets[Hover]==WheelModel.More?"打开桌面悬浮窗口":"→ "+Targets[Hover].ToUpperInvariant(),11,"#7D8799",new Point(230,253),false);
  Label(dc,PageCount>1?"滚轮 / 方向键翻页":"仅此一页 · Esc 取消",10,"#A0A8B7",new Point(230,274),false);
 }
 public void PointAt(Point point){int value=WheelModel.Hit(point,Targets.Length);if(value!=Hover){Hover=value;InvalidateVisual();}}
}
public sealed class RadialCompass:Window {
 public readonly WheelSurface Surface;public string[] Targets{get;private set;}readonly List<string[]> pages;public int PageIndex{get;private set;}public int PageCount{get{return pages.Count;}}readonly Action<string> chosen;readonly int button;readonly bool preview;internal bool DeferSelection;readonly WheelScroll scrollInput=new WheelScroll();public bool IsLive{get{return !finished;}}DispatcherTimer timer;bool finished;DateTime opened=DateTime.UtcNow;
 public RadialCompass(string[] files,List<Route> routes,string category,int heldButton,Action<string> action,bool screenshot):this(files,routes,category,heldButton,action,screenshot,new Preferences()){}
 public RadialCompass(string[] files,List<Route> routes,string category,int heldButton,Action<string> action,bool screenshot,Preferences preferences){
  button=heldButton;chosen=action;preview=screenshot;DeferSelection=!screenshot;pages=WheelPresets.Build(preferences,routes,category);Targets=pages[0];Surface=new WheelSurface(Targets,files,routes.Select(r=>r.Format).Distinct().Count(),button!=0);Surface.PageCount=pages.Count;
  Title="流转 · 格式轮盘";Width=Height=460;WindowStyle=WindowStyle.None;ResizeMode=ResizeMode.NoResize;AllowsTransparency=true;Background=Brushes.Transparent;Topmost=true;ShowInTaskbar=false;Content=new Viewbox(){Child=Surface,Stretch=Stretch.Uniform};
  var pointer=Forms.Cursor.Position;var work=Forms.Screen.FromPoint(pointer).WorkingArea;double scale=1;var source=Application.Current==null?null:PresentationSource.FromVisual(Application.Current.MainWindow);if(source!=null&&source.CompositionTarget!=null)scale=source.CompositionTarget.TransformToDevice.M11;Rect bounds=WheelModel.Placement(new Rect(work.Left/scale,work.Top/scale,work.Width/scale,work.Height/scale),new Point(pointer.X/scale,pointer.Y/scale),460);Left=bounds.Left;Top=bounds.Top;Width=Height=bounds.Width;
  PreviewMouseRightButtonUp+=(s,e)=>{e.Handled=true;if(button==2)ConfirmAt(e.GetPosition(Surface));};PreviewMouseWheel+=(s,e)=>{Scroll(e.Delta);e.Handled=true;};
  MouseMove+=(s,e)=>Surface.PointAt(e.GetPosition(Surface));MouseLeftButtonUp+=(s,e)=>{if(button==0){Point point=e.GetPosition(Surface);if(PageCount>1&&point.Y>165&&point.Y<210&&Math.Abs(point.X-230)>25&&WheelModel.Hit(point,Targets.Length)<0)ChangePage(point.X>230?1:-1);else ConfirmAt(point);e.Handled=true;}};PreviewKeyDown+=(s,e)=>{if(e.Key==Key.Escape){Cancel();e.Handled=true;}else {int direction=WheelNavigation.Direction(System.Windows.Input.KeyInterop.VirtualKeyFromKey(e.Key));if(direction!=0){ChangePage(direction);e.Handled=true;}}};
  if(!preview){Deactivated+=(s,e)=>Cancel();Loaded+=(s,e)=>{Activate();Surface.Focus();timer=new DispatcherTimer(){Interval=TimeSpan.FromMilliseconds(20)};timer.Tick+=(a,b)=>Tick();timer.Start();};}
  Closed+=(s,e)=>{finished=true;if(timer!=null)timer.Stop();};
 }
 void Tick(){if(finished)return;if((Native.GetAsyncKeyState(0x1B)&0x8000)!=0||(DateTime.UtcNow-opened).TotalSeconds>45){Cancel();return;}var pointer=Forms.Cursor.Position;Point local=Surface.PointFromScreen(new Point(pointer.X,pointer.Y));Surface.PointAt(local);if(button!=0&&(Native.GetAsyncKeyState(button)&0x8000)==0)ConfirmAt(local);}
 public void ReleaseScreen(Point point){if(button!=0&&!finished)ConfirmAt(Surface.PointFromScreen(point));}
 public void Scroll(int delta){int steps=scrollInput.Steps(delta);if(steps!=0)ChangePage(steps);}
 public void ChangePage(int delta){if(finished||pages.Count<2)return;PageIndex=(PageIndex+delta%pages.Count+pages.Count)%pages.Count;Targets=pages[PageIndex];Surface.Targets=Targets;Surface.PageIndex=PageIndex;Surface.Hover=-1;Surface.InvalidateVisual();}
 public void ConfirmAt(Point point){if(finished)return;Surface.PointAt(point);int index=Surface.Hover;string target=index>=0?Targets[index]:null;finished=true;Close();if(target!=null&&chosen!=null){if(!DeferSelection)chosen(target);else Dispatcher.BeginInvoke(DispatcherPriority.ContextIdle,new Action(()=>{Native.ReleaseOurMouseCapture();chosen(target);}));}}
 public void Cancel(){if(!finished){finished=true;Close();}}
}
public partial class MainWindow {
 RadialCompass radial;
 public void ShowGestureCompass(string[] files,int button){OpenRadial(files,button);}
 public void ReleaseCompass(Native.POINT point){if(radial!=null)radial.ReleaseScreen(new Point(point.x,point.y));}
 void OpenRadial(string[] files,int button){if(cts!=null||Dock!=null&&Dock.IsBusy)return;if(radial!=null)radial.Cancel();if(files==null||files.Length==0)files=Native.Selection();var valid=(files??new string[0]).Where(File.Exists).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();if(valid.Length==0){if(Dock!=null){Dock.Reveal();Dock.Expand();}return;}var routes=Engine.Common(valid);var popup=new RadialCompass(valid,routes,WheelPresets.Category(valid),button,target=>RunWheelChoice(valid,target),false,Pref);radial=popup;popup.Closed+=(s,e)=>{if(radial==popup)radial=null;};popup.Show();}
 public void RunWheelChoice(string[] files,string target){if(Dock==null||Dock.IsBusy)return;Native.ReleaseOurMouseCapture();if(target==WheelModel.More){Dock.Reveal();Dock.ShowAllFormats(files);Dock.FocusAfterCompass();return;}if(!Engine.Common(files).Any(r=>r.Format==target))return;Dock.Reveal();Dock.ShowFiles(files);Dock.Start(target);}
}
public partial class FloatingDock {
 public void ShowAllFormats(string[] paths){showAll=true;ShowFiles(paths);}
 public void FocusAfterCompass(){Dispatcher.BeginInvoke(DispatcherPriority.ContextIdle,new Action(()=>{Native.ReleaseOurMouseCapture();Activate();if(search!=null){search.Focus();System.Windows.Input.Keyboard.Focus(search);}}));}
 public bool ShowingAllFormats{get{return showAll;}}
}
}
