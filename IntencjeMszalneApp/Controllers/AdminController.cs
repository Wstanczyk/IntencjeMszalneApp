using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

[Authorize(Roles = "Admin")]
public class AdminController : Controller
{
    private readonly ApplicationDbContext _context;

    public AdminController(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var intentions = await _context.Intentions.Include(i => i.Mass).Include(i => i.User).ToListAsync();
        return View(intentions);
    }

    public async Task<IActionResult> Delete(int id)
    {
        var intention = await _context.Intentions.FindAsync(id);
        if (intention != null)
        {
            _context.Intentions.Remove(intention);
            await _context.SaveChangesAsync();
        }
        return RedirectToAction("Index");
    }
}