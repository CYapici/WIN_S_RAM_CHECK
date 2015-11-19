using System;
using System.Collections.Generic;
using System.Text;

namespace WS_Alarm_Ram.Classes
{
    class Ram_Info
    {

        public string Name { get; set; }
        public int AlarmStartSeverity { get; set; }
        public int AlarmEndSeverity { get; set; }
        public int AlarmValue { get; set; }
        public int AlarmCode { get; set; }
        public string AlarmFilePath { get; set; }
        public int AlarmStatus { get; set; }
        public int AlarmCount { get; set; }
        public string FtpFileName { get; set; }
        public string EmailSubject { get; set; }
        public string EmailBody { get; set; }
        public string ReceiverList { get; set; }
        public int SleepTime { get; set; }
        public string ServiceCode { get; set; }
        public string SmsList { get; set; }
        public string MessageStarted { get; set; }
        public string MessageStopped { get; set; }
        public string ClubName { get; set; }
        public bool isMailSendEnable { get; set; }
        public bool isSmsSendEnable { get; set; }
        public bool isFtpUploadEnable { get; set; }
        public string AlarmTxtPath { get; set; }
        public string ServerIpAddress { get; set; }
        public float AlarmPercentage { get; set; }
    }
}