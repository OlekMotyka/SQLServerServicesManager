using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.ServiceProcess;
using System.Management;
using MSSQLTunOnOff.Properties;

namespace SQLServerServicesManager
{
    public partial class Form1 : Form
    {
        ServiceController[] services = ServiceController.GetServices().Where(x => x.ServiceName.Contains("SQL")).ToArray();

        public Form1()
        {
            InitializeComponent();
            Text = Application.ProductName;
            this.Icon = Resources.logo;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            for(int i = 0; i < services.Count(); i++)
            {
                var svc = services[i];
                Button b = new Button();
                b.Name = "b" + svc.ServiceName;
                b.AutoSize = false;
                b.Text = svc.DisplayName;
                b.TextAlign = ContentAlignment.MiddleLeft;
                b.Size = new Size(250, 25);
                b.Location = new Point(5, i * 25 + 20);
                b.Tag = svc;
                b.Click += Button_Click;
                Controls.Add(b);
                b.Show();

                Label l = new Label();
                l.Text = "";
                l.Name = "l" + svc.ServiceName;
                l.Tag = svc;
                l.TextAlign = ContentAlignment.MiddleLeft;
                l.Location = new Point(260, b.Location.Y);
                l.AutoSize = false;
                l.Size = new Size(50, 25);
                Controls.Add(l);
                l.Show();

                updateSvcInfo(svc);
            }
            Size = MinimumSize = MaximumSize = new Size(330, services.Count() * 25 + 70);
        }

        private void Button_Click(object sender, EventArgs e)
        {
            var btn = sender as Button;
            var svc = btn.Tag as ServiceController;
            var lbl = Controls["l" + svc.ServiceName] as Label;
            try
            {
                if (svc.Status == ServiceControllerStatus.Stopped || svc.Status == ServiceControllerStatus.Paused)
                {
                    svc.Start();
                    svc.WaitForStatus(ServiceControllerStatus.StartPending);
                    updateSvcInfo(svc);
                    svc.WaitForStatus(ServiceControllerStatus.Running);
                    updateSvcInfo(svc);
                }
                else if (svc.Status == ServiceControllerStatus.Running)
                {
                    svc.Stop();
                    svc.WaitForStatus(ServiceControllerStatus.Stopped);
                    updateSvcInfo(svc);
                }
            }
            catch { }
        }

        private void updateSvcInfo(ServiceController svc)
        {
            var lbl = (Controls["l" + svc.ServiceName] as Label);
            var btn = (Controls["b" + svc.ServiceName] as Button);
            switch (svc.Status)
            {
                case ServiceControllerStatus.Running:
                    ManagementObject service = new ManagementObject(@"Win32_service.Name='" + svc.ServiceName + "'");
                    var id = (uint)service.GetPropertyValue("ProcessId");
                    lbl.Text = id.ToString();
                    btn.BackColor = Color.LightGreen;
                    break;
                case ServiceControllerStatus.Stopped:
                    lbl.Text = "";
                    btn.BackColor = Color.IndianRed;
                    break;
                case ServiceControllerStatus.StartPending:
                    lbl.Text = "";
                    btn.BackColor = Color.LightYellow;
                    break;
                case ServiceControllerStatus.StopPending:
                    lbl.Text = "";
                    btn.BackColor = Color.LightYellow;
                    break;
                default:
                    lbl.Text = "";
                    btn.BackColor = Color.White;
                    break;
            }
        }

        private string RunCmd(string command)
        {
            Process p = new Process();
            p.StartInfo.FileName = command.Split(' ')[0];
            p.StartInfo.Arguments = command.Substring(command.IndexOf(' ') + 1);
            p.StartInfo.CreateNoWindow = true;
            p.StartInfo.UseShellExecute = false;
            p.StartInfo.RedirectStandardOutput = true;
            p.Start();
            return p.StandardOutput.ReadToEnd();
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            foreach(var s in services)
            {
                if (s.Status == ServiceControllerStatus.Running)
                {
                    s.Stop();
                    s.WaitForStatus(ServiceControllerStatus.StopPending);
                    updateSvcInfo(s);
                    s.WaitForStatus(ServiceControllerStatus.Stopped);
                    updateSvcInfo(s);
                }
            }
        }

        private void Form1_SizeChanged(object sender, EventArgs e)
        {
            ShowInTaskbar = WindowState != FormWindowState.Minimized;
        }

        private void try_DoubleClick(object sender, EventArgs e)
        {
            if (WindowState == FormWindowState.Minimized)
            {
                WindowState = FormWindowState.Normal;
                BringToFront();
            }
            else
            {
                WindowState = FormWindowState.Minimized;
            }
        }
    }
}
