using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Management;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Permissions;
using System.IO;
using System.Configuration;
using System.Xml;
using WS_Alarm_Ram.Classes;
using Utilities;

namespace WS_Alarm_Ram
{
    class AlarmMonitor
    {

        #region variables
        Ram_Info ramInstance = new Ram_Info();
        public static string AlarmFilePath = ConfigurationManager.AppSettings.Get("RamXmlPath");
        static int AlarmCount;
        static int AlarmStatus = 0;
        static string SMTP_HOST = ConfigurationManager.AppSettings.Get("SMTP_HOST");
        static string SMTP_PORT = ConfigurationManager.AppSettings.Get("SMTP_PORT");
        static string SMTP_USER = ConfigurationManager.AppSettings.Get("SMTP_USER");
        static string SMTP_PASS = ConfigurationManager.AppSettings.Get("SMTP_PASS");
        static bool isSSLEnabled = Convert.ToBoolean(ConfigurationManager.AppSettings.Get("SMTP_SSL_ENABLE"));
        enum AlarmSignal
        {
            AlarmStart = 1,
            AlarmEnd = 0
        }

        public struct MemoryStatus
        {
            public uint Length;
            public uint MemoryLoad;
            public uint TotalPhysical;
            public uint AvailablePhysical;
            public uint TotalPageFile;
            public uint AvailablePageFile;
            public uint TotalVirtual;
            public uint AvailableVirtual;
        }



        [DllImport("kernel32.dll")]
        public static extern void GlobalMemoryStatus(out MemoryStatus stat);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        private class MEMORYSTATUSEX
        {
            public uint dwLength;
            public uint dwMemoryLoad;
            public ulong ullTotalPhys;
            public ulong ullAvailPhys;
            public ulong ullTotalPageFile;
            public ulong ullAvailPageFile;
            public ulong ullTotalVirtual;
            public ulong ullAvailVirtual;
            public ulong ullAvailExtendedVirtual;
            public MEMORYSTATUSEX()
            {
                this.dwLength = (uint)Marshal.SizeOf(typeof(MEMORYSTATUSEX));
            }
        }

        [return: MarshalAs(UnmanagedType.Bool)]
        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        static extern bool GlobalMemoryStatusEx([In, Out] MEMORYSTATUSEX lpBuffer);


        #endregion

        static Ram_Info readXmlToObj()
        {


            try
            {
                XmlDocument doc = new XmlDocument();
                doc.Load(AlarmFilePath);
                XmlElement root = doc.DocumentElement;
                XmlNodeList nodes = root.SelectNodes("/Settings/RAM");


                Ram_Info _objRamInfo = new Ram_Info();




                foreach (XmlNode node in nodes)
                {

                    _objRamInfo.SleepTime = Convert.ToInt32(node["CheckPeriodInSeconds"].InnerText);
                    _objRamInfo.ServiceCode = node["ServiceCode"].InnerText;
                    _objRamInfo.SmsList = node["SmsTo"].InnerText;
                    _objRamInfo.MessageStarted = node["MessageStarted"].InnerText;
                    _objRamInfo.MessageStopped = node["MessageStopped"].InnerText;
                    _objRamInfo.ClubName = node["ProjectName"].InnerText;

                    _objRamInfo.isMailSendEnable = CommonFunctions.toBool(node["EmailEnabled"].InnerText);
                    _objRamInfo.isSmsSendEnable = CommonFunctions.toBool(node["SmsEnabled"].InnerText);
                    _objRamInfo.isFtpUploadEnable = CommonFunctions.toBool(node["FtpEnabled"].InnerText);
                    _objRamInfo.AlarmTxtPath = node["AlarmFilePath"].InnerText;

                    _objRamInfo.Name = node["Name"].InnerText;
                    _objRamInfo.AlarmStartSeverity = Convert.ToInt32(node["AlarmStartSeverity"].InnerText);
                    _objRamInfo.AlarmEndSeverity = Convert.ToInt32(node["AlarmEndSeverity"].InnerText);
                    _objRamInfo.AlarmValue = Convert.ToInt32(node["AlarmValue"].InnerText);
                    _objRamInfo.AlarmCode = Convert.ToInt32(node["AlarmCode"].InnerText);
                    _objRamInfo.AlarmFilePath = node["AlarmFilePath"].InnerText;
                    _objRamInfo.AlarmStatus = 0;
                    _objRamInfo.AlarmCount = 0;
                    _objRamInfo.FtpFileName = node["FtpFileName"].InnerText;
                    _objRamInfo.EmailSubject = node["EmailSubject"].InnerText;
                    _objRamInfo.EmailBody = node["EmailBody"].InnerText;
                    _objRamInfo.ReceiverList = node["EmailTo"].InnerText;
                    _objRamInfo.AlarmPercentage = Convert.ToSingle(node["AlarmPercentage"].InnerText);

                }

                return _objRamInfo;

            }
            catch (Exception ex)
            {
                LogUtil.WriteLog(LogLevel.ERROR, String.Format("MESSAGE: {0} *** STACK TRACE: {1}", ex.Message, ex.StackTrace));

                return null;
            }
        }

        public void Start()
        {
            Ram_Info ramObject = readXmlToObj();


            while (true)
            {
                #region OldCode
                //MemoryStatus stat = new MemoryStatus();
                //GlobalMemoryStatus(out stat);

                //long ram_availbale = (long)stat.AvailablePhysical / 1024;
                //long ram_total = (long)stat.TotalPhysical / 1024;
                //long ram_used = ram_total - ram_availbale;
                #endregion



                ulong ram_availbale;
                ulong ram_total;
                ulong ram_used;


                MEMORYSTATUSEX memStatus = new MEMORYSTATUSEX();
                if (GlobalMemoryStatusEx(memStatus))
                {

                    ram_availbale = memStatus.ullAvailPhys;
                    ram_total = memStatus.ullTotalPhys;
                    ram_used = ram_total - ram_availbale;

                    float CurrentPercentage = (ram_used * 100) / ram_total;

                    if (CurrentPercentage >= ramObject.AlarmPercentage)
                    {
                        AlarmCount += 1;
                    }
                    else
                    {
                        AlarmCount = 0;
                    }

                    AlarmChecker(ramObject);
                    Thread.Sleep(ramObject.SleepTime);
                }
                else
                {
                    LogUtil.WriteLog(LogLevel.ERROR, "Cannot retreive ram value ! " + Environment.NewLine + "**** " + "Date:" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                    Thread.Sleep(ramObject.SleepTime);
                }
            }
        }

        static void AlarmChecker(Ram_Info ws1)
        {

            try
            {

                if (AlarmCount > ws1.AlarmValue && AlarmStatus == 0)
                {

                    try
                    {
                        AlarmFormatter(ws1);
                    }
                    catch (Exception ex)
                    {

                        LogUtil.WriteLog(LogLevel.ERROR, ex.Message + " " + ex.StackTrace + Environment.NewLine + "**** " + "Date:" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                    }

                    if (ws1.isFtpUploadEnable)
                    {
                        FTPHandler ftpUploader = new FTPHandler();
                        ftpUploader.Upload(ws1.AlarmFilePath, ws1.FtpFileName);
                    }

                    if (ws1.isMailSendEnable)
                    {
                        string[] mailingList1 = ws1.ReceiverList.Split('#');
                        foreach (string mailAdress in mailingList1)
                        {
                            Mailer cs = new Mailer();
                            cs.EmailSubject = String.Format(ws1.ClubName + "/" + ws1.Name, ws1.ClubName);
                            cs.EmailBody = "Server-IP: " + ws1.ServerIpAddress + "\n" + "Project Name:  " + ws1.ClubName + "\n" + ws1.MessageStopped + "%" + ws1.AlarmPercentage + "\n" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                            cs.IsHtmlMail = false;
                            cs.EmailTo = mailAdress;
                            cs.SmtpServerPort = SMTP_PORT;
                            cs.SenderEmailAddress = SMTP_USER;
                            cs.SenderEmailPassword = SMTP_PASS;
                            cs.EnableSsl = true;
                            cs.SmtpServerName = SMTP_HOST;
                            Mailer.SendSingleEmail(cs);
                            cs = null;


                        }


                    }

                    if (ws1.isSmsSendEnable)
                    {

                        try
                        {


                            string[] arrSmsList = ws1.SmsList.Split('#');
                            foreach (string Msisdn in arrSmsList)
                            {
                                DataAccessLayer _objDataAccessLayer = new DataAccessLayer();
                                _objDataAccessLayer.SendMessage(ws1.ServiceCode, Msisdn, String.Format("{0} - {1}", ws1.ClubName, ws1.MessageStopped + "\n" + "Server-IP: " + ws1.ServerIpAddress + "\n" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")));
                            }
                        }
                        catch (Exception exSms)
                        {
                            LogUtil.WriteLog(LogLevel.ERROR, exSms.Message + " " + exSms.StackTrace + Environment.NewLine + "**** " + "Date:" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                        }
                    }
                    AlarmStatus = 1;


                }

                else if (AlarmCount == 0 && AlarmStatus == 1)
                {

                    AlarmStatus = 0;
                    AlarmCount = 0;

                    try
                    {
                        AlarmFormatter(ws1);
                    }
                    catch (Exception ex)
                    {

                        LogUtil.WriteLog(LogLevel.ERROR, ex.Message + " " + ex.StackTrace + Environment.NewLine + "**** " + "Date:" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                    }


                    if (ws1.isFtpUploadEnable)
                    {
                        FTPHandler ftpUploader = new FTPHandler();
                        ftpUploader.Upload(ws1.AlarmFilePath, ws1.FtpFileName);
                    }

                    if (ws1.isSmsSendEnable)
                    {

                        try
                        {


                            string[] arrSmsList = ws1.SmsList.Split('#');
                            foreach (string Msisdn in arrSmsList)
                            {
                                DataAccessLayer _objDataAccessLayer = new DataAccessLayer();
                                _objDataAccessLayer.SendMessage(ws1.ServiceCode, Msisdn, String.Format("{0} - {1}", ws1.ClubName, ws1.MessageStarted + "\n" + "Server-IP: " + ws1.ServerIpAddress + "\n" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")));
                            }
                        }
                        catch (Exception exSms)
                        {
                            LogUtil.WriteLog(LogLevel.ERROR, exSms.Message + " " + exSms.StackTrace + Environment.NewLine + "**** " + "Date:" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                        }
                    }

                    if (ws1.isMailSendEnable)
                    {
                        string[] mailingList = ws1.ReceiverList.Split('#');
                        foreach (string mailAdress in mailingList)
                        {
                            Mailer cs = new Mailer();
                            cs.EmailSubject = String.Format(ws1.ClubName + "/" + ws1.Name, ws1.ClubName);
                            cs.EmailBody = "Server-IP: " + ws1.ServerIpAddress + "\n" + "Project Name:  " + ws1.ClubName + "\n" + ws1.MessageStarted + "%" + ws1.AlarmPercentage + "\n" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                            cs.IsHtmlMail = false;
                            cs.EmailTo = mailAdress;
                            cs.SmtpServerPort = SMTP_PORT;
                            cs.SenderEmailAddress = SMTP_USER;
                            cs.SenderEmailPassword = SMTP_PASS;
                            cs.EnableSsl = true;
                            cs.SmtpServerName = SMTP_HOST;
                            Mailer.SendSingleEmail(cs);
                            cs = null;
                        }

                    }
                }
                else
                {
                    Console.WriteLine("Normal");
                }
            }


            catch (Exception ex)
            {

                LogUtil.WriteLog(LogLevel.ERROR, ex.Message + " " + ex.StackTrace + Environment.NewLine + "**** " + "Date:" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            }



        }

        static void AlarmFormatter(Ram_Info ws1)
        {
            DirectoryInfo dInfo = new DirectoryInfo(ws1.AlarmTxtPath);
            if (!dInfo.Parent.Exists)
            {
                dInfo.Parent.Create();

            }

            StreamWriter sw = File.CreateText(AlarmFilePath);
            sw.WriteLine("%a");
            sw.WriteLine("-ObjectOfReference=SubNetwork=ONRM_RootMo,SubNetwork=TEXT");
            sw.WriteLine("-RecordType=1");
            sw.WriteLine("-EventTime=" + DateTime.Now.ToString("yyyyMMddHHmmss"));
            if (AlarmStatus == Convert.ToInt32(AlarmSignal.AlarmEnd))
            {
                sw.WriteLine("-PerceivedSeverity=" + ws1.AlarmEndSeverity.ToString());
            }
            else
            {
                sw.WriteLine("-PerceivedSeverity=" + ws1.AlarmStartSeverity.ToString());
            }
            sw.WriteLine("-SpecificProblem=" + ws1.AlarmCode);
            sw.WriteLine("-ManagedElement=mores");
            sw.WriteLine("%A");
            sw.Close();

        }

    }
}
