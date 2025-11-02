using Etutlist.Data;
using Etutlist.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace Etutlist.Controllers
{
    public class MazeretController : Controller
    {
        private readonly AppDbContext _context;
        public MazeretController(AppDbContext context) => _context = context;

        public async Task<IActionResult> Index()
        {
            var mazeretler = await _context.Mazeretler
                .Include(m => m.Personel)
                .ToListAsync();
            return View(mazeretler);
        }

        public IActionResult Create()
        {
            ViewBag.PersonelList = new SelectList(_context.Personeller, "Id", "Ad");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Mazeret mazeret)
        {
            if (ModelState.IsValid)
            {
                _context.Mazeretler.Add(mazeret);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewBag.PersonelList = new SelectList(_context.Personeller, "Id", "Ad", mazeret.PersonelId);
            return View(mazeret);
        }


        public async Task<IActionResult> Edit(int id)
        {
            var mazeret = await _context.Mazeretler.FindAsync(id);
            if (mazeret == null) return NotFound();

            ViewBag.PersonelList = new SelectList(_context.Personeller, "Id", "Ad", mazeret.PersonelId);
            return View(mazeret);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(int id, Mazeret mazeret)
        {
            if (id != mazeret.Id) return NotFound();
            if (ModelState.IsValid)
            {
                _context.Update(mazeret);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewBag.PersonelList = new SelectList(_context.Personeller, "Id", "Ad", mazeret.PersonelId);
            return View(mazeret);
        }

        public async Task<IActionResult> Delete(int id)
        {
            var mazeret = await _context.Mazeretler
                .Include(m => m.Personel)
                .FirstOrDefaultAsync(m => m.Id == id);
            return mazeret == null ? NotFound() : View(mazeret);
        }

        [HttpPost, ActionName("Delete")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var mazeret = await _context.Mazeretler.FindAsync(id);
            if (mazeret != null)
            {
                _context.Mazeretler.Remove(mazeret);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }
    }
}