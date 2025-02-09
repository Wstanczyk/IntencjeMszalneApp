using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

public class MassesController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<MassesController> _logger;

    public MassesController(ApplicationDbContext context, ILogger<MassesController> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<IActionResult> Index(DateTime? filterDate)
    {
        var query = _context.Masses.Include(m => m.Intentions).AsQueryable();

        if (filterDate.HasValue)
        {
            query = query.Where(m => m.Date.Date == filterDate.Value.Date);
        }

        var masses = await query.ToListAsync();
        return View(masses);
    }

    [HttpPost]
    public async Task<IActionResult> Reserve(int massId, string intentionText)
    {
        if (string.IsNullOrWhiteSpace(intentionText))
        {
            return BadRequest("Intencja nie może być pusta.");
        }

        if (!User.Identity.IsAuthenticated)
        {
            // Zapisujemy dane rezerwacji w sesji, aby wrócić do niej po zalogowaniu
            HttpContext.Session.SetInt32("PendingMassId", massId);
            HttpContext.Session.SetString("PendingIntentionText", intentionText);

            _logger.LogInformation("Użytkownik niezalogowany. Przekierowanie do Google Login.");
            return Challenge(new AuthenticationProperties { RedirectUri = Url.Action("GoogleResponse", "Account") }, GoogleDefaults.AuthenticationScheme);
        }

        return await CompleteReservation(massId, intentionText);
    }

    [HttpGet]
    public async Task<IActionResult> Reserve()
    {
        var massId = HttpContext.Session.GetInt32("PendingMassId");
        var intentionText = HttpContext.Session.GetString("PendingIntentionText");

        if (!massId.HasValue || string.IsNullOrEmpty(intentionText))
        {
            return RedirectToAction("Index"); // Brak danych → wracamy do listy mszy
        }

        // Czyścimy sesję
        HttpContext.Session.Remove("PendingMassId");
        HttpContext.Session.Remove("PendingIntentionText");

        return await CompleteReservation(massId.Value, intentionText);
    }

    private async Task<IActionResult> CompleteReservation(int massId, string intentionText)
    {
        var userEmail = User.FindFirst(ClaimTypes.Email)?.Value;
        if (string.IsNullOrEmpty(userEmail))
        {
            _logger.LogWarning("Nie można znaleźć adresu email zalogowanego użytkownika.");
            return Unauthorized();
        }

        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == userEmail);
        if (user == null)
        {
            _logger.LogWarning("Użytkownik o emailu {UserEmail} nie został znaleziony w bazie danych.", userEmail);
            return Unauthorized();
        }

        var mass = await _context.Masses.Include(m => m.Intentions)
                                        .FirstOrDefaultAsync(m => m.Id == massId);
        if (mass == null)
        {
            _logger.LogWarning("Msza o ID {MassId} nie została znaleziona.", massId);
            return NotFound("Nie znaleziono mszy.");
        }

        if (mass.Intentions.Count >= mass.MaxIntentions)
        {
            _logger.LogWarning("Próba dodania intencji do pełnej mszy (ID: {MassId}).", massId);
            return BadRequest("Nie można dodać więcej intencji do tej mszy.");
        }

        var intention = new Intention
        {
            MassId = massId,
            UserId = user.Id,
            IntentionText = intentionText,
            CreatedAt = DateTime.UtcNow
        };

        _context.Intentions.Add(intention);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Dodano nową intencję dla mszy {MassId} przez użytkownika {UserEmail}.", massId, userEmail);

        return RedirectToAction("Index");
    }

    [Authorize(Roles = "Admin")]
    public IActionResult ManageIntentions()
    {
        var intentions = _context.Intentions.Include(i => i.Mass).ThenInclude(m => m.Intentions).ToList();
        return View(intentions);
    }

    [Authorize(Roles = "Admin")]
    [HttpPost]
    public async Task<IActionResult> DeleteIntention(int intentionId)
    {
        var intention = await _context.Intentions.FindAsync(intentionId);

        if (intention == null)
        {
            _logger.LogWarning("Intencja o ID {IntentionId} nie została znaleziona.", intentionId);
            return NotFound();
        }

        _context.Intentions.Remove(intention);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Usunięto intencję o ID {IntentionId}.", intentionId);

        return RedirectToAction("ManageIntentions");
    }
}
