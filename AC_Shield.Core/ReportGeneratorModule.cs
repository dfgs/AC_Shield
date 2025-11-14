using LogLib;
using ModuleLib;
using ResultTypeLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace AC_Shield.Core
{
	public class ReportGeneratorModule : AtModule<DateTime>
	{
		private DatabaseModule databaseModule;
		private TimeOnly reportGenerationTime;
		private string smtpServer;
		private string from;
		private string to;
		private string subject;
		private string login;
		private string password;

		public ReportGeneratorModule(ILogger Logger, DatabaseModule DatabaseModule, TimeOnly ReportGenerationTime,string SMTPServer,string Login,string Password, string From,string To,string Subject) : base(Logger, ThreadPriority.Normal, 5000)
		{
			this.databaseModule = DatabaseModule;
			this.reportGenerationTime = ReportGenerationTime;
			this.smtpServer= SMTPServer;
			this.login= Login;
			this.password= Password;
			this.from = From;
			this.to = To;
			this.subject= Subject;

		}

		protected override IResult<bool> OnStarting()
		{
			DateTime nextEventTime;

			nextEventTime=new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, reportGenerationTime.Hour, reportGenerationTime.Minute, reportGenerationTime.Second);	
			if (nextEventTime<=DateTime.Now) nextEventTime=nextEventTime.AddDays(1);

			Log(Message.Information($"Enqueue first event at {nextEventTime}"));
			Add(nextEventTime, nextEventTime);

			return base.OnStarting();
		}

		private void SendMail(BlackListItem[] BlackList)
		{
			SmtpClient client = new SmtpClient(smtpServer);
			if (!string.IsNullOrEmpty(login) && !string.IsNullOrEmpty(password)) client.Credentials=new System.Net.NetworkCredential(login, password);
			MailAddress from = new MailAddress(this.from);
			MailAddress to = new MailAddress(this.to);

			
			MailMessage message = new MailMessage(from, to);
			message.Body = 
				$$"""
				Hello,
				
				Please find a report of blacklisted callers:
				
				{{string.Join("\r\n", BlackList.Select(item=>item.Caller))}}

				Best regards
				""";
			
			
			message.BodyEncoding = System.Text.Encoding.UTF8;
			message.Subject = this.subject;
			message.SubjectEncoding = System.Text.Encoding.UTF8;
			
			client.Send(message);
		}

		protected override IResult<bool> OnTriggerEvent(DateTime Event)
		{
			DateTime nextEventTime;
			BlackListItem[] blackList;

			Log(Message.Information($"Generating new report"));

			if (databaseModule.GetBlackList(DateTime.Now).Match(
					success => Log(Message.Information($"Black list collected succesfully")),
					failure => Log(Message.Error($"Failed to get black list: {failure.Message}"))
				).Succeeded(out blackList))
			{

				Try(() => SendMail(blackList)).Match(
					success => Log(Message.Information($"Report sent succesfully")),
					failure => Log(Message.Error($"Failed to send report: {failure.Message}"))
				);

			}


			nextEventTime = Event.AddDays(1);
			Log(Message.Information($"Enqueue next event at {nextEventTime}"));
			Add(nextEventTime, nextEventTime);

			return Result.Success(true);

		}


	}
}
