using Assignment;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AssignmentTest
{
    public class WoonObjectBronTest
    {
        private Mock<IApiClient> fakeApiClient;

        [SetUp]
        public void Setup()
        {
            fakeApiClient = new Mock<IApiClient>();
        }

        [Test]
        public void HaalPagina_WhenPaginaInfoNull_ThrowsUnexpectedApiResponseException()
        {
            // Arrange
            fakeApiClient.Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(@"{
                        ""Objects"": []
                    }")
                }));
            var woonObjectBron = new WoonObjectBron("/amsterdam/", fakeApiClient.Object);

            Assert.That(
                // Act
                async () => await woonObjectBron.HaalPagina(1, default),
                // Assert
                Throws.TypeOf<UnexpectedApiResponseException>()
            );
        }
    }
}
