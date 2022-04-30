using Apotheca.Web.Api.Controllers;
using Apotheca.Web.Api.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace Apotheca.Web.Api.Tests.Controllers
{
    [TestFixture]
    public class ApothecaBaseControllerTests
    {
        #region IsCurrentUser Tests

        [Test]
        public void IsCurrentUser_NoPrincipal_ReturnsFalse()
        {
            ApothecaBaseController controller = new ApothecaBaseController();
            Assert.IsFalse(controller.IsCurrentUser);
        }

        [Test]
        public void IsCurrentUser_PrincipalWithNoClaims_ReturnsFalse()
        {
            ApothecaBaseController controller = new ApothecaBaseController();
            var user = new ClaimsPrincipal(new ClaimsIdentity());
            controller.ControllerContext = new ControllerContext()
            {
                HttpContext = new DefaultHttpContext() { User = user }
            };
            Assert.IsFalse(controller.IsCurrentUser);
        }

        [Test]
        public void IsCurrentUser_PrincipalWithClaims_ReturnsTrue()
        {
            ApothecaBaseController controller = CreateController();
            Assert.IsTrue(controller.IsCurrentUser);
        }

        #endregion

        #region CurrentUser Tests

        [Test]
        public void CurrentUser_NotLoggedIn_ThrowsNullReferenceException()
        {
            ApothecaBaseController controller = new ApothecaBaseController();
            Assert.Throws<NullReferenceException>(() => { var user = controller.CurrentUser; });
        }

        [Test]
        public void CurrentUser_LoggedIn_SetsValues()
        {
            string userId = Guid.NewGuid().ToString();
            string userName = Guid.NewGuid().ToString();
            string email = Guid.NewGuid().ToString();

            ApothecaBaseController controller = CreateController(userId, userName, email);

            UserInfo userInfo = controller.CurrentUser;
            Assert.AreEqual(userId, userInfo.AuthId);
            Assert.AreEqual(userName, userInfo.Name);
            Assert.AreEqual(email, userInfo.Email);
        }

        #endregion

        #region Private Methods

        private ApothecaBaseController CreateController(string userId = "11111", string userName = "TestUser", string email = "test@apotheca.com")
        {
            var controller = new ApothecaBaseController();

            var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
            {
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim(ClaimTypes.Name, userName),
                new Claim(ClaimTypes.Email, email),
            }, "mock"));

            controller.ControllerContext = new ControllerContext()
            {
                HttpContext = new DefaultHttpContext() { User = user }
            };

            return controller;
        }

        #endregion
    }
}
