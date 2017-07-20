using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.ServiceProcess;
using System.Text;
using System.Configuration;
using System.Timers;
using System.Runtime.InteropServices;
using NLog;
using System.Management;
using System.Diagnostics;

namespace GuardAngel
{
    class Program
    {
        [DllImport("user32.dll", EntryPoint = "ShowWindow", SetLastError = true)]
        static extern bool ShowWindow(IntPtr hWnd, uint nCmdShow);
        [DllImport("user32.dll", EntryPoint = "FindWindow", SetLastError = true)]
        public static extern IntPtr FindWindow(string lpClassName, string lpWindowName);
        private static readonly ILogger _logger = LogManager.GetCurrentClassLogger();
        static void Main(string[] args)
        {
            Console.Title = "GuardAngel";
            IntPtr intptr = FindWindow("ConsoleWindowClass", "GuardAngel");
            if (intptr != IntPtr.Zero)
            {
                //隐藏这个窗口
                ShowWindow(intptr, 0);
            }

            var allServices = ServiceController.GetServices();
            var needGuardServices = GetNeedGuardService();
            int checkRate = 0;

            try
            {
                checkRate = (Convert.ToInt32(ConfigurationManager.AppSettings["CheckRate"])) * 1000;
            }
            catch (Exception ex)
            {
                _logger.Error("读取检测频率错误，请检查配置文件是否配置");
                throw ex;
            }

            Timer time = new Timer(checkRate);

            time.Elapsed += (sender, e) =>
            {
                time.Stop();

                foreach (var service in allServices)
                {
                    if (IsExists(service.ServiceName, needGuardServices))
                    {
                        string processName = string.Empty;

                        if (IsAbnormalService(service, out processName))
                        {
                            if (!string.IsNullOrEmpty(processName))
                            {
                                KillProcess(processName);
                            }
                            else
                            {
                                service.Refresh();
                            }
                        }

                        if (service.Status == ServiceControllerStatus.Stopped ||
                        service.Status == ServiceControllerStatus.StopPending)
                        {
                            try
                            {
                                service.WaitForStatus(ServiceControllerStatus.Stopped);
                                service.Start();

                                service.WaitForStatus(ServiceControllerStatus.Running);

                                _logger.Info("成功启动了：{0}服务", service.ServiceName);
                            }
                            catch (Exception ex)
                            {
                                _logger.Error("启动服务:{0}错误", service.ServiceName, ex);
                            }
                        }
                    }
                }

                time.Start();
            };

            time.Start();

            System.Threading.Thread.Sleep(-1);
        }

        static bool IsAbnormalService(ServiceController sc, out string processName)
        {
            ManagementObject service = new ManagementObject(@"Win32_service.Name='" + sc.ServiceName + "'");
            object o = service.GetPropertyValue("ProcessId");
            int processId = (int)((uint)o);

            Process process = Process.GetProcessById(processId);

            if (processId != 0)
            {
                processName = process.ProcessName;
            }
            else
            {
                processName = string.Empty;
            }

            if (sc.Status != ServiceControllerStatus.Stopped)
            {
                if (processId == 0)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                if (processId == 0)
                {
                    return false;
                }
                else
                {
                    return true;
                }
            }
        }

        static void KillProcess(string processName)
        {
            var arrProcess = Process.GetProcessesByName(processName);

            for (int i = 0; i < arrProcess.Length; i++)
            {
                var process = arrProcess[i];

                try
                {
                    process.Kill();
                }
                catch (Exception ex)
                {
                    _logger.Error("杀死进程:{0} 失败", process.ProcessName, ex);
                    throw;
                }
            }
        }
        /// <summary>
        /// 进程名称是否存在数组中
        /// </summary>
        /// <param name="sName"></param>
        /// <param name="arrSInfo"></param>
        /// <returns></returns>
        static bool IsExists(string sName, ServiceInfo[] arrSInfo)
        {
            foreach (var info in arrSInfo)
            {
                if (string.Compare(info.Name, sName, true) == 0)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 获取所有需要守护的进程
        /// </summary>
        /// <returns></returns>
        static ServiceInfo[] GetNeedGuardService()
        {
            string jsonPath = AppDomain.CurrentDomain.BaseDirectory + "Service.json";
            string result = File.ReadAllText(jsonPath, Encoding.UTF8);
            ServiceInfo[] needGuardServices = JsonConvert.DeserializeObject<ServiceInfo[]>(result);

            return needGuardServices;
        }
    }

    public class ServiceInfo
    {
        public string Name { get; set; }
        public string Desc { get; set; }
    }
}
