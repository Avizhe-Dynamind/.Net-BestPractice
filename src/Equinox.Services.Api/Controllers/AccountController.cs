using Equinox.Infra.CrossCutting.Identity.API;
using Equinox.Infra.CrossCutting.Identity.Models;
using Equinox.Infra.CrossCutting.Identity.Services;
using Equinox.Infra.CrossCutting.Identity.User;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Equinox.Services.Api.Controllers
{
    [EnableRateLimiting("authentication")]
    [Route("account")]
    [ApiController]
    public class AccountController : ApiController
    {
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly ITokenService _tokenService;
        private readonly ISessionService _sessionService;
        private readonly ISecurityAuditService _auditService;
        private readonly IAspNetUser _aspNetUser;
        private readonly AppJwtSettings _appJwtSettings;

        public AccountController(
            SignInManager<IdentityUser> signInManager,
            UserManager<IdentityUser> userManager,
            ITokenService tokenService,
            ISessionService sessionService,
            ISecurityAuditService auditService,
            IAspNetUser aspNetUser,
            IOptions<AppJwtSettings> appJwtSettings)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _tokenService = tokenService;
            _sessionService = sessionService;
            _auditService = auditService;
            _aspNetUser = aspNetUser;
            _appJwtSettings = appJwtSettings.Value;
        }

        [HttpPost("register")]
        public async Task<ActionResult> Register(RegisterUser registerUser)
        {
            if (!ModelState.IsValid) return CustomResponse(ModelState);

            var user = new IdentityUser
            {
                UserName = registerUser.Email,
                Email = registerUser.Email,
                EmailConfirmed = true
            };

            var result = await _userManager.CreateAsync(user, registerUser.Password);

            var ipAddress = _aspNetUser.GetHttpContext().Connection.RemoteIpAddress?.ToString();
            var userAgent = _aspNetUser.GetHttpContext().Request.Headers["User-Agent"].ToString();

            if (result.Succeeded)
            {
                await _auditService.LogEventAsync(
                    SecurityEventType.AccountCreated,
                    user.Id,
                    user.Email,
                    ipAddress,
                    userAgent,
                    true);

                // Generate tokens for new user
                var authResult = await _tokenService.GenerateTokensAsync(user, ipAddress, userAgent);

                if (authResult.Success)
                {
                    // Create session
                    await _sessionService.CreateSessionAsync(user.Id, authResult.RefreshToken, ipAddress, userAgent);

                    return CustomResponse(new
                    {
                        accessToken = authResult.AccessToken,
                        refreshToken = authResult.RefreshToken,
                        expiresAt = authResult.AccessTokenExpiration,
                        user = new
                        {
                            email = user.Email,
                            id = user.Id
                        }
                    });
                }
            }

            await _auditService.LogEventAsync(
                SecurityEventType.AccountCreated,
                null,
                registerUser.Email,
                ipAddress,
                userAgent,
                false,
                "Failed to create account");

            foreach (var error in result.Errors)
            {
                AddError(error.Description);
            }

            return CustomResponse();
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginUser loginUser)
        {
            if (!ModelState.IsValid) return CustomResponse(ModelState);

            var ipAddress = _aspNetUser.GetHttpContext().Connection.RemoteIpAddress?.ToString();
            var userAgent = _aspNetUser.GetHttpContext().Request.Headers["User-Agent"].ToString();

            var result = await _signInManager.PasswordSignInAsync(loginUser.Email, loginUser.Password, false, true);

            if (result.Succeeded)
            {
                var user = await _userManager.FindByEmailAsync(loginUser.Email);

                await _auditService.LogEventAsync(
                    SecurityEventType.LoginSuccess,
                    user.Id,
                    user.Email,
                    ipAddress,
                    userAgent,
                    true);

                var authResult = await _tokenService.GenerateTokensAsync(user, ipAddress, userAgent);

                if (authResult.Success)
                {
                    // Create session
                    await _sessionService.CreateSessionAsync(user.Id, authResult.RefreshToken, ipAddress, userAgent);

                    return CustomResponse(new
                    {
                        accessToken = authResult.AccessToken,
                        refreshToken = authResult.RefreshToken,
                        expiresAt = authResult.AccessTokenExpiration,
                        user = new
                        {
                            email = user.Email,
                            id = user.Id
                        }
                    });
                }

                AddError("Failed to generate authentication tokens");
                return CustomResponse();
            }

            if (result.IsLockedOut)
            {
                await _auditService.LogEventAsync(
                    SecurityEventType.LoginLockedOut,
                    null,
                    loginUser.Email,
                    ipAddress,
                    userAgent,
                    false,
                    "Account is locked out");

                AddError("This user is temporarily blocked");
                return CustomResponse();
            }

            await _auditService.LogEventAsync(
                SecurityEventType.LoginFailed,
                null,
                loginUser.Email,
                ipAddress,
                userAgent,
                false,
                "Invalid credentials");

            AddError("Incorrect user or password");
            return CustomResponse();
        }

        [HttpPost("refresh")]
        public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request)
        {
            if (!ModelState.IsValid) return CustomResponse(ModelState);

            var ipAddress = _aspNetUser.GetHttpContext().Connection.RemoteIpAddress?.ToString();
            var userAgent = _aspNetUser.GetHttpContext().Request.Headers["User-Agent"].ToString();

            var authResult = await _tokenService.RefreshTokenAsync(
                request.AccessToken,
                request.RefreshToken,
                ipAddress,
                userAgent);

            if (!authResult.Success)
            {
                await _auditService.LogEventAsync(
                    SecurityEventType.TokenRefreshFailed,
                    null,
                    null,
                    ipAddress,
                    userAgent,
                    false,
                    string.Join(", ", authResult.Errors));

                foreach (var error in authResult.Errors)
                {
                    AddError(error);
                }
                return CustomResponse();
            }

            // Update session activity
            await _sessionService.UpdateSessionActivityAsync(authResult.RefreshToken);

            await _auditService.LogEventAsync(
                SecurityEventType.TokenRefreshed,
                null, // Could extract from token if needed
                null,
                ipAddress,
                userAgent,
                true);

            return CustomResponse(new
            {
                accessToken = authResult.AccessToken,
                refreshToken = authResult.RefreshToken,
                expiresAt = authResult.AccessTokenExpiration
            });
        }

        [Authorize]
        [HttpPost("logout")]
        public async Task<IActionResult> Logout([FromBody] LogoutRequest request)
        {
            var userId = _aspNetUser.GetUserId().ToString();
            var ipAddress = _aspNetUser.GetHttpContext().Connection.RemoteIpAddress?.ToString();
            var userAgent = _aspNetUser.GetHttpContext().Request.Headers["User-Agent"].ToString();

            if (!string.IsNullOrEmpty(userId) && !string.IsNullOrEmpty(request.RefreshToken))
            {
                await _tokenService.RevokeTokenAsync(request.RefreshToken, userId);
                await _signInManager.SignOutAsync();

                await _auditService.LogEventAsync(
                    SecurityEventType.Logout,
                    userId,
                    _aspNetUser.GetUserEmail(),
                    ipAddress,
                    userAgent,
                    true);

                return Ok(new { message = "Logged out successfully" });
            }

            return BadRequest(new { message = "Invalid logout request" });
        }

        [Authorize]
        [HttpPost("logout-all")]
        public async Task<IActionResult> LogoutAllDevices()
        {
            var userId = _aspNetUser.GetUserId().ToString();
            var ipAddress = _aspNetUser.GetHttpContext().Connection.RemoteIpAddress?.ToString();
            var userAgent = _aspNetUser.GetHttpContext().Request.Headers["User-Agent"].ToString();

            if (!string.IsNullOrEmpty(userId))
            {
                await _tokenService.RevokeAllUserTokensAsync(userId);
                await _sessionService.RevokeAllSessionsAsync(userId);
                await _signInManager.SignOutAsync();

                await _auditService.LogEventAsync(
                    SecurityEventType.LogoutAll,
                    userId,
                    _aspNetUser.GetUserEmail(),
                    ipAddress,
                    userAgent,
                    true);

                return Ok(new { message = "Logged out from all devices successfully" });
            }

            return BadRequest(new { message = "Invalid request" });
        }
    }

    // Request models
    public class RefreshTokenRequest
    {
        public string AccessToken { get; set; }
        public string RefreshToken { get; set; }
    }

    public class LogoutRequest
    {
        public string RefreshToken { get; set; }
    }
}
