using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Security.Claims;
using System.Threading.Tasks;

public class AccountController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<AccountController> _logger;
    public AccountController(ApplicationDbContext context, ILogger<AccountController> logger)
    {
        _context = context;
        _logger = logger;
    }

    public IActionResult Login()
    {
        if (User.Identity.IsAuthenticated)
        {
            return RedirectToAction("Index", "Home"); // Jeśli użytkownik jest już zalogowany, przekieruj na stronę główną
        }

        var redirectUrl = Url.Action(nameof(GoogleResponse), "Account");
        var properties = new AuthenticationProperties { RedirectUri = redirectUrl };
        return Challenge(properties, GoogleDefaults.AuthenticationScheme);
    }

    public async Task<IActionResult> GoogleResponse()
    {
        var result = await HttpContext.AuthenticateAsync(GoogleDefaults.AuthenticationScheme);

        if (!result.Succeeded || result.Principal == null)
        {
            _logger.LogWarning("Logowanie nieudane.");
            return RedirectToAction("Login", "Account");
        }

        var claims = result.Principal.Identities.FirstOrDefault()?.Claims;
        var email = claims?.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
        var googleId = claims?.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
        var name = claims?.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value;

        if (email != null && googleId != null)
        {
            var user = _context.Users.FirstOrDefault(u => u.GoogleId == googleId);
            if (user == null)
            {
                user = new User { GoogleId = googleId, Email = email, Name = name, Role = "User" };
                _context.Users.Add(user);
                await _context.SaveChangesAsync();
            }

            var userClaims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.GoogleId),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Name, user.Name),
                new Claim(ClaimTypes.Role, user.Role ?? "User")
            };

            var claimsIdentity = new ClaimsIdentity(userClaims, CookieAuthenticationDefaults.AuthenticationScheme);
            var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);

            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, claimsPrincipal, new AuthenticationProperties
            {
                IsPersistent = true,
                ExpiresUtc = DateTime.UtcNow.AddMinutes(30)
            });

            // 🔹 Po zalogowaniu sprawdzamy, czy użytkownik miał niedokończoną rezerwację
            var pendingMassId = HttpContext.Session.GetInt32("PendingMassId");
            var pendingIntentionText = HttpContext.Session.GetString("PendingIntentionText");

            if (pendingMassId.HasValue && !string.IsNullOrEmpty(pendingIntentionText))
            {
                // Przekierowanie do rezerwacji
                return RedirectToAction("Reserve", "Masses");
            }

            // 🔹 Jeśli użytkownik ma rolę "Admin", przekieruj do zarządzania intencjami
            if (user.Role == "Admin")
            {
                return RedirectToAction("ManageIntentions", "Masses");
            }
        }

        return RedirectToAction("Index", "Home");
    }

    public async Task<IActionResult> Logout()
    {
        // Wylogowanie użytkownika z systemu cookies
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

        // Przekierowanie na stronę główną po wylogowaniu
        return RedirectToAction("Index", "Home");
    }
}
