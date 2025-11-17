using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Net.Http;
using System.Runtime.Caching;
using System.Security.Cryptography;
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

        public string GetStationsForContract(String contract)
        {
            string cacheKey = "stations-" + contract.ToLower();

            string cachedResponse = _cache.Get(cacheKey) as string;

            if (cachedResponse != null)
            {
                Console.WriteLine($"'{contract}' trouvé en cache.");
                return cachedResponse;
            }

            Console.WriteLine($"'{contract}' non trouvé en cache. Appel à l'API JCDecaux.");

            string apiResponse = CallJCDecauxApi(contract).Result;

            if (!string.IsNullOrEmpty(apiResponse))
            {  //Requête réussie --> on met en cache
                CacheItemPolicy policy = new CacheItemPolicy
                {
                    AbsoluteExpiration = DateTimeOffset.Now.AddMinutes(5)
                };
                _cache.Set(cacheKey, apiResponse, policy);
            }

            return apiResponse;
        }

        private async Task<string> CallJCDecauxApi(string contract)
        {
            try
            {
                string url = $"https://api.jcdecaux.com/vls/v1/stations?contract={contract}&apiKey={JCDecauxApiKey}";

                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, url);
                HttpResponseMessage response = await httpClient.SendAsync(request); //attends la réponse pour la retourner

                if (!response.IsSuccessStatusCode) return null;
                return await response.Content.ReadAsStringAsync();
            }
            catch (HttpRequestException e)
            {
                Console.WriteLine("\nException caught.\n");
                Console.WriteLine("Message {0}: ", e.Message);
                return null;
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
                // Ces deux lignes pour avoir des '.' et pas des ',' dans les coord
                string startCoord = $"{start.Longitude.ToString(System.Globalization.CultureInfo.InvariantCulture)},{start.Latitude.ToString(System.Globalization.CultureInfo.InvariantCulture)}";
                string endCoords = $"{end.Longitude.ToString(System.Globalization.CultureInfo.InvariantCulture)},{end.Latitude.ToString(System.Globalization.CultureInfo.InvariantCulture)}";

                string url = $"https://api.openrouteservice.org/v2/directions/{profile}?api_key={ORS_API_KEY}&start={startCoord}&end={endCoords}&geometry_format=polyline";

                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.Add("User-Agent", "Younes-H");

                HttpResponseMessage response = await httpClient.SendAsync(request);
                if (!response.IsSuccessStatusCode) return null;
                string jsonResponse = await response.Content.ReadAsStringAsync();

                using (JsonDocument doc = JsonDocument.Parse(jsonResponse))
                {   
                    JsonElement root = doc.RootElement;
                    JsonElement features = root.GetProperty("features");
                    if (features.GetArrayLength() == 0) return null;

                    JsonElement firstRoute = features[0];
                    JsonElement properties = firstRoute.GetProperty("properties");
                    JsonElement summary = properties.GetProperty("summary");

                    JsonElement geometryElement = firstRoute.GetProperty("geometry");
                    string geometry = geometryElement.ToString();
                    double duration = summary.GetProperty("duration").GetDouble();
                    double distance = summary.GetProperty("distance").GetDouble();

                    return new RouteInfo { Duration = duration, Distance = distance, Geometry = geometry };

                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur dans CallRouteApi : {ex.Message}");
                return null;
            }
        }

        public string GetAllStations()
        {
            string cacheKey = "all-stations";
            string cachedResponse = _cache.Get(cacheKey) as string;
            
            if(cachedResponse != null)
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
        public List<MyContract> GetContracts()
        {
            string cacheKey = "contracts";
            List<MyContract> cachedContracts = _cache.Get(cacheKey) as List<MyContract>;
            if(cachedContracts != null)
            {
                return cachedContracts;
            }

            string url = $"https://api.jcdecaux.com/vls/v3/contracts?apiKey={JCDecauxApiKey}";
            HttpResponseMessage response = httpClient.GetAsync(url).Result;

            if (!response.IsSuccessStatusCode) return null;
            string json = response.Content.ReadAsStringAsync().Result;

            JsonSerializerOptions options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            List<MyContract> contracts = JsonSerializer.Deserialize<List<MyContract>>(json, options);

            _cache.Set(cacheKey, contracts, new CacheItemPolicy { AbsoluteExpiration = DateTimeOffset.Now.AddHours(1) });

            return contracts;
        }

        public string GetCityFromCoordinates(MyGeoCoordinate coord)
        {
            try
            {
                string lat = coord.Latitude.ToString(CultureInfo.InvariantCulture);
                string lon = coord.Longitude.ToString(CultureInfo.InvariantCulture);
                Console.WriteLine($"lon:{coord.Latitude}, lat:{coord.Longitude}");
                string url = $"https://nominatim.openstreetmap.org/reverse?format=jsonv2&lat={lat}&lon={lon}";
                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.Add("User-Agent", "Younes-H");
                HttpResponseMessage response = httpClient.SendAsync(request).Result;

                if (!response.IsSuccessStatusCode) return $"ICI SOAP: erreur HTTP -> Code {response.StatusCode}";

                string json = response.Content.ReadAsStringAsync().Result;
                if (string.IsNullOrEmpty(json)) return "ICI SOAP: réponse vide";

                using (JsonDocument doc = JsonDocument.Parse(json))
                {
                    JsonElement root = doc.RootElement;
                    if (root.TryGetProperty("address", out JsonElement address))
                    {
                        if (address.TryGetProperty("city", out JsonElement cityElem))
                            return cityElem.GetString();
                        if (address.TryGetProperty("town", out cityElem))
                            return cityElem.GetString();
                        if (address.TryGetProperty("village", out cityElem))
                            return cityElem.GetString();
                    }
                }
                return "ICI SOAP: pas de ville trouvée";
            }
            catch (Exception ex)
            {
                return "ICI SOAP: exception attrapée -> " + ex.Message;
            }
        }



    }
}
