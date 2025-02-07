using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

public class MassesController : Controller
{
    private readonly ApplicationDbContext _context;

    public MassesController(ApplicationDbContext context)
    {
        _context = context;
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


    [Authorize]
    public async Task<IActionResult> Reserve(int massId, string intentionText)
    {
        var userId = _context.Users.FirstOrDefault(u => u.Email == User.Identity.Name)?.Id;
        if (userId == null) return Unauthorized();

        var mass = await _context.Masses.Include(m => m.Intentions)
                                        .FirstOrDefaultAsync(m => m.Id == massId);
        if (mass == null || mass.Intentions.Count >= mass.MaxIntentions)
        {
            return BadRequest("Nie można dodać więcej intencji do tej mszy.");
        }

        var intention = new Intention
        {
            MassId = massId,
            UserId = userId.Value,
            IntentionText = intentionText,
            CreatedAt = DateTime.UtcNow
        };

        _context.Intentions.Add(intention);
        await _context.SaveChangesAsync();

        return RedirectToAction("Index");
    }

}
