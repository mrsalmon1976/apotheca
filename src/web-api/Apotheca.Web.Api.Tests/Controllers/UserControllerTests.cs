using Apotheca.Web.Api.Controllers;
using Microsoft.AspNetCore.Mvc.Testing;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Apotheca.Web.Api.Tests.Controllers
{
    [TestFixture]
    public class UserControllerTests
    {
        [Test]
        public void GetCurrentUserDocumentStores_NotLoggedIn_ReturnsUnauthorizedResult()
        {
            // Setup
            var client = CreateApplicationHttpClient();

            // Execute
            var response = client.GetAsync(UserController.UrlGetCurrentUserDocumentStores).Result;

            // Assert
            Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
            Assert.IsFalse(response.IsSuccessStatusCode);
        }

        private HttpClient CreateApplicationHttpClient()
        {
            var application = new WebApplicationFactory<Program>()
                    .WithWebHostBuilder(builder =>
                    {
                        // ... Configure test services
                    });
            var client = application.CreateClient();
            return client;
        }
    }
}
