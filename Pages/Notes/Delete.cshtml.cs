using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using HW5NoteKeeperSolution.Data;
using HW5NoteKeeperSolution.Models;

namespace HW5NoteKeeperSolution.Pages.Notes
{
    public class DeleteModel : PageModel
    {
        private readonly HW5NoteKeeperSolution.Data.NoteKeeperContext _context;

        public DeleteModel(HW5NoteKeeperSolution.Data.NoteKeeperContext context)
        {
            _context = context;
        }

        [BindProperty]
        public Note Note { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var note = await _context.Notes.FirstOrDefaultAsync(m => m.Id == id);

            if (note is not null)
            {
                Note = note;

                return Page();
            }

            return NotFound();
        }

        public async Task<IActionResult> OnPostAsync(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var note = await _context.Notes.FindAsync(id);
            if (note != null)
            {
                Note = note;
                _context.Notes.Remove(Note);
                await _context.SaveChangesAsync();
            }

            return RedirectToPage("./Index");
        }
    }
}
