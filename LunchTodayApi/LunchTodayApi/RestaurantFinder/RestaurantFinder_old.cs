using System.Text.Json;
using System.Text.Json.Nodes;
using GoogleApi;
using GoogleApi.Entities.Common;
using GoogleApi.Entities.Places.Common.Enums;
using GoogleApi.Entities.Places.Details.Request;
using GoogleApi.Entities.Places.Search.Common.Enums;
using GoogleApi.Entities.Places.Search.NearBy.Request;
using GoogleApi.Entities.Places.Search.Text.Request;

namespace LunchTodayApi.RestaurantFinders;

public class RestaurantFinder_old
{
    private const string ApiKey = "AIzaSyBRGE5Z6mwQW96FeJ8SGERdb7PtauBlJeo";
    private readonly GooglePlaces.Search.TextSearchApi _textSearchApi;
    private readonly GooglePlaces.Search.NearBySearchApi _nearBySearchApi;
    private readonly GooglePlaces.DetailsApi _detailsApi;

    public RestaurantFinder_old(GooglePlaces.Search.TextSearchApi textSearchApi,
        GooglePlaces.Search.NearBySearchApi nearBySearchApi,
        GooglePlaces.DetailsApi detailsApi)
    {
        _textSearchApi = textSearchApi;
        _nearBySearchApi = nearBySearchApi;
        _detailsApi = detailsApi;
    }
    
    public async Task<IEnumerable<Uri>> GetAllRestaurantUrls()
    {
        var cities = await GetAllCities();
        var urls = new List<Uri>();
        foreach (var city in cities)
        {
            var restaurantUrls = await GetAllRestaurants(city);
            urls.AddRange(restaurantUrls.Select(restaurantUrl => new Uri(restaurantUrl)));
        }
        return urls;
        
     
        
        
        // urls.Add(new Uri("https://www.hotellsolliden.se/"));
        // urls.Add(new Uri("https://www.bistrobeo.se/lunch/"));
        // urls.Add(new Uri("https://www.stenungealle.se/lunchmeny/"));
        // urls.Add(new Uri("https://restaurangtjornbron.se/dagens-lunch/"));
        // urls.Add(new Uri("https://jordhammarsherrgard.se/dagens-lunch/"));
        //urls.Add(new Uri("https://restauranghimalaya.com/"));
        // urls.Add(new Uri("http://www.vallagat.se/"));
        // urls.Add(new Uri("https://www.kungstorget.com/"));
        // urls.Add(new Uri("https://www.kallarkrogen.net/"));
        // urls.Add(new Uri("https://bastardburgers.com/se/"));
        // urls.Add(new Uri("https://www.restaurangkometen.se/"));
        // urls.Add(new Uri("https://www.hotelbellora.se/"));
        // urls.Add(new Uri("https://www.kgkallare.se/"));
        // urls.Add(new Uri("https://restaurangfelicia.se/"));
        //return urls;
    }
    
    private static readonly HttpClient _httpClient = new HttpClient();
    

    public async Task<string[]> GetRestaurantHomePages()
    {
        var restaurants = await GetRestaurantsFromGooglePlaces();
        var homePages = restaurants
            .Select(restaurant => restaurant.Website)
            .Where(url => !string.IsNullOrEmpty(url))
            .ToArray();

        return homePages;
    }

    public async Task<List<Coordinate>> GetAllCities()
    {
        var cityList = new List<Coordinate>();
        
        var cityResponse = await _textSearchApi.QueryAsync(new PlacesTextSearchRequest()
        {
            Key = ApiKey,
            Query = "cities in Sweden"
        });
        
        var cities= cityResponse.Results.Select(result => result.Geometry.Location).ToList();
        
        cityList.AddRange(cities);

        // while (cityResponse.NextPageToken != null)
        // {
        //     var nextPageToken = cityResponse.NextPageToken;
        //     cityResponse = await _textSearchApi.QueryAsync(new PlacesTextSearchRequest()
        //     {
        //         Key = ApiKey,
        //         PageToken = nextPageToken
        //     });
        //     cities= cityResponse.Results.Select(result => result.Geometry.Location).ToList();
        //     cityList.AddRange(cities);
        // }
        


        return cityList;
    }
    
    public async Task<List<string>> GetAllRestaurants(Coordinate location)
    {
        try
        {
            var restaurants = new List<string>();
            var websites = new List<string>();
        
            var searchResponse = await _nearBySearchApi.QueryAsync(new PlacesNearBySearchRequest()
            {
                Key = ApiKey,
                Location = location,
                Radius = 5000,
                Type = SearchPlaceType.Restaurant
            });
        
            var cities= searchResponse.Results.Select(result => result.PlaceId).ToList();
        
            restaurants.AddRange(cities);

            while (searchResponse.NextPageToken != null)
            {
                var nextPageToken = searchResponse.NextPageToken;
                searchResponse = await _nearBySearchApi.QueryAsync(new PlacesNearBySearchRequest()
                {
                    Key = ApiKey,
                    PageToken = nextPageToken
                });
                cities= searchResponse.Results.Select(result => result.PlaceId).ToList();
                restaurants.AddRange(cities);
            }

            foreach (var restaurant in restaurants)
            {
                var detailsResponse = await _detailsApi.QueryAsync(new PlacesDetailsRequest()
                {
                    Key = ApiKey,
                    Fields = FieldTypes.Website,
                    PlaceId = restaurant
                });
                var website = detailsResponse.Result.Website;
                websites.Add(website);
            }
            
            

            return websites;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
       
    }
    

    private async Task<List<Restaurant>> GetRestaurantsFromGooglePlaces()
    {
        var cityResponse = await _textSearchApi.QueryAsync(new PlacesTextSearchRequest()
        {
            Key = ApiKey,
            Query = "cities in Sweden"
        });
        
        var cities= cityResponse.Results.Select(result => result.PlaceId).ToList();
        foreach (var city in cities)
        {
            var g = GetRestaurantHomepage(city);
        }
        

        return null;

        // return restaurants;
        // var baseUrl = "https://maps.googleapis.com/maps/api/place/nearbysearch/json";
        // var location = "59.3293,18.0686"; // Coordinates for Stockholm, Sweden
        // var radius = 5000; // Search radius in meters
        // var type = "restaurant"; // Filter by restaurant type
        //
        // var requestUrl = $"{baseUrl}?location={location}&radius={radius}&type={type}&key={ApiKey}";
        //
        // var response = await _httpClient.GetAsync(requestUrl);
        // if (!response.IsSuccessStatusCode)
        // {
        //     throw new Exception("Failed to retrieve restaurants from Google Places API.");
        // }
        //
        // var responseDataString = await response.Content.ReadAsStringAsync();
        // var responseData = JsonSerializer.Deserialize<Model1.RootObject>(responseDataString);
        //
        //
        // List<Restaurant> restaurants = new List<Restaurant>();
        // foreach (var result in responseData.results)
        // {
        //     var website = await GetRestaurantHomepage(result.place_id);
        //     if (website != null)
        //     {
        //         var restaurant = new Restaurant()
        //         {
        //             Name = result.name,
        //             Website = website
        //         };
        //         restaurants.Add(restaurant);
        //     }
        // }
        //
        // return restaurants;
    }
    
    static async Task<string> GetRestaurantHomepage(string placeId)
    {
        string apiKey = ApiKey;
        string apiUrl = $"https://maps.googleapis.com/maps/api/place/details/json?place_id={placeId}&key={apiKey}";
    
        using HttpClient client = new HttpClient();
        HttpResponseMessage response = await client.GetAsync(apiUrl);
    
        if (response.IsSuccessStatusCode)
        {
            string json = await response.Content.ReadAsStringAsync();
            // Parse the JSON response to retrieve the homepage URL
            // Replace this with your own JSON parsing logic
            // Here's an example assuming a "website" field in the response:
            var responseData = JsonObject.Parse(json);
            string homepageUrl = responseData["result"]["website"].ToString();
            homepageUrl = GetBaseUrl(homepageUrl);
            return homepageUrl;
        }
        else
        {
            Console.WriteLine("Failed to retrieve restaurant details.");
            return null;
        }
    }
    
    private static string GetBaseUrl(string url)
    {
        if (url == null)
        {
            return null;
        }

        var uri = new Uri(url);
        var baseUrl = $"{uri.Scheme}://{uri.Host}";
        return baseUrl;
    }
   
}
public class Restaurant
{
    public string Name { get; set; }
    public string Website { get; set; }
}
    