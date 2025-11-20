using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading.Tasks;


namespace AC_Shield.Core
{
	public class BasicAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
	{
		//private readonly IMemberRepository _memberRepository

		public BasicAuthenticationHandler(IOptionsMonitor<AuthenticationSchemeOptions> OptionsMonitor,UrlEncoder UrlEncoder,ILoggerFactory LoggerFactory) 
			: base(OptionsMonitor, LoggerFactory, UrlEncoder)
		{
			
        }
		
		protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
		{
			string? member = null;
			

			await Task.Yield();
			if (!Request.Headers.ContainsKey("Authorization")) return AuthenticateResult.Fail("Missing Authorization header");

			try
			{
				if (!AuthenticationHeaderValue.TryParse(Request.Headers["Authorization"], out var authenticationHeader)) return AuthenticateResult.Fail("Invalid Header Format");
				if (string.IsNullOrEmpty(authenticationHeader.Parameter)) return AuthenticateResult.Fail("Missing Header Content");

				var credentialBytes = Convert.FromBase64String(authenticationHeader.Parameter);

				var credential = Encoding.UTF8.GetString(credentialBytes).Split(':', 2);

				if (credential.Length != 2) return AuthenticateResult.Fail("Invalid Header Content");
				
				var userName = credential[0];
				var password = credential[1];

				// check here is user exists in system
				member = userName;// await _memberRepository.ValidateMember(userName, password);
			}
			catch (Exception ex)
			{
				return AuthenticateResult.Fail(ex.Message);
			}
			if (member == null)
			{
				return AuthenticateResult.Fail("Invalid Member");
			}

			var claims = new[]
			{
				new Claim("NameIdentifier", member),
				new Claim("Name", member),
			};

			var claimIdentity = new ClaimsIdentity(claims, Scheme.Name);
			var claimPrincipal = new ClaimsPrincipal(claimIdentity);
			var ticket = new AuthenticationTicket(claimPrincipal, Scheme.Name);

			return AuthenticateResult.Success(ticket);
		}
	}
}

