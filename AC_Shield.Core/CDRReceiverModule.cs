using LogLib;
using ModuleLib;
using ResultTypeLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace AC_Shield.Core
{
	public class CDRReceiverModule:ThreadModule
	{
		private DatabaseModule databaseModule;
		
		private int port;
		private UdpClient? listener = null;
		private string ipGroup;


		private static Regex CDRRegex = new Regex(@"[^]]+] +\|(?<CDR>(CALL_START).*)");
		private DateTimeParser dateTimeParser;

		public CDRReceiverModule(ILogger Logger,DatabaseModule DatabaseModule, int Port,string IPGroup) : base(Logger, ThreadPriority.Normal, 5000)
		{
			this.databaseModule = DatabaseModule;
			this.port=Port;
			this.ipGroup=IPGroup;
			dateTimeParser = new DateTimeParser();
		}


		protected override IResult<bool> OnStopping()
		{
			if (listener != null)
			{
				Log(Message.Information("Closing UDP listener"));
				Try(() => listener.Close());
				listener = null;
			}
			return base.OnStopping();
		}

		private IResult<CDR> ParseCDR(string Line)
		{
			string[] parts;
			CDR cdr;

			if (Line == null) return Result.Fail<CDR>(new ArgumentNullException(nameof(Line)));

		
			parts = Line.Split('|');
			if ((parts.Length < 38) || (parts.Length > 39)) return Result.Fail<CDR>(new InvalidDataException("Invalid SBC report format, please check SBC configuration"));

			return Try(() =>
			{
				cdr = new CDR();
				cdr.SBCReportType = parts[0].Trim();
				cdr.EPTyp = parts[1].Trim();
				cdr.SIPCallId = parts[2].Trim();
				cdr.SessionId = parts[3].Trim();
				cdr.Orig = parts[4].Trim();
				cdr.SourceIp = parts[5].Trim();
				cdr.SourcePort = parts[6].Trim();
				cdr.DestIp = parts[7].Trim();
				cdr.DestPort = parts[8].Trim();
				cdr.TransportType = parts[9].Trim();
				cdr.SrcURI = parts[10];
				cdr.SrcURIBeforeMap = parts[11].Trim();
				cdr.DstURI = parts[12].Trim();
				cdr.DstURIBeforeMap = parts[13].Trim();
				cdr.Duration = parts[14].Trim();
				cdr.TrmSd = parts[15].Trim();
				cdr.TrmReason = parts[16].Trim();
				cdr.TrmReasonCategory = parts[17].Trim();
				cdr.SetupTime = dateTimeParser.ParseCDRDate(parts[18].Trim());
				cdr.ConnectTime = dateTimeParser.ParseCDRDate(parts[19].Trim());
				cdr.ReleaseTime = dateTimeParser.ParseCDRDate(parts[20].Trim());
				cdr.RedirectReason = parts[21].Trim();
				cdr.RedirectURINum = parts[22].Trim();
				cdr.RedirectURINumBeforeMap = parts[23].Trim();
				cdr.TxSigIPDiffServ = parts[24].Trim();
				cdr.IPGroup = parts[25].Trim();
				cdr.SrdId = parts[26].Trim();
				cdr.SIPInterfaceId = parts[27].Trim();
				cdr.ProxySetId = parts[28].Trim();
				cdr.IpProfileId = parts[29].Trim();
				cdr.MediaRealmId = parts[30].Trim();
				cdr.DirectMedia = parts[31].Trim();
				cdr.SIPTrmReason = parts[32].Trim();
				cdr.SIPTermDesc = parts[33].Trim();
				cdr.Caller = parts[34].Trim();
				cdr.Callee = parts[35].Trim();
				cdr.Trigger = parts[36].Trim();
				cdr.LegId = parts[37].Trim();
				if (parts.Length == 39) cdr.VoiceAIConnectorName = parts[38].Trim();
				else cdr.VoiceAIConnectorName = "";

				return cdr;
			});

		}
		protected override void ThreadLoop()
		{
			bool result;
			IPEndPoint? groupEP=null;
			byte[]? bytes=null;
			string syslog;
			Match match;
			CDR? cdr=null;

			Try(()=>new UdpClient(port)).Match(
				
				success => { Log(Message.Information($"UDP listener created on port {port}")); listener = success; },
				failure => Log(Message.Error($"Failed to create UDP listener on port {port}: {failure.Message}"))
			);
			if (listener == null) return;

			Try(() => new IPEndPoint(IPAddress.Any, port)).Match(

				success => { Log(Message.Information($"Endpoint created")); groupEP = success; },
				failure => Log(Message.Error($"Failed to create endpoint: {failure.Message}"))
			);
			if (groupEP == null) return;



			Log(Message.Information("Waiting for data or quit signal"));
			while (State == ModuleStates.Started)
			{
				
				result=Try(() =>  listener.Receive(ref groupEP) ).Match(
					success => bytes = success,
					failure => Log(Message.Error($"Error during data reception"))
				);
				if (!result) break;

				if (bytes==null) continue;

				syslog = Encoding.ASCII.GetString(bytes, 0, bytes.Length);
				Log(Message.Debug(syslog));

				// check if message matches CDR format
				match = CDRRegex.Match(syslog);
				if (!match.Success) continue;
				

				// parse CDR
				ParseCDR(match.Groups["CDR"].Value).Match(
					success => cdr=success,
					failure => Log(Message.Error($"Failed to parse CDR message: {failure.Message}"))
				);
				if (cdr== null) continue;
				if (cdr.IPGroup != ipGroup) continue;

				Log(Message.Information($"New call started on IPGroup {cdr.IPGroup} at {cdr.SetupTime} from {cdr.SrcURI}"));
				databaseModule.InsertCDR(cdr).Match(
					success => Log(Message.Information($"CDR inserted successfully")),
					failure => Log(Message.Error($"Failed to insert CDR into database: {failure.Message}"))
				);
			}



		}


	}
}
