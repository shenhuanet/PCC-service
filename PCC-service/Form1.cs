using Microsoft.Win32;
using PCC_service.Properties;
using System;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace PCC_service
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            RegistryKey rk = Registry.LocalMachine;
            try
            {
                RegistryKey rk2 = rk.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run");
                if (rk2.GetValue("PCC_service") == null)
                {
                    rk2.SetValue("PCC_service", Application.ExecutablePath);
                    rk2.Close();
                    rk.Close();
                }
            }
            catch (Exception)
            {
                notifyIcon1.BalloonTipText = "请以管理员模式运行，以授权开机自启动";
                notifyIcon1.ShowBalloonTip(20000);
                Console.WriteLine("请以管理员模式运行.");
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            this.BeginInvoke(new Action(() => { this.Hide(); }));
            Console.WriteLine("start");
            notifyIcon1.Text = "PCC-service";
            Socket serverSocket = new Socket(SocketType.Stream, ProtocolType.Tcp);
            IPAddress ip = IPAddress.Any;
            IPEndPoint point = new IPEndPoint(ip, 9);
            try
            {
                serverSocket.Bind(point);
                Console.WriteLine("Listen Success.");
                notifyIcon1.Icon = Resources.icon_listening;
                notifyIcon1.Text = "PCC-service 服务已启动";
                notifyIcon1.BalloonTipText = "PCC-service 服务已启动";
                notifyIcon1.ShowBalloonTip(20000);

                serverSocket.Listen(1);
                Thread thread = new Thread(Listen)
                {
                    IsBackground = true
                };
                thread.Start(serverSocket);
                Console.Read();
            }
            catch (Exception)
            {
                Console.WriteLine("Listen Error.");
                notifyIcon1.Icon = Resources.icon_error;
                notifyIcon1.Text = "PCC-service 服务启动失败";
                notifyIcon1.BalloonTipText = "PCC-service 服务启动失败";
                notifyIcon1.ShowBalloonTip(20000);
            }
        }

        private void Listen(object o)
        {
            var serverSocket = o as Socket;
            while (true)
            {
                var send = serverSocket.Accept();
                var sendIpoint = send.RemoteEndPoint.ToString();
                Console.WriteLine($"{sendIpoint} Connection");
                notifyIcon1.Icon = Resources.icon_connected;
                notifyIcon1.Text = $"PCC-service {sendIpoint} 客服端已连接";
                notifyIcon1.BalloonTipText = $"PCC-service {sendIpoint} 客服端已连接";
                notifyIcon1.ShowBalloonTip(20000);
                Thread thread = new Thread(Recive)
                {
                    IsBackground = true
                };
                thread.Start(send);
            }
        }

        private void Recive(object o)
        {
            var send = o as Socket;
            while (true)
            {
                byte[] buffer = new byte[1024 * 1024 * 2];
                var effective = send.Receive(buffer);
                if (effective == 0)
                {
                    Console.WriteLine("客户端断开连接");
                    notifyIcon1.Icon = Resources.icon_listening;
                    notifyIcon1.Text = "PCC-service 服务已启动";
                    notifyIcon1.BalloonTipText = "PCC-service 服务已启动";
                    notifyIcon1.ShowBalloonTip(20000);
                    break;
                }
                var str = Encoding.UTF8.GetString(buffer, 0, effective);
                Console.WriteLine(str);
                if (str == "shutdown")
                {
                    shutdown();
                }
                var buffers = Encoding.UTF8.GetBytes("200");
                send.Send(buffers);
            }
        }

        private void shutdown()
        {
            Console.WriteLine("---------- 关机指令");
            Process.Start("shutdown.exe", "-s");

            // Process.Start("shutdown.exe", "-r");//重启
            // Process.Start("shutdown.exe", "-l");//注销
        }
    }
}
