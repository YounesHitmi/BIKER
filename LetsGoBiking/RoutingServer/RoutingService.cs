using RoutingServer.ProxyCacheReference; //SOAP
using System;
using System.Collections.Generic;
using System.Device.Location; //GetDistance()
using System.Linq; //Where() et OrderBy()
using System.Net.Http;
using System.Net.NetworkInformation;
using System.ServiceModel;
using System.ServiceModel.Web;
using System.Text.Json; //Deserialisation
using System.Threading.Tasks;

namespace RoutingServer
{
    public class RoutingService : IRoutingService
    {
        public ItineraryResponse FindItinerary(string origin, string destination)
        {
            ItineraryResponse response = new ItineraryResponse();

            try
            {
                WSHttpBinding binding = new WSHttpBinding();
                binding.MaxReceivedMessageSize = 2147483647;
                binding.ReaderQuotas.MaxStringContentLength = 2147483647;
                EndpointAddress endpoint = new EndpointAddress("http://localhost:8733/ProxyCacheServ/");
                ProxyCacheServiceClient proxyClient = new ProxyCacheServiceClient(binding, endpoint);

                MyGeoCoordinate originCoord = proxyClient.GetCoordinates(origin);
                if (originCoord == null)
                {
                    response.Status = "ERREUR";
                    response.Message = "Impossible de trouver l'adresse d'origine ou la ville associée.";
                    return response;
                }

                MyGeoCoordinate destCoord = proxyClient.GetCoordinates(destination);
                if (destCoord == null)
                {
                    response.Status = "ERREUR";
                    response.Message = "Impossible de trouver l'adresse de destination ou la ville associée.";
                    return response;
                }

                List<MyContract> contracts = proxyClient.GetContracts().ToList();

                List<string> villes =
                contracts
                  .Where(c => c.cities != null)
                  .SelectMany(c => c.cities)
                  .Distinct()
                  .ToList();

                string userCity = proxyClient.GetCityFromCoordinates(originCoord);
                /*if (!villes.Contains(userCity)) {
                    response.Status = "ERREUR";
                    response.Message = "Votre ville n'est pas couverte par le service JCDecaux.";

                }*/
                if (string.IsNullOrEmpty(userCity))
                {
                    response.Status = "ERREUR";
                    response.Message = "Impossible de récupérer la ville de départ.";
                    return response;
                }

                string userContractName = contracts
                    .FirstOrDefault(c => c.name.Equals(userCity, StringComparison.OrdinalIgnoreCase) ||
                                         c.commercial_name != null && c.commercial_name.Equals(userCity, StringComparison.OrdinalIgnoreCase))
                    ?.name;
                if (string.IsNullOrEmpty(userContractName))
                {
                    userContractName = contracts
                        .FirstOrDefault(c => c.cities != null && c.cities.Any(v => v.Equals(userCity, StringComparison.OrdinalIgnoreCase)))
                        ?.name;
                }

                string allStationsJson = proxyClient.GetAllStations();
                JsonSerializerOptions options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                List<Station> allStations = JsonSerializer.Deserialize<List<Station>>(allStationsJson, options);

                //Conversion des objets MyGeoCoordinate en GeoCoordinate (pour calculs de distances)
                GeoCoordinate originGeo = new GeoCoordinate(originCoord.Latitude, originCoord.Longitude);
                GeoCoordinate destGeo = new GeoCoordinate(destCoord.Latitude, destCoord.Longitude);

                Station bestStartStation = allStations
                    .Where(s=> s.status == "OPEN" && s.available_bikes > 0 && s.position != null)
                    .OrderBy(s =>
                        new GeoCoordinate(s.position.lat, s.position.lng)
                        .GetDistanceTo(originGeo)
                    )
                    .FirstOrDefault();

                Station bestEndStation = allStations
                    .Where(s => s.status == "OPEN" && s.available_bike_stands > 0 && s.position != null)
                    .OrderBy(s =>
                        new GeoCoordinate(s.position.lat, s.position.lng)
                        .GetDistanceTo(destGeo)
                    )
                    .FirstOrDefault();


                /*Console.WriteLine("\n\n                    //LOG DEBUG//                       \n\n");
                Console.WriteLine($"Ville du trajet : {userCity}");
                Console.WriteLine($"Coordonnées origine: Lat={originCoord.Latitude}, Lon={originCoord.Longitude}");
                Console.WriteLine($"Adresse origine: {origin}");
                Console.WriteLine($"Coordonnées destination: Lat={destCoord.Latitude}, Lon={destCoord.Longitude}");
                Console.WriteLine($"Adresse destination: {destination}\n");*/


                MyGeoCoordinate startStationCoord = new MyGeoCoordinate { Latitude = bestStartStation.position.lat, Longitude = bestStartStation.position.lng };
                MyGeoCoordinate endStationCoord = new MyGeoCoordinate { Latitude = bestEndStation.position.lat, Longitude = bestEndStation.position.lng };

                string walkProfile = "foot-walking";
                string bikeProfile = "cycling-regular";

                RouteInfo walkToStation = proxyClient.GetRouteInfo(originCoord, startStationCoord, walkProfile);
                RouteInfo bikeRide = proxyClient.GetRouteInfo(startStationCoord, endStationCoord, bikeProfile);
                RouteInfo walkFromStation = proxyClient.GetRouteInfo(endStationCoord, destCoord, walkProfile);
                RouteInfo fullWalk = proxyClient.GetRouteInfo(originCoord, destCoord, walkProfile);

                if (walkToStation == null || bikeRide == null || walkFromStation == null || fullWalk == null)
                {
                    response.Status = "ERREUR";
                    response.Message = "Impossible de calculer un des itinéraires (ORS a échoué ou n'a pas trouvé de route).";
                    return response;
                }

                double bikeTotalTime = walkToStation.Duration + bikeRide.Duration + walkFromStation.Duration;
                double walkTotalTime = fullWalk.Duration;
                double bikeTotalDistance = walkToStation.Distance + bikeRide.Distance + walkFromStation.Distance;
                double walkTotalDistance = fullWalk.Distance;


                if (walkTotalTime <= bikeTotalTime)
                {
                    response.Status = "OK";
                    response.Message = "Réponse finale du REST";
                    response.Mean = "Marche recommandée\n";
                    response.Time = Math.Round(walkTotalTime / 60);
                    response.Distance = walkTotalDistance;
                    response.Comparison = $"🚶 Le trajet à pied est plus rapide : env. {Math.Round(walkTotalTime / 60)} min.\n" +
                    $"En vélo : env. {Math.Round(bikeTotalTime / 60)} min.\n";
                    response.Steps = $"Marchez jusqu'à {bestStartStation.name}, \n" +
                        $"Prenez un vélo jusqu'à {bestEndStation.name}, \n" +
                        $"Puis marchez jusqu'à {destination}. \n";

                    response.Segments = new List<RouteInfo> { fullWalk };
                }
                else
                {
                    response.Status = "OK";
                    response.Message = "Réponse finale du REST";
                    response.Mean = "Vélo recommandée\n";
                    response.Time = Math.Round(bikeTotalTime / 60);
                    response.Distance = bikeTotalDistance;
                    response.Comparison = $"🚴 Le trajet en vélo est plus rapide : env. {Math.Round(bikeTotalTime / 60)} min.\n" +
                        $"En faisant le trajet uniquement à pieds : env. {Math.Round(walkTotalTime / 60)} min.\n";
                    response.Steps = $"Marchez jusqu'à {bestStartStation.name}, \n" +
                        $"Prenez un vélo jusqu'à {bestEndStation.name}, \n" +
                        $"Puis marchez jusqu'à {destination}. \n";
                    response.Segments = new List<RouteInfo> { walkToStation, bikeRide, walkFromStation };
                }
            }
            catch (Exception e)
            {
                response.Status = "ERREUR";
                response.Message = "Une erreur est survenue : " + e.Message;
            }

            if (WebOperationContext.Current != null) 
            {
                WebOperationContext.Current.OutgoingResponse.Headers.Add(
                    "Access-Control-Allow-Origin",
                    "*" 
                );
            }
            return response;
        }


    }
}
