﻿using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.ServiceProcess;
using System.Text;
using System.Configuration;
using System.Timers;

namespace GuardAngel
{
    class Program
    {
        static void Main(string[] args)
        {
            var allServices = ServiceController.GetServices();
            var needGuardServices = GetNeedGuardService();
            int checkRate = 0;

            try
            {
                checkRate = (Convert.ToInt32(ConfigurationManager.AppSettings["CheckRate"])) * 1000;
            }
            catch (Exception ex)
            {
                throw ex;
            }

            Timer time = new Timer(checkRate);

            time.Elapsed += (sender, e) =>
            {
                foreach (var service in allServices)
                {
                    if (IsExists(service.ServiceName, needGuardServices))
                    {
                        if (service.Status == ServiceControllerStatus.Stopped || service.Status == ServiceControllerStatus.StopPending)
                        {
                            service.WaitForStatus(ServiceControllerStatus.Stopped);
                            service.Start();
                        }
                    }
                }
            };

            time.Start();

            System.Threading.Thread.Sleep(-1);
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