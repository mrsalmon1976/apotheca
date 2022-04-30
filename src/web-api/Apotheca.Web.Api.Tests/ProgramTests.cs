using Apotheca.Web.Api.Controllers;
using Apotheca.Web.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Apotheca.Web.Api.Tests
{
    [TestFixture]
    public class ProgramTests
    {

        [Test]
        public void Program_VerifyViewServices()
        {
            var app = CreateApplication();
            var typesToVerify = GetTypesToVerify(typeof(IViewService))
                .Where(x => x.IsInterface)
                .ToList();

            Assert.Greater(typesToVerify.Count, 0);
            VerifyTypes(app, typesToVerify);
        }

        private WebApplicationFactory<Program> CreateApplication()
        {
            var application = new WebApplicationFactory<Program>()
                .WithWebHostBuilder(builder =>
                {
                    // Configure test services
                });
            return application;
        }

        private List<Type> GetTypesToVerify(Type t)
        {
            Assembly assembly = Assembly.GetAssembly(typeof(ApothecaBaseController));
            return assembly.GetTypes()
                .Where(x => x.IsInstanceOfType(t) || x.IsSubclassOf(t) || x.GetInterfaces().Contains(t)).ToList();
        }

        private void VerifyTypes(WebApplicationFactory<Program> application, List<Type> typesToVerify, IEnumerable<Type> ignoreTypes = null)
        {
            foreach (Type t in typesToVerify)
            {
                if (ignoreTypes != null && ignoreTypes.Any() && ignoreTypes.Contains(t))
                {
                    continue;
                }
                var service = application.Services.GetService(t);
                Assert.IsNotNull(service, $"Expected service of type '{t.FullName}' was not instantiated.");
            }
        }
    }
}
