using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace AC_Shield.Core
{
	public class AC_CDR 
	{
		private static Regex prefixRegex = new Regex(@"(?<Prefix>[^@]+).*");

		public string? SIPCallId { get; set; }
		public string? SessionId { get; set; }
		public string? SBCReportType { get; set; }
		public string? EPTyp { get; set; }
		public string? Orig { get; set; }
		public string? SourceIp { get; set; }
		public string? SourcePort { get; set; }
		public string? DestIp { get; set; }
		public string? DestPort { get; set; }
		public string? TransportType { get; set; }
		public string? SrcURI { get; set; }
		public string? SrcURIBeforeMap { get; set; }
		public string? DstURI { get; set; }
		public string? DstURIBeforeMap { get; set; }
		public string? Duration { get; set; }
		public string? TrmSd { get; set; }
		public string? TrmReason { get; set; }
		public string? TrmReasonCategory { get; set; }
		public DateTime? SetupTime { get; set; }
		public DateTime? ConnectTime { get; set; }
		public DateTime? ReleaseTime { get; set; }
		public string? RedirectReason { get; set; }
		public string? RedirectURINum { get; set; }
		public string? RedirectURINumBeforeMap { get; set; }
		public string? TxSigIPDiffServ { get; set; }
		public string? IPGroup { get; set; }
		public string? SrdId { get; set; }
		public string? SIPInterfaceId { get; set; }
		public string? ProxySetId { get; set; }
		public string? IpProfileId { get; set; }
		public string? MediaRealmId { get; set; }
		public string? DirectMedia { get; set; }
		public string? SIPTrmReason { get; set; }
		public string? SIPTermDesc { get; set; }
		public string? Caller { get; set; }
		public string? Callee { get; set; }
		public string? Trigger { get; set; }
		public string? LegId { get; set; }
		public string? VoiceAIConnectorName { get; set; }

		public string? GetCallerNumber()
		{
			string prefix;
			Match match;

			if( SrcURI==null ) return null;
			match = prefixRegex.Match(SrcURI);
			if (!match.Success) return null;
			prefix = match.Groups["Prefix"].Value;
			return prefix;
		}

	}
}
