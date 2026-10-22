using System;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
namespace FlowShift {
public class AboutWindow:Window {
 static Brush Color(string c){return (Brush)new BrushConverter().ConvertFromString(c);}
 public AboutWindow(){Title="关于流转 FlowShift";Width=470;Height=570;ResizeMode=ResizeMode.NoResize;WindowStartupLocation=WindowStartupLocation.CenterScreen;Background=Color("#F7F9FD");Foreground=Color("#324562");FontFamily=new FontFamily("Microsoft YaHei UI");ShowInTaskbar=false;Topmost=true;
  using(var icon=AppVisual.LoadIcon())Icon=AppVisual.Image(icon);
  var p=new StackPanel(){Margin=new Thickness(28)};Content=p;
  p.Children.Add(MainWindow.T("◈  流转 FlowShift",26,"#407CE8"));
  p.Children.Add(MainWindow.T("版本 "+Assembly.GetExecutingAssembly().GetName().Version.ToString(3),11,"#8291A8",new Thickness(0,8,0,18)));
  p.Children.Add(MainWindow.T("让文件转换，变成顺手的事。",18,"#324562",new Thickness(0,0,0,12)));
  p.Children.Add(MainWindow.T("本地格式转换器，支持贴边悬浮、文件右键菜单、格式罗盘和批量处理。文件不上传，原件保留。",12,"#7387A3",new Thickness(0,0,0,22)));
  p.Children.Add(MainWindow.T("联系作者",16,"#324562",new Thickness(0,0,0,12)));
  Contact(p,"微信公众号","景同志");Contact(p,"小红书","景同学");
  p.Children.Add(MainWindow.T("开源与下载",16,"#324562",new Thickness(0,18,0,12)));
  GitHubRow(p,"GitHub 仓库","https://github.com/jingpanlong/FlowShift");
  GitHubRow(p,"下载 Release","https://github.com/jingpanlong/FlowShift/releases");
  p.Children.Add(MainWindow.T("转换内核：Filee（MIT）及其开源组件。\n各组件保留原许可证，详见随包开源声明。",10,"#8291A8",new Thickness(0,16,0,15)));
  var close=new Button(){Content="关闭",Padding=new Thickness(18,9,18,9),HorizontalAlignment=HorizontalAlignment.Right};close.Click+=(s,e)=>Close();p.Children.Add(close);
 }
  void Contact(StackPanel p,string platform,string name){var grid=new Grid(){Margin=new Thickness(0,0,0,10),Background=Color("#EDF3FC")};grid.ColumnDefinitions.Add(new ColumnDefinition());grid.ColumnDefinitions.Add(new ColumnDefinition(){Width=GridLength.Auto});var text=new StackPanel(){Margin=new Thickness(13,10,13,10)};text.Children.Add(MainWindow.T(platform,10,"#8291A8"));text.Children.Add(MainWindow.T(name,17,"#324562",new Thickness(0,4,0,0)));grid.Children.Add(text);var copy=new Button(){Content="复制",Padding=new Thickness(12,6,12,6),Margin=new Thickness(8,0,12,0),VerticalAlignment=VerticalAlignment.Center};copy.Click+=(s,e)=>{try{Clipboard.SetText(name);copy.Content="已复制";}catch(Exception ex){MessageBox.Show(ex.Message);}};Grid.SetColumn(copy,1);grid.Children.Add(copy);p.Children.Add(grid);}
 void GitHubRow(StackPanel p,string label,string url){var grid=new Grid(){Margin=new Thickness(0,0,0,10),Background=Color("#EDF3FC")};grid.ColumnDefinitions.Add(new ColumnDefinition());grid.ColumnDefinitions.Add(new ColumnDefinition(){Width=GridLength.Auto});var text=new StackPanel(){Margin=new Thickness(13,10,13,10)};text.Children.Add(MainWindow.T(label,10,"#8291A8"));text.Children.Add(MainWindow.T(url,13,"#324562",new Thickness(0,4,0,0)));grid.Children.Add(text);var open=new Button(){Content="打开",Padding=new Thickness(12,6,12,6),Margin=new Thickness(8,0,12,0),VerticalAlignment=VerticalAlignment.Center};open.Click+=(s,e)=>{try{System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(url){UseShellExecute=true});}catch(Exception ex){MessageBox.Show(ex.Message);}};Grid.SetColumn(open,1);grid.Children.Add(open);p.Children.Add(grid);}
}
}
