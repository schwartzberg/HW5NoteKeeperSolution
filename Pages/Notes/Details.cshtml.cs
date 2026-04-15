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
    public class DetailsModel : PageModel
    {
        private readonly HW5NoteKeeperSolution.Data.NoteKeeperContext _context;

        public DetailsModel(HW5NoteKeeperSolution.Data.NoteKeeperContext context)
        {
            _context = context;
        }

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
    }
}
