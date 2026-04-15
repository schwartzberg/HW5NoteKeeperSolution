using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using HW5NoteKeeperSolution.Data;
using HW5NoteKeeperSolution.Models;

namespace HW5NoteKeeperSolution.Pages.Notes
{
    public class CreateModel : PageModel
    {
        private readonly HW5NoteKeeperSolution.Data.NoteKeeperContext _context;

        public CreateModel(HW5NoteKeeperSolution.Data.NoteKeeperContext context)
        {
            _context = context;
        }

        public IActionResult OnGet()
        {
            return Page();
        }

        [BindProperty]
        public Note Note { get; set; } = default!;

        // For more information, see https://aka.ms/RazorPagesCRUD.
        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            _context.Notes.Add(Note);
            await _context.SaveChangesAsync();

            return RedirectToPage("./Index");
        }
    }
}
