using Etutlist.Data;
using Etutlist.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Etutlist.Controllers
{
    public class OzelGunController : Controller
    {
        private readonly AppDbContext _context;
        public OzelGunController(AppDbContext context) => _context = context;

        public async Task<IActionResult> Index() =>
            View(await _context.OzelGunler.ToListAsync());

        public IActionResult Create() => View();

        [HttpPost]
        public async Task<IActionResult> Create(OzelGun ozelGun)
        {
            if (ModelState.IsValid)
            {
                _context.Add(ozelGun);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(ozelGun);
        }

        public async Task<IActionResult> Edit(int id)
        {
            var ozelGun = await _context.OzelGunler.FindAsync(id);
            return ozelGun == null ? NotFound() : View(ozelGun);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(int id, OzelGun ozelGun)
        {
            if (id != ozelGun.Id) return NotFound();
            if (ModelState.IsValid)
            {
                _context.Update(ozelGun);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(ozelGun);
        }

        public async Task<IActionResult> Delete(int id)
        {
            var ozelGun = await _context.OzelGunler.FindAsync(id);
            return ozelGun == null ? NotFound() : View(ozelGun);
        }

        [HttpPost, ActionName("Delete")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var ozelGun = await _context.OzelGunler.FindAsync(id);
            if (ozelGun != null)
            {
                _context.OzelGunler.Remove(ozelGun);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }
    }
}