using System.Net;
using iOSBot.Data;
using iOSBot.Service;
using NSubstitute;
using NUnit.Framework;
using RestSharp;

namespace iOSBot.Tests
{
    [TestFixture]
    public class AppleServiceTests
    {
        private IAppleService _appleService;

        [SetUp]
        public void SetUp()
        {
            _appleService = new AppleService();
        }

        [Test]
        public void GetUpdateForDevice_GM()
        {
            var data = File.ReadAllText(".\\Responses\\iOS17.0GM");
            var mockedResponse = new RestResponse()
            {
                StatusCode = HttpStatusCode.OK,
                Content = data
            };

            var mockRest = Substitute.For<IRestClient>();
            mockRest.When(x => x.PostAsync(Arg.Any<RestRequest>()))
                .Returns(mockedResponse);

            var update = _appleService.GetUpdate(new Device()
            {
                AssetType = "com.apple.MobileAsset.SoftwareUpdate",
                AudienceId = "a6050bca-50d8-4e45-adc2-f7333396a42c",
                FriendlyName = "iOS 17 Developer Beta",
                BoardId = "D74AP",
                BuildId = "20G75",
                Product = "iPhone15,3",
                Version = "16.6"
            });

            Assert.AreEqual("17.0 Golden Master", update.Result.VersionReadable);
            Assert.IsNotEmpty(data);
        }
    }
}
