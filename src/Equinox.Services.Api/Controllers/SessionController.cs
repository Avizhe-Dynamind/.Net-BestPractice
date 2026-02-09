using Equinox.Infra.CrossCutting.Identity.Services;
using Equinox.Infra.CrossCutting.Identity.User;
using Equinox.Services.Api.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Equinox.Services.Api.Controllers
{
    [Authorize]
    [Route("api/sessions")]
    [ApiController]
    public class SessionController : ApiController
    {
        private readonly ISessionService _sessionService;
        private readonly IAspNetUser _aspNetUser;

        public SessionController(ISessionService sessionService, IAspNetUser aspNetUser)
        {
            _sessionService = sessionService;
            _aspNetUser = aspNetUser;
        }

        [HttpGet]
        public async Task<ActionResult<List<SessionViewModel>>> GetActiveSessions()
        {
            var userId = _aspNetUser.GetUserId().ToString();
            var sessions = await _sessionService.GetActiveSessionsAsync(userId);

            var currentIp = _aspNetUser.GetHttpContext().Connection.RemoteIpAddress?.ToString();

            var viewModels = sessions.Select(s => new SessionViewModel
            {
                Id = s.Id,
                DeviceName = s.DeviceName,
                IpAddress = s.IpAddress,
                Location = s.Location,
                CreatedAt = s.CreatedAt,
                LastActivityAt = s.LastActivityAt,
                IsCurrent = s.IpAddress == currentIp
            }).ToList();

            return Ok(viewModels);
        }

        [HttpDelete("{sessionId}")]
        public async Task<IActionResult> RevokeSession(Guid sessionId)
        {
            var userId = _aspNetUser.GetUserId().ToString();
            await _sessionService.RevokeSessionAsync(sessionId, userId);
            return Ok(new { message = "Session revoked successfully" });
        }

        [HttpDelete("revoke-all")]
        public async Task<IActionResult> RevokeAllSessions()
        {
            var userId = _aspNetUser.GetUserId().ToString();
            await _sessionService.RevokeAllSessionsAsync(userId);
            return Ok(new { message = "All sessions revoked successfully" });
        }
    }
}
