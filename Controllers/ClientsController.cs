using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TracKeee.Areas.Identity.Data;
using TracKeee.Models;

namespace TracKeee.Controllers
{
    [Authorize]
    public class ClientsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public ClientsController(ApplicationDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: Clients
        public async Task<IActionResult> Index()
        {
            var userId = _userManager.GetUserId(User);
            var clients = await _context.Clients
                .Where(c => c.UserId == userId)
                .OrderBy(c => c.Name)
                .ToListAsync();
            return View(clients);
        }

        // GET: Clients/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var userId = _userManager.GetUserId(User);
            var client = await _context.Clients
                .FirstOrDefaultAsync(m => m.Id == id && m.UserId == userId);

            if (client == null) return NotFound();
            return View(client);
        }

        // GET: Clients/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Clients/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Name,ContactPerson,Email,Phone,VatNumber,Address,Notes")] Client client)
        {
            ModelState.Remove("UserId");

            if (ModelState.IsValid)
            {
                client.UserId = _userManager.GetUserId(User)!;
                client.CreatedAt = DateTime.UtcNow;
                _context.Add(client);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(client);
        }

        // GET: Clients/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var userId = _userManager.GetUserId(User);
            var client = await _context.Clients
                .FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId);

            if (client == null) return NotFound();
            return View(client);
        }

        // POST: Clients/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,ContactPerson,Email,Phone,VatNumber,Address,Notes")] Client client)
        {
            if (id != client.Id) return NotFound();

            ModelState.Remove("UserId");

            var userId = _userManager.GetUserId(User);
            var existing = await _context.Clients
                .FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId);

            if (existing == null) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    existing.Name = client.Name;
                    existing.ContactPerson = client.ContactPerson;
                    existing.Email = client.Email;
                    existing.Phone = client.Phone;
                    existing.VatNumber = client.VatNumber;
                    existing.Address = client.Address;
                    existing.Notes = client.Notes;

                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ClientExists(client.Id)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(client);
        }

        // GET: Clients/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var userId = _userManager.GetUserId(User);
            var client = await _context.Clients
                .FirstOrDefaultAsync(m => m.Id == id && m.UserId == userId);

            if (client == null) return NotFound();
            return View(client);
        }

        // POST: Clients/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var userId = _userManager.GetUserId(User);
            var client = await _context.Clients
                .FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId);

            if (client != null)
            {
                _context.Clients.Remove(client);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        private bool ClientExists(int id)
        {
            var userId = _userManager.GetUserId(User);
            return _context.Clients.Any(e => e.Id == id && e.UserId == userId);
        }
    }
}