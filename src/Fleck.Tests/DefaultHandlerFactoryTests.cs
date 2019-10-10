using NUnit.Framework;

namespace Fleck.Tests
{
    [TestFixture]
    public class DefaultHandlerFactoryTests
    {
        [Test]
        public void ShouldThrowWhenUnsupportedType()
        {
            var request = new WebSocketHttpRequest {Headers = {{"Bad", "Request"}}};
            Assert.Throws<WebSocketException>(() => HandlerFactory.BuildHandler(request, x => {}, () => {}, x => { }, x => { }, x => { }));
        }
    }
}
