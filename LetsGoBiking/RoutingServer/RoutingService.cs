using RoutingServer.ProxyCacheReference; //SOAP
using System;
using System.Collections.Generic;
using System.Device.Location; //GetDistance()
using System.Linq; //Where() et OrderBy()
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.ServiceModel;
using System.ServiceModel.Web;
using System.Text.Json; //Deserialisation
using System.Threading.Tasks;

namespace RoutingServer
{
    public class RoutingService : IRoutingService
    {
        /* public ItineraryResponse FindItinerary(string origin, string destination)
         {
             ItineraryResponse response = new ItineraryResponse();

             try
             {
                 WSHttpBinding binding = new WSHttpBinding();
                 binding.MaxReceivedMessageSize = 2147483647;
                 binding.ReaderQuotas.MaxStringContentLength = 2147483647;
                 EndpointAddress endpoint = new EndpointAddress("http://localhost:8733/ProxyCacheServ/");
                 ProxyCacheServiceClient proxyClient = new ProxyCacheServiceClient(binding, endpoint);

                 if (WebOperationContext.Current != null) //Pour CORS
                 {
                     WebOperationContext.Current.OutgoingResponse.Headers.Add(
                         "Access-Control-Allow-Origin",
                         "*"
                     );
                 }

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
        /*
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
                 GeoCoordinate currentGeo = new GeoCoordinate(originCoord.Latitude, originCoord.Longitude);
                 GeoCoordinate destGeo = new GeoCoordinate(destCoord.Latitude, destCoord.Longitude);

                 List<RouteInfo> routesResp = new List<RouteInfo>();

                 while (!currentGeo.Equals(destGeo))
                 {
                     Station bestStartStation = allStations
                     .Where(s => s.status == "OPEN" && s.available_bikes > 0 && s.position != null)
                     .OrderBy(s =>
                         new GeoCoordinate(s.position.lat, s.position.lng)
                         .GetDistanceTo(currentGeo)
                     )
                     .FirstOrDefault();

                     Station bestEndStation = allStations
                         .Where(s => s.status == "OPEN" && s.available_bike_stands > 0 && s.position != null)
                         .OrderBy(s =>
                             new GeoCoordinate(s.position.lat, s.position.lng)
                             .GetDistanceTo(destGeo)
                         )
                         .FirstOrDefault();

                     if (bestStartStation.contract_name != bestEndStation.contract_name)
                     {

                     }


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

                     if (bestStartStation.contract_name != bestEndStation.contract_name)
                     {
                         response.Status = "OK";
                         response.Message = "Réponse finale du REST";
                         response.Mean = "Marche obligatoire\n";
                         response.Time = Math.Round(walkTotalTime / 60);
                         response.Distance = walkTotalDistance;
                         response.Comparison = $"🚶 Le trajet à pied est la seule option. Le départ et l'arrivée n'ont pas le même contrat. Temps total du trajet : env. {Math.Round(walkTotalTime / 60)} min.\n" +
                         $"En vélo : env. {Math.Round(bikeTotalTime / 60)} min.\n";
                         response.Steps = $"Marchez jusqu'à {bestStartStation.name}, \n" +
                             $"Prenez un vélo jusqu'à {bestEndStation.name}, \n" +
                             $"Puis marchez jusqu'à {destination}. \n";
                         routesResp.Add(fullWalk);
                         response.Segments = routesResp;
                         return reponse;

                     }
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
                         routesResp.Add(fullWalk);
                         response.Segments = routesResp;
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
                         routesResp.Add(walkToStation);
                         routesResp.Add(bikeRide);
                         routesResp.Add(walkFromStation);
                         response.Segments = routesResp;
                     }
                 }


             }

             catch (Exception e)
             {
                 response.Status = "ERREUR";
                 response.Message = "Une erreur est survenue : " + e.Message;
             }

             return response;
         }*/

        public ItineraryResponse FindItinerary(string origin, string destination)
        {
            ItineraryResponse response = new ItineraryResponse();
            response.Segments = new List<RouteInfo>();

            try
            {
                WSHttpBinding binding = new WSHttpBinding();
                binding.MaxReceivedMessageSize = 2147483647;
                binding.ReaderQuotas.MaxStringContentLength = 2147483647;
                EndpointAddress endpoint = new EndpointAddress("http://localhost:8733/ProxyCacheServ/");
                ProxyCacheServiceClient proxyClient = new ProxyCacheServiceClient(binding, endpoint);

                MyGeoCoordinate originCoord = proxyClient.GetCoordinates(origin);
                MyGeoCoordinate destCoord = proxyClient.GetCoordinates(destination);

                if (originCoord == null || destCoord == null)
                {
                    response.Status = "ERREUR";
                    response.Message = "Impossible de géocoder l'origine ou la destination.";
                    return response;
                }

                GeoCoordinate globalOrigin = new GeoCoordinate(originCoord.Latitude, originCoord.Longitude);
                GeoCoordinate globalDest = new GeoCoordinate(destCoord.Latitude, destCoord.Longitude);

                string allStationsJson = proxyClient.GetAllStations();
                if (string.IsNullOrEmpty(allStationsJson))
                {
                    response.Status = "ERREUR";
                    response.Message = "Impossible de récupérer les stations.";
                    return response;
                }

                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                List<Station> allStations = JsonSerializer.Deserialize<List<Station>>(allStationsJson, options);

                var contracts = allStations
                    .Where(s => s.position != null)
                    .GroupBy(s => s.contract_name)
                    .Select(g => new
                    {
                        Name = g.Key,
                        Stations = g.ToList(),
                        Center = new GeoCoordinate(g.Average(s => s.position.lat), g.Average(s => s.position.lng))
                    })
                    .Where(c => IsOnPath(c.Center, globalOrigin, globalDest))
                    .OrderBy(c => c.Center.GetDistanceTo(globalOrigin))
                    .ToList();

                if (!contracts.Any())
                {
                    response.Status = "OK_WALK";
                    response.Message = "Aucune ville JCDecaux sur votre trajet. Marche recommandée.";
                    return response;
                }

                GeoCoordinate currentLocation = globalOrigin;
                double totalDuration = 0;
                double totalDistance = 0;
                string messageBuilder = "Itinéraire optimisé :\n";

                foreach (var contract in contracts)
                {
                    Station entryStation = contract.Stations
                        .Where(s => s.status == "OPEN" && s.available_bikes > 0)
                        .OrderBy(s => new GeoCoordinate(s.position.lat, s.position.lng).GetDistanceTo(currentLocation))
                        .FirstOrDefault();

                    Station exitStation = contract.Stations
                        .Where(s => s.status == "OPEN" && s.available_bike_stands > 0)
                        .OrderBy(s => new GeoCoordinate(s.position.lat, s.position.lng).GetDistanceTo(globalDest))
                        .FirstOrDefault();

                    // Si stations invalides ou si traverser la ville à vélo n'avance pas, on saute ce contrat
                    if (entryStation == null || exitStation == null) continue;

                    GeoCoordinate entryGeo = new GeoCoordinate(entryStation.position.lat, entryStation.position.lng);
                    GeoCoordinate exitGeo = new GeoCoordinate(exitStation.position.lat, exitStation.position.lng);

                    if (entryGeo.GetDistanceTo(globalDest) > currentLocation.GetDistanceTo(globalDest)) continue;

                    MyGeoCoordinate currentMyGeo = new MyGeoCoordinate { Latitude = currentLocation.Latitude, Longitude = currentLocation.Longitude };
                    MyGeoCoordinate entryMyGeo = new MyGeoCoordinate { Latitude = entryGeo.Latitude, Longitude = entryGeo.Longitude };

                    RouteInfo walkSegment = proxyClient.GetRouteInfo(currentMyGeo, entryMyGeo, "foot-walking");
                    if (walkSegment != null)
                    {
                        response.Segments.Add(walkSegment);
                        totalDuration += walkSegment.Duration;
                        totalDistance += walkSegment.Distance;
                        messageBuilder += $"- Marchez jusqu'à {entryStation.name} ({contract.Name})\n";
                    }

                    MyGeoCoordinate exitMyGeo = new MyGeoCoordinate { Latitude = exitGeo.Latitude, Longitude = exitGeo.Longitude };

                    RouteInfo bikeSegment = proxyClient.GetRouteInfo(entryMyGeo, exitMyGeo, "cycling-regular");
                    if (bikeSegment != null)
                    {
                        response.Segments.Add(bikeSegment);
                        totalDuration += bikeSegment.Duration;
                        totalDistance += bikeSegment.Distance;
                        messageBuilder += $"- Prenez un vélo jusqu'à {exitStation.name}\n";
                    }

                    currentLocation = exitGeo;
                }

                MyGeoCoordinate lastStationMyGeo = new MyGeoCoordinate { Latitude = currentLocation.Latitude, Longitude = currentLocation.Longitude };
                MyGeoCoordinate finalDestMyGeo = new MyGeoCoordinate { Latitude = globalDest.Latitude, Longitude = globalDest.Longitude };

                RouteInfo finalWalk = proxyClient.GetRouteInfo(lastStationMyGeo, finalDestMyGeo, "foot-walking");
                if (finalWalk != null)
                {
                    response.Segments.Add(finalWalk);
                    totalDuration += finalWalk.Duration;
                    totalDistance += finalWalk.Distance;
                    messageBuilder += "- Terminez à pied jusqu'à votre destination.";
                }

                response.Status = "OK_MULTI";
                response.Message = messageBuilder + $"\nTemps total : {Math.Round(totalDuration / 60)} min.";

            }
            catch (Exception e)
            {
                response.Status = "ERREUR";
                response.Message = "Erreur : " + e.Message;
            }

            if (WebOperationContext.Current != null)
            {
                WebOperationContext.Current.OutgoingResponse.Headers.Add("Access-Control-Allow-Origin", "*");
            }

            return response;
        }

        private bool IsOnPath(GeoCoordinate point, GeoCoordinate start, GeoCoordinate end)
        {
            double margin = 0.1;

            double minLat = Math.Min(start.Latitude, end.Latitude) - margin;
            double maxLat = Math.Max(start.Latitude, end.Latitude) + margin;
            double minLon = Math.Min(start.Longitude, end.Longitude) - margin;
            double maxLon = Math.Max(start.Longitude, end.Longitude) + margin;

            return point.Latitude >= minLat && point.Latitude <= maxLat &&
                   point.Longitude >= minLon && point.Longitude <= maxLon;
        }
    }
}
