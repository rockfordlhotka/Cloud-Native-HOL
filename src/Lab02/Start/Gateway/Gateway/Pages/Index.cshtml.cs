using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;

namespace Gateway.Pages
{
    public class IndexModel : PageModel
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Minor Code Smell", "S1450:Private fields only used as local variables in methods should become local variables", Justification = "<Pending>")]
        private readonly ILogger<IndexModel> _logger;

        public IndexModel(ILogger<IndexModel> logger)
        {
            _logger = logger;
        }

        public void OnGet()
        {

        }
    }
}
