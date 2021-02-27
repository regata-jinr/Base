﻿/***************************************************************************
 *                                                                         *
 *                                                                         *
 * Copyright(c) 2020-2021, REGATA Experiment at FLNP|JINR                  *
 * Author: [Boris Rumyantsev](mailto:bdrum@jinr.ru)                        *
 *                                                                         *
 * The REGATA Experiment team license this file to you under the           *
 * GNU GENERAL PUBLIC LICENSE                                              *
 *                                                                         *
 ***************************************************************************/

using System;
using System.Diagnostics;
using System.Linq;
using System.Collections.Generic;
using System.Net.Mail;
using System.Net;
using Regata.Core.Settings;
using Regata.Core.Messages;
using AdysTech.CredentialManager;

namespace Regata.Core
{
    /// <summary>
    /// Implementation of the Report service. The service allows to user set up reporting system:
    /// - logs
    /// - different levels messaging:
    ///     - info  
    ///     - warn  
    ///     - error
    ///     - success
    /// - email notification
    /// - different gui notification
    /// For each class we have to use instance of logger with predefined settings.
    /// Initialization of the system based on secrets that keeps into Windows Credential Manager (WCM). You can load them via specifying Target as arguments:
    /// </summary>
    public static class Report
    {
        public static string MailHostTarget;
        public static string PathToMessages;
        private static string _logDir;

        public static string LogDir
        {
            get { return _logDir; }
            set
            {
                _logDir = value;
                NLog.GlobalDiagnosticsContext.Set("LogDir", _logDir);
            }
        }

        private const string _logConnectionStringTarget = "RegataCoreLogCS";
        private  static NLog.Logger _nLogger;

        static Report()
        {
            NLog.GlobalDiagnosticsContext.Set("LogConnectionString", CredentialManager.GetCredentials(_logConnectionStringTarget).Password);
            _nLogger = NLog.LogManager.GetCurrentClassLogger();

        }

        private static readonly Dictionary<Status, NLog.LogLevel> ExceptionLevel_LogLevel = new Dictionary<Status, NLog.LogLevel> { { Status.Error, NLog.LogLevel.Error }, { Status.Warning, NLog.LogLevel.Warn }, { Status.Info, NLog.LogLevel.Info } };

        public static MailAddressCollection Emails = new MailAddressCollection();

        // FIXME: in case of one of the available detector has already opened in not read-only mode (e.g. by hand) application will not run. The problem related with this  event!
        /// <summary>
        /// This event can be use as entry point for GUI messaging
        /// </summary>
        public  static event Action<Message> NotificationEvent;

        public static void Notify(Message msg, bool callEvent=false, bool WriteToLog=true, bool NotifyByEmail = false) 
        {
            try
            {
                StackTrace st = new StackTrace();
                StackFrame sf = st.GetFrame(1);
                var method = sf.GetMethod();
                var status = msg.Status;

                switch (msg)
                {
                    case DetectorMessage dmsg:
                        break;
                }

                msg.Sender = method.DeclaringType.Name;
                msg.Caption = $"{method.Module}-[{status}]-[{DateTime.Now}]";
                _nLogger.SetProperty("Sender", msg.Sender);
                _nLogger.SetProperty("Assistant", GlobalSettings.User);

                if (WriteToLog)
                {
                    _nLogger?.Log(ExceptionLevel_LogLevel[status], string.Concat(msg.Head, "---", msg.DetailedText));
                }
                 
                if (status == Status.Error || NotifyByEmail)
                     SendMessageByEmail(msg);
                
                if ((int)GlobalSettings.Verbosity <= (int)status || callEvent)
                    NotificationEvent?.Invoke(msg);

            }
            catch (Exception e)
            {
                if (NotificationEvent == null) return;
                e.Data.Add("Assembly", "Regata.Core.Inform.WriteMessage");
                var tstr = e.ToString();
                if (tstr.Length <= 300)
                    _nLogger.Log(NLog.LogLevel.Error, e.ToString());
                else
                    _nLogger.Log(NLog.LogLevel.Error, e.ToString().Substring(0, 300));
            }
        }


        public static void FillMessageByCode()
        {
            
        }


        // FIXME in case of network error will this freeze the app despite of SendAsync?
        private static void SendMessageByEmail(Message msg)
        {
            if (Emails == null || !Emails.Any()) return;
            var mail = CredentialManager.GetCredentials(MailHostTarget).UserName;
            var mailPass = CredentialManager.GetCredentials(MailHostTarget).Password;
            var fromMail = new MailAddress(mail, "REGATA Report Service");

            using (var smtp = new SmtpClient
            {
                Host = "smtp.jinr.ru",
                Port = 465,
                EnableSsl = true,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                UseDefaultCredentials = false,
                Credentials = new NetworkCredential(mail, mailPass)
            })
            {
                foreach (var toAddress in Emails)
                {
                    using (var message = new MailMessage(fromMail, toAddress)
                    {
                        Subject = msg.ToString(),
                        Body = string.Join(
                          msg.Text,
                          Environment.NewLine,
                          Environment.NewLine,
                          "===*******TECH INFO*******==",
                          Environment.NewLine,
                          Environment.NewLine,
                          msg.DetailedText
                          )
                    })
                    {
                        //var ct = new CancellationTokenSource(TimeSpan.FromSeconds(30));
                        smtp.Send(message);
                    }
                }
            }

        } // private static void SendMessageByEmail(Message msg)

    } // public  class Notify

    public enum Status { Info, Success, Warning, Error }

} // namespace Regata.Measurements.Managers