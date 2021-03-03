
using IdentityModel;
using IdentityServer4;
using IdentityServer4.Configuration;
using IdentityServer4.Events;
using IdentityServer4.Extensions;
using IdentityServer4.Models;
using IdentityServer4.Services;
using IdentityServer4.Stores;
using IdentityServer4.Validation;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace IdentityServer.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class AccountController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IIdentityServerInteractionService _interaction;
        private readonly IClientStore _clientStore;
        private readonly IAuthenticationSchemeProvider _schemeProvider;
        private readonly IEventService _events;
        public AccountController(
             UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            IIdentityServerInteractionService interaction,
            IClientStore clientStore,
            IAuthenticationSchemeProvider schemeProvider,
            IEventService events
            )
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _interaction = interaction;
            _clientStore = clientStore;
            _schemeProvider = schemeProvider;
            _events = events;
        }

        [HttpGet]
        public IActionResult Index()
        {
            return Ok();
        }
        [HttpPost]
        public async Task<IActionResult> Login(string userName , string password, [FromServices] ITokenService TS,
    [FromServices] IUserClaimsPrincipalFactory<ApplicationUser> principalFactory,
    [FromServices] IdentityServerOptions options)
        {
            // check if we are in the context of an authorization request
            var context = await _interaction.GetAuthorizationContextAsync("");

            var result = await _signInManager.PasswordSignInAsync(userName, password,true, lockoutOnFailure: true);
            if (result.Succeeded)
            {
                var user = await _userManager.FindByNameAsync(userName);
                await _events.RaiseAsync(new UserLoginSuccessEvent(user.UserName, user.Id, user.UserName, clientId: context?.Client.ClientId));
                string TokenValue = await GenrateToken(TS, principalFactory, options, user);
                return Ok(TokenValue);
            }
            return Ok();
        }

        private async Task<string> GenrateToken(ITokenService TS, IUserClaimsPrincipalFactory<ApplicationUser> principalFactory, IdentityServerOptions options, ApplicationUser user)
        {
            var Request = new TokenCreationRequest();
            var User = await _userManager.FindByIdAsync(user.Id);
            var IdentityPricipal = await principalFactory.CreateAsync(User);
            var IdentityUser = new IdentityServerUser(User.Id.ToString());
            IdentityUser.AdditionalClaims = IdentityPricipal.Claims.ToArray();
            IdentityUser.DisplayName = User.UserName;
            IdentityUser.AuthenticationTime = System.DateTime.UtcNow;
            IdentityUser.IdentityProvider = IdentityServerConstants.LocalIdentityProvider;
            Request.Subject = IdentityUser.CreatePrincipal();
            Request.IncludeAllIdentityClaims = true;
            Request.ValidatedRequest = new ValidatedRequest();
            Request.ValidatedRequest.Subject = Request.Subject;
            Request.ValidatedRequest.SetClient(Config.Clients.First());
            Request.ValidatedResources = new ResourceValidationResult(new Resources(Config.IdentityResources, new ApiResource[] { }, Config.ApiScopes));

            Request.ValidatedRequest.Options = options;
            Request.ValidatedRequest.ClientClaims = IdentityUser.AdditionalClaims;
            var Token = await TS.CreateAccessTokenAsync(Request);
            Token.Issuer = HttpContext.Request.Scheme + "://" + HttpContext.Request.Host.Value;
            var TokenValue = await TS.CreateSecurityTokenAsync(Token);
            return TokenValue;
        }
    }
}
