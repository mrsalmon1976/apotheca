using Microsoft.AspNetCore.Mvc;

namespace Apotheca.Web.Api.Controllers
{
    public class ApothecaBaseController : ControllerBase
    {
        private string? _userId = null;

        public string? UserId
        {
            get
            {
                if (_userId == null)
                {
                    var claim = this.User?.Claims?.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.NameIdentifier);
                    if (claim != null)
                    {
                        _userId = claim.Value;
                    }
                }
                return _userId;
            }
            set
            {
                _userId = value;
            }
        }
    }
}
