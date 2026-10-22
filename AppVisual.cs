using System;
using System.Drawing;
using System.Reflection;
using System.Windows;
using System.Windows.Media.Imaging;
namespace FlowShift {
public static class AppVisual {
 public static Icon LoadIcon(){try{using(var stream=Assembly.GetExecutingAssembly().GetManifestResourceStream("FlowShift.AppIcon"))if(stream!=null)using(var icon=new Icon(stream,32,32))return (Icon)icon.Clone();}catch{}try{return Icon.ExtractAssociatedIcon(Assembly.GetExecutingAssembly().Location);}catch{return (Icon)SystemIcons.Information.Clone();}}
 public static BitmapSource Image(Icon icon){var image=System.Windows.Interop.Imaging.CreateBitmapSourceFromHIcon(icon.Handle,Int32Rect.Empty,BitmapSizeOptions.FromEmptyOptions());image.Freeze();return image;}
}
}
