using System.Net;
using System.Text.RegularExpressions;
using CSharpFunctionalExtensions;
using ScrapySharp.Extensions;
using ScrapySharp.Network;

namespace LunchTodayApi.LunchMenuFinder;

public class RestaurantCrawler : IRestaurantCrawler
{
    private readonly ScrapingBrowser _browser = new();
    private static readonly List<string> SwedishWeekDays = new() { "måndag", "tisdag", "onsdag", "torsdag", "fredag" };


    public async Task<Result<RestaurantResult, Exception>> GetWeeklyLunchMenu(string url) =>
        await NavigateToPage(url)
            .Bind(wp => ExtractAllLinks(wp, url))
            .Bind(FindLunchPage)
            .Bind(GetMenusFromPage)
            .Map(x => new RestaurantResult
            {
                LunchMenus = x,
                RestaurantUrl = url
            });

    private Result<List<LunchMenuResult>, Exception> GetMenusFromPage(string pageContent) =>
        ContainsAllWeekDays(pageContent, SwedishWeekDays)
            ? GetLunchesFromPage(pageContent, SwedishWeekDays)
            : new Exception("Could not find all weekdays");

    private Result<List<LunchMenuResult>, Exception> GetLunchesFromPage(string pageContent,
        List<string> swedishWeekDays) =>
        Result.Try<List<LunchMenuResult>, Exception>(() => 
        {
            var results = new List<LunchMenuResult>();
            foreach (var weekDay in swedishWeekDays)
            {
                var nextWeekDay = weekDay == "fredag" ? null : swedishWeekDays[swedishWeekDays.IndexOf(weekDay) + 1];
                int indexOfWeekDay = pageContent.IndexOf(weekDay, StringComparison.InvariantCultureIgnoreCase);
                if (nextWeekDay is not null)
                {
                    var lunchMenuDataResult = Result.Try(() =>
                    {
                        var indexOfNextWeekDay =
                            pageContent.IndexOf(nextWeekDay, StringComparison.InvariantCultureIgnoreCase);
                        return pageContent.Substring(indexOfWeekDay, indexOfNextWeekDay - indexOfWeekDay);
                    });

                    var lunchMenuResult = GetLunchMenuResult(lunchMenuDataResult, weekDay);

                    if (lunchMenuResult.IsSuccess)
                        results.Add(lunchMenuResult.Value);
                }
                else
                {
                    const int takeNoOfRows = 10;
                    var lunchMenuDataResult = Result.Try(() =>
                    {
                        var indexOfLastWeekDay = pageContent.IndexOf(weekDay, StringComparison.InvariantCultureIgnoreCase);
                        var rowsAfterFriday = pageContent.Substring(indexOfLastWeekDay).Split("\n").Take(takeNoOfRows);
                        return string.Join("\n", rowsAfterFriday);
                    });

                    var lunchMenuResult = GetLunchMenuResult(lunchMenuDataResult, weekDay);
                    if (lunchMenuResult.IsSuccess)
                        results.Add(lunchMenuResult.Value);
                }
            }

            return results;
        }, e => e);

    private Result<LunchMenuResult> GetLunchMenuResult(Result<string> lunchMenuDataResult, string weekDay) =>
        lunchMenuDataResult
            .Ensure(s => !string.IsNullOrEmpty(s), "No lunch menu found")
            .Ensure(s => s.Contains(weekDay, StringComparison.InvariantCultureIgnoreCase), "Not weekly menu")
            .MapTry(s => RemoveWeekDay(s, weekDay))
            .MapTry(WebUtility.HtmlDecode)
            .MapTry(s => s.Trim())
            .MapTry(s => s.TrimStart('\n'))
            .MapTry(s => s.Replace("\n", "<br>"))
            .MapTry(RemoveConsecutiveSpaces)
            .Map(s => new LunchMenuResult { Menu = s, Weekday = weekDay });

    private string RemoveConsecutiveSpaces(string lunchMenu)
    {
        lunchMenu = Regex.Replace(lunchMenu, @"\s+", " ");
        return lunchMenu;
    }

    private string RemoveWeekDay(string lunchMenu, string weekDay)
    {
        var lastIndexOfWeekDay = lunchMenu.LastIndexOf(weekDay, StringComparison.InvariantCultureIgnoreCase);
        lunchMenu = lunchMenu[(lastIndexOfWeekDay+weekDay.Length)..];
        //lunchMenu = lunchMenu.Replace(weekDay, "", StringComparison.InvariantCultureIgnoreCase);
        return lunchMenu;
    }

    private static bool ContainsAllWeekDays(string pageContent, List<string> swedishWeekDays) =>
        swedishWeekDays.All(x => pageContent.ToLower().Contains(x));

    private async Task<Result<string, Exception>> FindLunchPage(IEnumerable<Uri> links)
    {
        try
        {
            var lunchPageLink = links.FirstOrDefault(x => x.AbsoluteUri.ToLower().Contains("lunch"));
            if (lunchPageLink == null)
            {
                return new Exception("Could not find any lunch page");
            }

            return await Result.Try<string, Exception>(
                async () => await GetLunchPageText(lunchPageLink), 
                e => e);
        }
        catch (Exception e)
        {
            return e;
        }
    }

    private async Task<string> GetLunchPageText(Uri lunchPageLink)
    {
        var webpage = await _browser.NavigateToPageAsync(lunchPageLink);
        var lunchPageText = webpage.Html.InnerText;
        lunchPageText = lunchPageText.Replace("\r", "");
        lunchPageText = lunchPageText.Replace("\t", "");
        return lunchPageText;
    }

    private Result<IEnumerable<Uri>, Exception> ExtractAllLinks(WebPage webpage, string url) =>
        Result.Try<IEnumerable<Uri>, Exception>(() =>
        {
            var links = webpage.Html.CssSelect("a");
            var lunchLinks = links.Where(x => x.InnerText.ToLower().Contains("lunch"))
                .Select(x => new Uri(new Uri(url), x.Attributes["href"].Value))
                .ToList();
            return lunchLinks;
            
        }, e => e);

    private async Task<Result<WebPage, Exception>> NavigateToPage(string url)
    {
        try
        {
            return await _browser.NavigateToPageAsync(new Uri(url));
        }
        catch (Exception e)
        {
            return e;
        }
    }
}

public interface ILogger<T>
{
    Task Log(string s);
}

public interface IRestaurantCrawler
{
}

public class RestaurantResult
{
    public List<LunchMenuResult> LunchMenus { get; set; }
    public string RestaurantUrl { get; set; }
}

public class LunchMenuResult
{
    public string Menu { get; set; }
    public string Weekday { get; set; }
}