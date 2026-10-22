using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Interop;
using System.Windows.Input;
using Forms=System.Windows.Forms;
using Drawing=System.Drawing;
namespace FlowShift {
public static class EdgeLayout {
 public static string Detect(Rect bounds,Rect work,double distance){if(bounds.Left<=work.Left+distance)return "Left";if(bounds.Right>=work.Right-distance)return "Right";if(bounds.Top<=work.Top+distance)return "Top";if(bounds.Bottom>=work.Bottom-distance)return "Bottom";return "";}
 public static Rect Peek(string side,Rect work,double along){along=Math.Max(0,Math.Min(1,along));bool vertical=side=="Left"||side=="Right";double w=vertical?18:76,h=vertical?76:18;double x=vertical?(side=="Left"?work.Left:work.Right-w):Clamp(work.Left+work.Width*along-w/2,work.Left,work.Right-w);double y=vertical?Clamp(work.Top+work.Height*along-h/2,work.Top,work.Bottom-h):(side=="Top"?work.Top:work.Bottom-h);return new Rect(x,y,w,h);}
 public static Rect Panel(string side,Rect work,double along,double width,double height){width=Math.Min(width,work.Width-16);height=Math.Min(height,work.Height-16);double x=Clamp(work.Left+work.Width*along-width/2,work.Left+8,work.Right-width-8),y=Clamp(work.Top+work.Height*along-height/2,work.Top+8,work.Bottom-height-8);if(side=="Left")x=work.Left+8;if(side=="Right")x=work.Right-width-8;if(side=="Top")y=work.Top+8;if(side=="Bottom")y=work.Bottom-height-8;return new Rect(x,y,width,height);}
 static double Clamp(double n,double min,double max){return Math.Max(min,Math.Min(max,n));}
}
public partial class FloatingDock {
 string edgeSide="";bool edgeHidden;double edgeAlong=.5;
 public bool IsEdgeHidden{get{return edgeHidden;}}
 Rect WorkArea(){var source=PresentationSource.FromVisual(this);double scale=source==null||source.CompositionTarget==null?1:source.CompositionTarget.TransformToDevice.M11;var rect=new Drawing.Rectangle((int)(Left*scale),(int)(Top*scale),Math.Max(1,(int)(Width*scale)),Math.Max(1,(int)(Height*scale)));var screen=Forms.Screen.FromRectangle(rect).WorkingArea;return new Rect(screen.Left/scale,screen.Top/scale,screen.Width/scale,screen.Height/scale);}
 void ApplyBounds(Rect rect){Left=rect.Left;Top=rect.Top;Width=rect.Width;Height=rect.Height;}
 void SavePlacement(){if(preview)return;owner.Pref.DockHasPosition=true;owner.Pref.DockX=Left;owner.Pref.DockY=Top;owner.Pref.DockEdge=edgeSide;owner.Pref.DockAlong=edgeAlong;owner.Pref.Save();}
 void NormalShell(){shell.Margin=new Thickness(12);shell.CornerRadius=new CornerRadius(23);shell.BorderThickness=new Thickness(1);shell.Background=Brush("#FFFFFF");shell.BorderBrush=Brush("#E2E8F1");}
 void RestoreEdge(bool expand){if(!edgeHidden)return;var work=WorkArea();edgeHidden=false;NormalShell();ApplyBounds(EdgeLayout.Panel(edgeSide,work,edgeAlong,expand?500:226,expand?640:106));expanded=expand;if(!expand)Collapsed();}
 public void Reveal(){RestoreEdge(false);Show();Activate();}
 public void HideAtEdge(string side){if(running)return;if(side!="Left"&&side!="Right"&&side!="Top"&&side!="Bottom")return;var work=WorkArea();if(!edgeHidden){edgeAlong=(side=="Left"||side=="Right")?(Top+Height/2-work.Top)/work.Height:(Left+Width/2-work.Left)/work.Width;}edgeAlong=Math.Max(0,Math.Min(1,edgeAlong));edgeSide=side;edgeHidden=true;expanded=false;ApplyBounds(EdgeLayout.Peek(side,work,edgeAlong));shell.Margin=new Thickness(0);shell.Padding=new Thickness(0);shell.CornerRadius=new CornerRadius(7);shell.Background=Brush("#EAF1FF");shell.BorderBrush=Brush("#AAC5F4");var button=new Border(){Background=Brushes.Transparent,Cursor=Cursors.Hand,ToolTip="流转 · 点击弹出，也可以直接拖入文件"};button.Child=new TextBlock(){Text=side=="Left"?"›":side=="Right"?"‹":side=="Top"?"⌄":"⌃",Foreground=Brush("#407CE8"),FontSize=20,HorizontalAlignment=HorizontalAlignment.Center,VerticalAlignment=VerticalAlignment.Center};button.MouseLeftButtonUp+=(s,e)=>{e.Handled=true;Expand();};shell.Child=button;SavePlacement();}
 void AfterMove(double oldLeft,double oldTop){if(Math.Abs(Left-oldLeft)+Math.Abs(Top-oldTop)<4)return;if(owner.Pref.EdgeAutoHide){string edge=EdgeLayout.Detect(new Rect(Left,Top,Width,Height),WorkArea(),24);if(edge!=""){HideAtEdge(edge);return;}}edgeSide="";edgeHidden=false;owner.Pref.DockEdge="";SavePlacement();}
 public void ApplyEdgePreference(){if(!owner.Pref.EdgeAutoHide){RestoreEdge(false);edgeSide="";edgeHidden=false;owner.Pref.DockEdge="";SavePlacement();}}
 void RestorePlacement(){if(preview||!owner.Pref.EdgeAutoHide||String.IsNullOrEmpty(owner.Pref.DockEdge))return;edgeAlong=owner.Pref.DockAlong;edgeSide=owner.Pref.DockEdge;edgeHidden=true;HideAtEdge(edgeSide);}
 public void ShowAbout(){new AboutWindow().ShowDialog();}
 public bool CanConvert(string target){return owner.Engine.Common(files).Exists(r=>r.Format==target);}
}
}
