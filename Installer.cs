using System;
using System.IO;
using System.Linq;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using Microsoft.Win32;
using System.Reflection;
namespace FlowShiftInstaller {
static class Program {
 static string InstallDir=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"Programs","FlowShift");
 static string MenuDir=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Programs),"FlowShift");
 static string DesktopLink=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),"流转 FlowShift.lnk");
 [STAThread]static void Main(string[] args){Application.EnableVisualStyles();if(args.Contains("--extract-payload")){using(var input=Assembly.GetExecutingAssembly().GetManifestResourceStream("FlowShift.Payload"))using(var output=File.Create(args[1]))input.CopyTo(output);return;}if(args.Contains("--render")){using(var form=new Setup()){form.Show();Application.DoEvents();using(var bmp=new Bitmap(form.Width,form.Height)){form.DrawToBitmap(bmp,form.ClientRectangle);bmp.Save(args[1],System.Drawing.Imaging.ImageFormat.Png);}}return;}if(args.Contains("--uninstall")||Path.GetFileName(Application.ExecutablePath).Equals("Uninstall.exe",StringComparison.OrdinalIgnoreCase)){Uninstall();return;}Application.Run(new Setup());}
 static void Shortcut(string link,string target,string arguments){Type t=Type.GetTypeFromProgID("WScript.Shell");dynamic shell=Activator.CreateInstance(t);dynamic shortcut=shell.CreateShortcut(link);shortcut.TargetPath=target;shortcut.Arguments=arguments;shortcut.WorkingDirectory=InstallDir;shortcut.Description="流转 FlowShift 本地格式转换器";shortcut.Save();}
 static bool Running(){return new[]{"FlowShift","FlowShift2","FlowShift21","FlowShift21-Update","FlowShift22","FlowShift22-Update","FlowShift23","FlowShift23-Update","FlowShift24","FlowShift24-Update","FlowShift241","FlowShift241-Update"}.Any(name=>Process.GetProcessesByName(name).Length>0);}
 static void RemoveOwnedRuntime(){string root=Path.GetFullPath(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"FlowShift"));if(!Directory.Exists(root))return;foreach(string candidate in Directory.GetDirectories(root,"runtime-*")){string full=Path.GetFullPath(candidate);if(!full.StartsWith(root+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase)||!File.Exists(Path.Combine(full,"ready.txt"))||!File.Exists(Path.Combine(full,"FlowShift.CoreHost.exe")))continue;Directory.Delete(full,true);}}
 static void Uninstall(){if(MessageBox.Show("卸载流转 FlowShift？转换文件及用户设置将保留。","流转卸载",MessageBoxButtons.YesNo,MessageBoxIcon.Question)!=DialogResult.Yes)return;if(Running()){MessageBox.Show("请先从托盘菜单退出流转，再运行卸载。");return;}try{string exe=Path.Combine(InstallDir,"FlowShift.exe");if(File.Exists(exe))File.Delete(exe);if(File.Exists(DesktopLink))File.Delete(DesktopLink);foreach(string n in new[]{"流转 FlowShift.lnk","卸载流转.lnk"}){string p=Path.Combine(MenuDir,n);if(File.Exists(p))File.Delete(p);}if(Directory.Exists(MenuDir)&&Directory.GetFileSystemEntries(MenuDir).Length==0)Directory.Delete(MenuDir);FlowShift.DesktopIntegration.Unregister(exe);RemoveOwnedRuntime();Registry.CurrentUser.DeleteSubKeyTree(@"Software\Microsoft\Windows\CurrentVersion\Uninstall\FlowShift",false);MessageBox.Show("卸载完成。当前正在执行的卸载程序保留在安装目录，可稍后手动删除。");}catch(Exception ex){MessageBox.Show(ex.Message,"卸载未完成");}}
 class Setup:Form {
  CheckBox desktop,launch;Button install;Label status;
  public Setup(){Text="流转 FlowShift · 安装向导";Size=new Size(600,470);StartPosition=FormStartPosition.CenterScreen;FormBorderStyle=FormBorderStyle.FixedDialog;MaximizeBox=false;BackColor=Color.FromArgb(246,248,252);ForeColor=Color.FromArgb(40,56,80);Font=new Font("Microsoft YaHei UI",10);
   Controls.Add(new Label(){Text="◈  流转 FlowShift",Font=new Font(Font.FontFamily,25,FontStyle.Bold),ForeColor=Color.FromArgb(64,124,232),AutoSize=true,Location=new Point(32,32)});
   Controls.Add(new Label(){Text="把繁琐转换，变成顺手的事。",AutoSize=true,Location=new Point(36,96)});
   Controls.Add(new Label(){Text="本地处理  /  保留原件  /  无捆绑\n\n安装到当前用户，无需管理员权限。\n"+InstallDir,Location=new Point(36,140),Size=new Size(520,100)});
   desktop=new CheckBox(){Text="创建桌面快捷方式",Checked=true,AutoSize=true,Location=new Point(36,255)};launch=new CheckBox(){Text="安装完成后打开流转",Checked=true,AutoSize=true,Location=new Point(36,288)};Controls.Add(desktop);Controls.Add(launch);
   status=new Label(){Text="Windows 10 / 11 · x64 · .NET Framework 4.8",Location=new Point(36,350),Size=new Size(320,50)};Controls.Add(status);
   install=new Button(){Text="安装  →",Location=new Point(420,346),Size=new Size(125,45),BackColor=Color.FromArgb(64,124,232),ForeColor=Color.White,FlatStyle=FlatStyle.Flat};install.Click+=(s,e)=>DoInstall();Controls.Add(install);
  }
  void DoInstall(){if(Running()){MessageBox.Show("请先从托盘退出正在运行的流转，再安装或更新。");return;}install.Enabled=false;try{Directory.CreateDirectory(InstallDir);string exe=Path.Combine(InstallDir,"FlowShift.exe"),temp=exe+".new";using(var input=Assembly.GetExecutingAssembly().GetManifestResourceStream("FlowShift.Payload"))using(var output=File.Create(temp))input.CopyTo(output);if(File.Exists(exe))File.Delete(exe);File.Move(temp,exe);string un=Path.Combine(InstallDir,"Uninstall.exe");using(var unInput=Assembly.GetExecutingAssembly().GetManifestResourceStream("FlowShift.Uninstaller"))using(var unOutput=File.Create(un))unInput.CopyTo(unOutput);FlowShift.DesktopIntegration.Register(exe);Directory.CreateDirectory(MenuDir);Shortcut(Path.Combine(MenuDir,"流转 FlowShift.lnk"),exe,"");Shortcut(Path.Combine(MenuDir,"卸载流转.lnk"),un,"--uninstall");if(desktop.Checked)Shortcut(DesktopLink,exe,"");using(var k=Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Uninstall\FlowShift")){k.SetValue("DisplayName","流转 FlowShift");k.SetValue("DisplayVersion","2.4.1");k.SetValue("Publisher","FlowShift Community");k.SetValue("InstallLocation",InstallDir);k.SetValue("DisplayIcon",exe);k.SetValue("UninstallString","\""+un+"\" --uninstall");k.SetValue("NoModify",1);k.SetValue("NoRepair",1);k.SetValue("EstimatedSize",(int)((new FileInfo(exe).Length+new FileInfo(un).Length)/1024));}status.Text="安装完成。";if(launch.Checked)Process.Start(exe);MessageBox.Show("流转已安装。关闭窗口后会驻留托盘。");Close();}catch(Exception ex){MessageBox.Show(ex.Message,"安装未完成");install.Enabled=true;}}
 }
}
}
