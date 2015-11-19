using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using System.Configuration;
using Utilities;
namespace WS_Alarm_Ram
{
    public partial class Service1 : ServiceBase
    {
        #region Begining
        public Service1()
        {
            InitializeComponent();
        }

        //static void Main()
        //{
        //    ServiceBase[] ServicesToRun;
        //    ServicesToRun = new ServiceBase[] 
        //    { 
        //        new Service1() 
        //    };
        //    ServiceBase.Run(ServicesToRun);
        //}


        static void Main()
        {

#if (!DEBUG)

                System.ServiceProcess.ServiceBase[] ServicesToRun;

                // More than one user Service may run within the same process. To add
                // another service to this process, change the following line to
                // create a second service object. For example,
                //
                //   ServicesToRun = new System.ServiceProcess.ServiceBase[] {new Service1(), new MySecondUserService()};
                //


                ServicesToRun = new System.ServiceProcess.ServiceBase[] { new Service1() };

                System.ServiceProcess.ServiceBase.Run(ServicesToRun);


#else

            Service1 service = new Service1();
            service.OnStart(new string[] { });
            System.Threading.Thread.Sleep(System.Threading.Timeout.Infinite);

#endif
        }
        #endregion


        protected override void OnStart(string[] args)
        {
            try
            {

                #region serviceVariables

                string ServiceStarterMessage = ConfigurationManager.AppSettings.Get("ServiceStarterMessage");
                string ClubName = ConfigurationManager.AppSettings.Get("ClubName");
                string AppName = ConfigurationManager.AppSettings.Get("ApplicationName");
                int StartSleepTime = Convert.ToInt32(ConfigurationManager.AppSettings.Get("StartSleepTime"));
                string ServiceStarterMailinglist = ConfigurationManager.AppSettings.Get("ServiceStarterMailinglist");
                string ServerIP = ConfigurationManager.AppSettings.Get("ServerIP");

                string SMTP_HOST = ConfigurationManager.AppSettings.Get("SMTP_HOST");
                string SMTP_PORT = ConfigurationManager.AppSettings.Get("SMTP_PORT");
                string SMTP_USER = ConfigurationManager.AppSettings.Get("SMTP_USER");
                string SMTP_PASS = ConfigurationManager.AppSettings.Get("SMTP_PASS");
                bool isSSLEnabled = Convert.ToBoolean(ConfigurationManager.AppSettings.Get("SMTP_SSL_ENABLE"));
             
                #endregion
                try
                {
                    #region SendMailFirstToAuthorized
                    string[] mailingList = ServiceStarterMailinglist.Split('#');
                    foreach (string Receievers in mailingList)
                    {

                        Mailer cs = new Mailer();
                        cs.EmailSubject = ClubName + "/" + AppName + " windows service has started:  " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + ClubName;
                        cs.EmailBody = "Server IP:" + ServerIP + Environment.NewLine + "Project Name:" + ClubName + Environment.NewLine + ServiceStarterMessage + Environment.NewLine + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                        cs.IsHtmlMail = false;
                        cs.EmailTo = Receievers;
                        cs.SmtpServerPort = SMTP_PORT;
                        cs.SenderEmailAddress = SMTP_USER;
                        cs.SenderEmailPassword = SMTP_PASS;
                        cs.EnableSsl = true;
                        cs.SmtpServerName = SMTP_HOST;
                        Mailer.SendSingleEmail(cs);
                        cs = null;
                    }
                    #endregion

                    LogUtil.WriteLog(LogLevel.INFO, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " " + AppName + " has started, sleeping for " + StartSleepTime.ToString() + " miliseconds before starting threads");
                    Thread.Sleep(StartSleepTime);
                    LogUtil.WriteLog(LogLevel.INFO, "Sleep ended, starting to process threads");

                    Thread alarmChecker = new Thread(new ThreadStart((new AlarmMonitor()).Start));
                    alarmChecker.Name = "Alarm Monitor";
                    alarmChecker.Start();

                }
                catch (Exception ex)
                {
                    LogUtil.WriteLog(LogLevel.ERROR, String.Format("MESSAGE: {0} *** STACK TRACE: {1}", ex.Message, ex.StackTrace));
                }



            }
            catch (Exception ex)
            {

                LogUtil.WriteLog(LogLevel.ERROR, String.Format("MESSAGE: {0} *** STACK TRACE: {1}", ex.Message, ex.StackTrace));
            }

        }

        protected override void OnStop()
        {
        }
    }
}
