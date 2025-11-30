using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Net.Http;
using System.Runtime.Caching;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using static System.Net.WebRequestMethods;

namespace ProxyCacheServer
{
    public class ProxyCacheService : IProxyCacheService
    {

        private static MemoryCache _cache = MemoryCache.Default;
        private static readonly HttpClient httpClient = new HttpClient();
        private const string JCDecauxApiKey = "5ea035e1b98780b596860a0e2c1f360bf3d4de8b";
        private const string ORS_API_KEY = "eyJvcmciOiI1YjNjZTM1OTc4NTExMTAwMDFjZjYyNDgiLCJpZCI6ImUxYzlhZjQyMWI0YzRlYTY5NjQ3YmNhODgyN2NkM2I3IiwiaCI6Im11cm11cjY0In0="; //clé api ORS

        public string GetAllStations()
        {
            string cacheKey = "all-stations";
            string cachedResponse = _cache.Get(cacheKey) as string;

            if (cachedResponse != null)
            {
                Console.WriteLine("Toutes les stations trouvées en cache.");
                return cachedResponse;
            }
            else
            {
                Console.WriteLine("Toutes les stations non trouvées en cache. Appel à JCDecaux API");
                string apiResponse = CallAllStationsApi().Result;

                if (!string.IsNullOrEmpty(apiResponse)) //Mise en cache
                {
                    CacheItemPolicy policy = new CacheItemPolicy
                    {
                        AbsoluteExpiration = DateTimeOffset.Now.AddHours(1)
                    };
                    _cache.Set(cacheKey, apiResponse, policy);
                }
                return apiResponse;
            }
        }
        

        public MyGeoCoordinate GetCoordinates(string address)
        {
            string cacheKey = "geocode-" + address.ToLower();
            MyGeoCoordinate cachedCoord = _cache.Get(cacheKey) as MyGeoCoordinate;
            if (cachedCoord != null)
            {
                Console.WriteLine($"Coordonnées pour {address} trouvées en cache.\n");
                return cachedCoord;
            }
            else
            {
                Console.WriteLine($"Coordonnées pour {address} non trouvées en cache. Appel à ORS.\n");
                MyGeoCoordinate result = CallGeocodeApi(address).Result;

                if (result != null)
                {
                    // On met en cache pour 1 heure
                    CacheItemPolicy policy = new CacheItemPolicy { AbsoluteExpiration = DateTimeOffset.Now.AddHours(1) };
                    _cache.Set(cacheKey, result, policy);
                }
                return result;
            }
        }


        private async Task<MyGeoCoordinate> CallGeocodeApi(string address)
        {
            try
            {
                string formattedAddress = Uri.EscapeDataString(address);
                string url = $"https://nominatim.openstreetmap.org/search?q={formattedAddress}&format=json&limit=1";

                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.Add("User-Agent", "Younes-H");

                HttpResponseMessage response = await httpClient.SendAsync(request);
                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"ERREUR CallGeocodeApi: {response.StatusCode}");
                    return null;
                }

                string jsonResponse = await response.Content.ReadAsStringAsync();

                using (JsonDocument doc = JsonDocument.Parse(jsonResponse))
                {
                    JsonElement root = doc.RootElement;

                    if (root.GetArrayLength() == 0)
                    {
                        Console.WriteLine($"ERREUR CallGeocodeApi: Nominatim n'a pas trouvé d'adresse pour '{address}'");
                        return null;
                    }

                    JsonElement firstResult = root[0];

                    double lat = double.Parse(firstResult.GetProperty("lat").GetString(), System.Globalization.CultureInfo.InvariantCulture);
                    double lon = double.Parse(firstResult.GetProperty("lon").GetString(), System.Globalization.CultureInfo.InvariantCulture);

                    return new MyGeoCoordinate { Latitude = lat, Longitude = lon};
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERREUR DANS CallGeocodeApi: {ex.Message}");
                return null;
            }
        }

        public RouteInfo GetRouteInfo(MyGeoCoordinate start, MyGeoCoordinate end, string profile)
        {

            string cacheKey = $"route-{profile}-{start.Latitude},{start.Longitude}-to-{end.Latitude},{end.Longitude}";
            RouteInfo cachedRoute = _cache.Get(cacheKey) as RouteInfo;

            if (cachedRoute != null)
            {
                Console.WriteLine($"Infos de la route trouvées en cache.\n");
                return cachedRoute;
            }
            else
            {
                Console.WriteLine($"Infos de la route non trouvées en cache. Appel à ORS.\n");
                RouteInfo result = CallRouteApi(start, end, profile).Result;

                if (result != null)
                {
                    // On met en cache pour 1 heure
                    CacheItemPolicy policy = new CacheItemPolicy { AbsoluteExpiration = DateTimeOffset.Now.AddHours(1) };
                    _cache.Set(cacheKey, result, policy);
                }
                return result;
            }
        }

        private async Task<RouteInfo> CallRouteApi(MyGeoCoordinate start, MyGeoCoordinate end, string profile)
        {
            try
            {
                string startCoord = $"{start.Longitude.ToString(System.Globalization.CultureInfo.InvariantCulture)},{start.Latitude.ToString(System.Globalization.CultureInfo.InvariantCulture)}";
                string endCoords = $"{end.Longitude.ToString(System.Globalization.CultureInfo.InvariantCulture)},{end.Latitude.ToString(System.Globalization.CultureInfo.InvariantCulture)}";

                string url = $"https://api.openrouteservice.org/v2/directions/{profile}";

                //Body
                var payload = new
                {
                    coordinates = new[]
                    {
                        new[] {start.Longitude, start.Latitude},
                        new[] { end.Longitude, end.Latitude}
                    },
                    language = "fr",
                    instructions = true,
                    units = "m",
                    geometry = true
                };

                string jsonPayload = JsonSerializer.Serialize(payload);
                StringContent content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, url);
                request.Content = content;

                //Header
                request.Headers.TryAddWithoutValidation("Authorization", ORS_API_KEY); 
                request.Headers.UserAgent.ParseAdd("Younes-H");

                HttpResponseMessage response = await httpClient.SendAsync(request);

                if(!response.IsSuccessStatusCode)
                {
                    string errorBody = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"ERREUR POST CallRouteApi ({response.StatusCode}): {errorBody}");
                    return null;
                }

                string jsonResponse = await response.Content.ReadAsStringAsync();
                Console.WriteLine(jsonResponse);

                using (JsonDocument doc = JsonDocument.Parse(jsonResponse))
                {
                    JsonElement root = doc.RootElement;
                    JsonElement features = root.GetProperty("routes");
                    if (features.GetArrayLength() == 0) return null;

                    JsonElement firstRoute = features[0];
                    JsonElement summary = firstRoute.GetProperty("summary");

                    JsonElement geometryElement = firstRoute.GetProperty("geometry");
                    string geometry = geometryElement.ToString();
                    double duration = summary.GetProperty("duration").GetDouble();
                    double distance = summary.GetProperty("distance").GetDouble();

                    List<RouteInstruction> instructions = new List<RouteInstruction>();
                    if (firstRoute.TryGetProperty("segments", out JsonElement segments))
                    {
                        foreach (JsonElement segment in segments.EnumerateArray())
                        {
                            if (segment.TryGetProperty("steps", out JsonElement steps))
                            {
                                foreach (JsonElement step in steps.EnumerateArray())
                                {
                                    instructions.Add(new RouteInstruction
                                    {
                                        Description = step.GetProperty("instruction").GetString(),
                                        Distance = step.GetProperty("distance").GetDouble(),
                                        Type = step.GetProperty("type").GetInt32()
                                    });
                                }
                            }
                        }
                    }

                    return new RouteInfo
                    {
                        Duration = duration,
                        Distance = distance,
                        Geometry = geometry,
                        Profile = profile,
                        Instructions = instructions.ToArray()
                    };
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur dans CallRouteApi : {ex.Message}");
                return null;
            }
        }
        private async Task<string> CallAllStationsApi()
        {
            try
            {
                string url = $"https://api.jcdecaux.com/vls/v1/stations?apiKey={JCDecauxApiKey}";

                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, url);
                HttpResponseMessage response = await httpClient.SendAsync(request);

                if (!response.IsSuccessStatusCode) return null;
                return await response.Content.ReadAsStringAsync();

            }
            catch (Exception e) 
            {
                Console.WriteLine($"Erreur dans CallAllStationsApi : {e.Message}");
                return null; 
            }
        }
    }
}
