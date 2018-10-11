using System;
using System.Threading.Tasks;
using App.Network;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace NetworkExampleTests
{
    [TestClass]
    public class UnitTest
    {
        [TestMethod]
        public async Task MockAuthenticationTestAsync()
        {
            try
            {
                var session = await Authentication.MockAuthenticationAsync();
                Assert.IsNotNull(session);
            }
            catch (Exception ex)
            {
                Assert.Fail(ex.Message);
            }
        }

        [TestMethod]
        public async Task PerformMutableDataOptionsTestAsync()
        {
            try
            {
                var session = await Authentication.MockAuthenticationAsync();
                Assert.IsNotNull(session);

                var mdOperations = new MutableDataOperations(session);
                await mdOperations.PerformMDataOperations();
            }
            catch (Exception ex)
            {
                Assert.Fail(ex.Message);
            }
        }
    }
}
