using Apotheca.Web.Api.Models;
using Microsoft.AspNetCore.Mvc;

namespace Apotheca.Web.Api.Controllers
{
    public class ApothecaBaseController : ControllerBase
    {
        private UserInfo? _userInfo = null;

        public virtual bool IsCurrentUser
        {
            get
            {
                var claimUserId = this.User?.Claims?.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.NameIdentifier);
                return (claimUserId != null);
            }
        }


        public virtual UserInfo CurrentUser
        {
            get
            {
                if (_userInfo == null)
                {
                    var claimUserId = this.User?.Claims?.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.NameIdentifier);
                    if (claimUserId == null)
                    {
                        throw new NullReferenceException("There is no user logged in - use IsCurrentUser to determine if authorised user exists");
                    }

                    _userInfo = new UserInfo();
                    _userInfo.AuthId = claimUserId.Value;
                    _userInfo.Name = this.User?.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.Name)?.Value;
                    _userInfo.Email = this.User?.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.Email)?.Value;
                }
                return _userInfo;
            }
            set
            {
                _userInfo = value;
            }
        }


    }
}
