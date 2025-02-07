using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Threading.Tasks;

public class AccountController : Controller
{
    private readonly ApplicationDbContext _context;

    public AccountController(ApplicationDbContext context)
    {
        _context = context;
    }

    public IActionResult Login()
    {
        var redirectUrl = Url.Action(nameof(GoogleResponse), "Account");
        var properties = new AuthenticationProperties { RedirectUri = redirectUrl };
        return Challenge(properties, GoogleDefaults.AuthenticationScheme);
    }

    public async Task<IActionResult> GoogleResponse()
    {
        var result = await HttpContext.AuthenticateAsync(GoogleDefaults.AuthenticationScheme);

        if (!result.Succeeded || result.Principal == null)
        {
            return RedirectToAction("Login", "Account"); // Jeśli logowanie się nie udało, wracamy do ekranu logowania
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
                IsPersistent = true, // Utrzymuje sesję po zamknięciu przeglądarki
                ExpiresUtc = DateTime.UtcNow.AddMinutes(30)
            });
        }

        return RedirectToAction("Index", "Home"); // Przekierowanie do strony głównej po zalogowaniu
    }



    public IActionResult Logout()
    {
        return SignOut(new AuthenticationProperties { RedirectUri = "/" },
                       CookieAuthenticationDefaults.AuthenticationScheme,
                       GoogleDefaults.AuthenticationScheme);
    }
}
