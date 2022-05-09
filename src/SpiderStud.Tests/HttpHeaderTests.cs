using System.Diagnostics;
using Xunit;
using System.Text;
using SpiderStud.Http;

namespace SpiderStud.Tests
{
    public class HttpHeaderTests
    {
        [Fact]
        public void ShouldReturnNullForEmptyBytes()
        {
            HttpRequest request = HttpHeader.Parse(new byte[0]);

            Assert.Null(request);
        }

        [Fact]
        public void ShouldReadResourceLine()
        {
            HttpRequest request = HttpHeader.Parse(ValidRequestArray());

            Assert.Equal("GET", request.Method);
            Assert.Equal("/demo", request.Path);
        }

        [Fact]
        public void ShouldReadHeaders()
        {
            HttpRequest request = HttpHeader.Parse(ValidRequestArray());

            Assert.Equal("example.com", request.Headers["Host"]);
            Assert.Equal("Upgrade", request.Headers["Connection"]);
            Assert.Equal("12998 5 Y3 1  .P00", request.Headers["Sec-WebSocket-Key2"]);
            Assert.Equal("http://example.com", request.Headers["Origin"]);
        }

        [Fact]
        public void ValidRequestShouldNotBeNull()
        {
            Assert.NotNull(HttpHeader.Parse(ValidRequestArray()));
        }

        [Fact]
        public void NoBodyRequestShouldNotBeNull()
        {
            const string noBodyRequest =
                "GET /demo HTTP/1.1\r\n" +
                "Host: example.com\r\n" +
                "Connection: Upgrade\r\n" +
                "Sec-WebSocket-Key2: 12998 5 Y3 1  .P00\r\n" +
                "Sec-WebSocket-Protocol: sample\r\n" +
                "Upgrade: WebSocket\r\n" +
                "Sec-WebSocket-Key1: 4 @1  46546xW%0l 1 5\r\n" +
                "Origin: http://example.com\r\n" +
                "\r\n" +
                "";
            var bytes = RequestArray(noBodyRequest);

            Assert.NotNull(HttpHeader.Parse(bytes));
        }

        [Fact]
        public void NoHeadersRequestShouldBeNull()
        {
            const string noHeadersNoBodyRequest =
                "GET /zing HTTP/1.1\r\n" +
                "\r\n" +
                "";
            var bytes = RequestArray(noHeadersNoBodyRequest);

            Assert.Null(HttpHeader.Parse(bytes));
        }

        [Fact]
        public void HeadersShouldBeCaseInsensitive()
        {
            HttpRequest request = HttpHeader.Parse(ValidRequestArray());

            Assert.True(request.Headers.ContainsKey("Sec-WebSocket-Protocol"));
            Assert.True(request.Headers.ContainsKey("sec-websocket-protocol"));
            Assert.True(request.Headers.ContainsKey("sec-WEBsocket-protoCOL"));
            Assert.True(request.Headers.ContainsKey("UPGRADE"));
            Assert.True(request.Headers.ContainsKey("CONNectiON"));
        }

        [Fact]
        public void PartialHeaderRequestShouldNotBeIncluded()
        {
            const string partialHeaderRequest =
                "GET /demo HTTP/1.1\r\n" +
                "Host: example.com\r\n" +
                "Connection: Upgrade\r\n" +
                "Sec-WebSocket-Key2: 12998 5 Y3 1  .P00\r\n" +
                "Sec-WebSocket-Protocol: sample\r\n" +
                "Upgrade: WebSocket\r\n" +
                "Sec-WebSoc"; //Cut off
            var bytes = RequestArray(partialHeaderRequest);
            var request = HttpHeader.Parse(bytes);
            Assert.NotNull(request);
            Assert.True(request.Headers.ContainsKey("Upgrade"));
            Assert.False(request.Headers.ContainsKey("Sec-WebSoc"));
        }

        [Fact]
        public void EmptyHeaderValuesShouldParse()
        {
            const string emptyCookieRequest =
                "GET /demo HTTP/1.1\r\n" +
                "Host: example.com\r\n" +
                "Connection: Upgrade\r\n" +
                "Sec-WebSocket-Key2: 12998 5 Y3 1  .P00\r\n" +
                "Sec-WebSocket-Protocol: sample\r\n" +
                "Upgrade: WebSocket\r\n" +
                "Sec-WebSocket-Key1: 4 @1  46546xW%0l 1 5\r\n" +
                "Origin: http://example.com\r\n" +
                "Cookie: \r\n" +
                "User-Agent:\r\n" +     //no space after colon
                "\r\n" +
                "^n:ds[4U";
            var bytes = RequestArray(emptyCookieRequest);
            var request = HttpHeader.Parse(bytes);
            Assert.NotNull(request);
            Assert.Equal("", request.Headers["Cookie"]);
        }

        [Fact]
        public void RunTimeOfParseRequestWithLargeCookie()
        {
            var watch = new Stopwatch();

            for (var i = 0; i < 100; i++)
            {
                var bytes = RequestArray(requestWithLargeCookie);
                watch.Start();
                var parsed = HttpHeader.Parse(bytes);
                watch.Stop();

                Assert.NotNull(parsed);
                Assert.Equal(11, parsed.Headers.Count);
            }

            Assert.InRange(watch.Elapsed.TotalSeconds, 0, 50.0 / 1000);
        }

        public byte[] ValidRequestArray()
        {
            return RequestArray(validRequest);
        }

        public byte[] RequestArray(string request)
        {
            return Encoding.UTF8.GetBytes(request);
        }

        const string validRequest =
            "GET /demo HTTP/1.1\r\n" +
            "Host: example.com\r\n" +
            "Connection: Upgrade\r\n" +
            "Sec-WebSocket-Key2: 12998 5 Y3 1  .P00\r\n" +
            "Sec-WebSocket-Protocol: sample\r\n" +
            "Upgrade: WebSocket\r\n" +
            "Sec-WebSocket-Key1: 4 @1  46546xW%0l 1 5\r\n" +
            "Origin: http://example.com\r\n" +
            "\r\n" +
            "^n:ds[4U";

        private const string requestWithLargeCookie =
            "GET / HTTP/1.1\r\n" +
            "Host: 192.168.1.1:8181\r\n" +
            "Connection: Upgrade\r\n" +
            "Pragma: no-cache\r\n" +
            "Cache-Control: no-cache\r\n" +
            "Upgrade: websocket\r\n" +
            "Origin: http://192.168.1.1:8000\r\n" +
            "Sec-WebSocket-Version: 13\r\n" +
            "User-Agent: Mozilla/5.0 (Windows NT 10.0; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/62.0.3202.94 Safari/537.36\r\n" +
            "Accept-Encoding: gzip, deflate\r\n" +
            "Accept-Language: zh-CN,zh;q=0.9\r\n" +
            "Cookie: .AspNetCore.Cookies=chunks-4; .AspNetCore.CookiesC1=CfDJ8FQ5Nbx0MjtOkhtbrGABpUTBCLQzn80-tkmp9tF81sgz7u5dSaYter8FbI-9tYF98S5Fjgdc6liVQduVC0mvN-Po6POE8XKrhoQ-3o014Jz249qeFDOd-7j4PsfOAxWhSwf02DlXkKU6eQCl1qEYuhDZFRTEjXZCiIl9k64bY2nMmeGSPvO7_mNKjBVGxINT94CDFI8Xtb26QHIEKSIhFM1z8JCcs_6HTJ0ASCvrDoVI6nMmb1goaftihsQYhTaocNA9f0284tJNXqW8VSnBh1NbsRTVNealgmsySooBwmDdpYZpVOVqN4BFX1GN_8veUImdBBvq1P_EGppOsSFrTh2q8npKROpztTxQ234wKSiH-Cs9RT_NS3cnIwYi1u0wSPVUqiDCSnTNYdYwpTXXPdt4H5-HPcB8nUDCgrJyNyq5wnVKCZhkbVEpJ_ZQOiMQ4Bw1h6xfvif2JKgf_0yhww6UTx6hon_6X4jzTNthogMGI9qO66RG9So9Qs6ze8H2SgnU4DxzL_VJh3MEesZGKpjaLC6_tvwqWRmNAcoRkCDV6HgR8oIGp3zdXidzlWDK_arltgN8reuDRbvEhcniLrVY0Z0___aag5joC0_TYsTcT6D1BVyc-XGnjBEvA9cV3m1qHHh83lxNPJTsqlEqbwleFpas0DVU-3VfnMQjsycsvcnQxvujwf2AKvmT_4WIYV7UBmXnrxHrfwje4CMxotwgdCn_HBQlj84GVQNqan0yD4r4oWYWWV-BTewWJL2Atdszh0CATGnpTlkcI_iT15XxbpwWvqnlO9hWAY6W_gJsZP6wI0zFan2NKPs-EoKU9oaY0OaYoGvikpqKRrnCu6gpA8ErAFtm94-fCEa5w6YbOyx1pIGTatzZnwjhGhEG4Gv7vZo77tPPL84tm7PXExURlRNhw--ycsBy4jLPXo6MqbrjW7-kNb67hUA7sDiJAPO4WmnJzBYS1Yl94LJQCOsE3cMnxeXs9koGrIXnmgnRU_vRavl1G8wwmDUa1V8eA1TyWEwuIdZ1v12mBjWuCE8Z_tcStfmHCuacXfkiMK4ciajVvXwqx2mXbkrQu_3qL0-NxnpGHaa7lVBV7xsxPdlnEmJi0paMqrn864shFek4E1f-w83TBL0pviH9EhLo6hdNtdBAzMwJRGOKJ_MHIjjrEExaI4kVhyocHFHX1VGCVCdPMAEQv77pHS902GhK1jqiKUtaLrZUQB1tDPxp6-02Yg4VbswoiCWeNUEyfhir4f4Xk3fh_fEDf6IPcuM39XtEhqfFJCKXASB4XvNlnlBPtlG3XejbybGV_Zlk9bxB9HNgG7Zr4ym5_zddpMod99XV1Iml7WL32n2m5oGzvRPVufaZhEzTxdwsaHzGzMpE6bYiMlOkWDO-vTq7F_Vi-Rk9IaVUiPwS8G6JZ_G0jhqA15fiG1aYNi28pcPNQz3VunV3HsUiZR4q9lsfTFGfHlXq470pg-LkRBPOHjzdsDPW-WnpNGyU4EJmJ9oKWN-TmrYEw3alD9cXv4CI_DIlzy9RPxaUYdm3Vjvgg4W7Phv33eiuPzQ9RjStzkL7jWRppV11GCwgJUaTxyg7wnf62ADz2oS4j7hR-eGdMq5YgKw_h3JQCgQNn1egcAUjNE1aZRxMMafgVbGcMqVZuozE1edXjWxs3C5u8vebXcasxofo_cKMShU72jcxz-YU0QpqnVHxnTUSvQUo09wZ_1FEIzL0pwhDUSyS_ZZX2WM8AoGptguJME4fSR3SBxVFb92VACP2Y3wUxVjw4sUl4hMwmVdCIT-LP4nfAXFLnraOKy9O3Yq_VBHR4iCPtYxEWNn9_iop2O2-ZBr5OHjv2Heemkem9H37-_Vu9qhESJQfW_q4wjCp-gB0BWHMYKBMptK_knrgZSPgZuKgmExryGfqokNYWYrQ-upDszIK87Pi934nwJDhZujWOG34_93j9cOJHkB4p5DHcbyf0R__Aio5Tmd8b7IThNucWzzQCIk4yatXQSs4NCyRaMApGyhCGKySgFZx1BOqCrAO82fnUykcaxWDqRjf-YVVZtkLrmmjjRkiQ_6FPKhBWhzqrXEkuFvBzQYgeOEy2YAVKDsL7947swenld2wLolZBgy2xvIjklM09Ph1PHeekEOmx6xFVUIANOrHSUKAVWy7FMGugbZCHTf1Ay33KXS03SJHz7G5VBbidx65j8cH3mYCMYXb1gqxq_uX8os7guBwoAhcLcmBJyIoR0FKbtZ01eOHskN7kT-Fvp8Ri1QB2kdOKWrQM2D7eqvARLrpUXc6IT6XlcMB0paYB0VwKShMDDiB3dfS2de-sRiXdEjiPo-yMIF4UNGEvWnPZQbtC6im4EhnSLnqMP76umBeLdZdSkQaiXWt0WFcpYhmLbrn0cWnxQriH2e_iidZa9ROwLhumuqd7M62dNyZgQZZB-DLB7xUJ4N_xtYUgLF82ldAOMiYvPZozJrCVZoVaUlr7QKbxS5NuVHdmKY-8fdq1l_4HUPt2EJK8kX2dXxZXqWEOsRQaSQL7lbWnFW0umNyM95UTM6kdgTbBoArzb0tkwtchXLohr2pW57EXVIuTyUMlAjDvVnAzYTTfe_9-cfiC4LBF1tHd2QJYdnQSMVbIBH84s6InbSSvL5E2ByO6e1XHs3uterl4ootastj6esSRU_bN1R1I3QmGqfOjs-41eA225BgSWtGlJg5bkFuwvw5mvjFMt4xmmD9DWB7WW6vkQwEzqj1stH341lKb9tsPD3aKS_yLmGiY6bB0l3me4_xTWgmHPov6xlK4_0RqNuRBnCAoLTYbKsiQ-PRZC75Kvn8s6IoxvtEaqKaIdD93yRadITElS45O8F2rq8hClWdBaIgpgutyqf4jfJSbUmceeq7eJ3UFkRK_m1SnWxcmFDl4Eb2Lb7gm9Y3GP2UanSnYIbjoD3Yp6K0Llu3pNRtLvXlxvov3ksT5S3zVkiaaURZkMnp2kN0fP3lrekCA6obO6i7kzNRlnXRSB3xqCeblmBGwSOa0X4fIC8t5o0_R09q1lbKturrpWiR9jACeuzjjEzm1LwXaIbXCyIC3O3h0NQHEJzsT8xK5ksX1sBbwAQ5O7dpgejFxMU1B1SQ_lJTN63z_ritASK5w4H6IMGmfnuHp8k_Zzt5t4L2FmExeDCvoDSG6bRvsdbmkuk8eO_ecZTGJQHzVxmReo-9uagVY8-rfIkooLtmnrRhFfHpmfXAFvHGWUU0VVtUay__v4rN-dzuL7Esx_OyAGVDwuRqUzPnqXR8u3wdgF1eqKBzkIjBX0r-OmY6L957OcgDiCWBsWQvuBKbya_7UKRMiPgPdkL_b2BtzeVuuFh9JpcjhkoiahMUACxJoZeoBgE5nC8nPuAi2_GN6MrHYJfljaivxP0hSv8SqV4bZT9xlTTePfgFR842xOqCJh0_4xkMe6UCSSJQbS71ELFADiISoZraqeLWe5MIXbNAdSk52gSWwjIgE6b3usRLtxUVZxrO0vBTG0Uw-o_CvLBTyMCeOIVmy3lA-ifyQ7UGeX0i_hRbwcZkfl-Q4uAeqw7Ts2qwEviyCB24d46BiX4hD1jKNH2HEVZyrwHCDkTEW9_8twc2dwl0xl\r\n" +
            "\r\n" +
            "body";
    }
}
