using Microsoft.AspNetCore.Mvc.RazorPages;
using SharedKernel.Data;
using SharedKernel.Domain;

namespace DataImportAgent.Pages
{
    public class IndexModel : PageModel
    {
        private readonly VDMAdminDbContext _db;

        public IndexModel(VDMAdminDbContext db)
        {
            _db = db;
        }

        public List<FileHistory> FileHistoryList { get; set; }

        public void OnGet()
        {
            FileHistoryList = _db.FileHistories
                .OrderByDescending(fh => fh.AccessDate)
                .Take(50)
                .ToList();
        }
    }
}