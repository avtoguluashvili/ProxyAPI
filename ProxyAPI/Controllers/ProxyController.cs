using System.Text.RegularExpressions;
using HtmlAgilityPack;
using Microsoft.AspNetCore.Mvc;

namespace RedditProxy.Controllers
{
    [ApiController]
    [Route("{*url}")]
    public class ProxyController : ControllerBase
    {
        private static readonly HttpClient HttpClient = new();

        [HttpGet]
        public async Task<IActionResult> Get(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
            {
                url = "https://www.reddit.com/";
            }
            else if (!url.StartsWith("http"))
            {
                url = $"https://www.reddit.com/{url.TrimStart('/')}";
            }

            var response = await HttpClient.GetAsync(url);
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var modifiedContent = ModifyContent(content);
                return Content(modifiedContent, "text/html");
            }

            return StatusCode((int)response.StatusCode);
        }

        private string ModifyContent(string content)
        {
            var htmlDocument = new HtmlDocument();
            htmlDocument.LoadHtml(content);

            var nodes = htmlDocument.DocumentNode.DescendantsAndSelf().Where(n => n.NodeType == HtmlNodeType.Text);
            foreach (var node in nodes)
            {
                if (node.InnerText.Trim().Length > 0)
                {
                    node.InnerHtml = Regex.Replace(node.InnerHtml, @"\b\w{6}\b", "$&™");
                }
            }

            var links = htmlDocument.DocumentNode.Descendants("a").Where(a => a.Attributes.Contains("href"));
            foreach (var link in links)
            {
                var href = link.Attributes["href"].Value;
                if (href.StartsWith("/"))
                {
                    link.Attributes["href"].Value = $"{Request.Scheme}://{Request.Host}{href}";
                }
            }

            using var stringWriter = new StringWriter();
            htmlDocument.Save(stringWriter);
            return stringWriter.ToString();
        }
    }
}
