using RoutingServer.ProxyCacheReference; //SOAP
using System;
using System.Collections.Generic;
using System.Device.Location; //GetDistance()
using System.Linq; //Where() et OrderBy()
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Security;
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
            response.Segments = new List<RouteInfo>();

            if (WebOperationContext.Current != null)
            {
                WebOperationContext.Current.OutgoingResponse.Headers.Add("Access-Control-Allow-Origin", "*");
            }

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
                
                RouteInfo fullWalkSeg = proxyClient.GetRouteInfo(originCoord, destCoord, "foot-walking");

                if (!contracts.Any())
                {
                    response.Status = "OK";
                    response.Message = "Aucune ville supportant JCDecaux sur votre trajet. Marche recommandée."; 
                    response.Mean = "Marche obligatoire";
                    int h = (int)(fullWalkSeg.Duration / 3600);
                    int min = (int)((fullWalkSeg.Duration % 3600) / 60);
                    if (h < 1)
                    {
                        response.Time = $" env. {min}m";
                    }
                    else
                    {
                        response.Time = $"env. {h}h {min:00}m";
                    }
                    int KM = (int)(fullWalkSeg.Distance / 1000);
                    int M = (int)fullWalkSeg.Distance;
                    string fDistance;
                    if (KM < 0)
                    {
                        fDistance = $" env. {M}m.";

                    }
                    else
                    {
                        fDistance = $"env. {KM}km.";
                    }
                    response.Comparison = $"🚶 Le trajet à pied est la seule option.";
                    response.Distance = fDistance ;
                    response.Steps = $"Marchez de {origin} à {destination}";
                    response.Segments.Add(fullWalkSeg);
                    return response;
                }

                GeoCoordinate currentLocation = globalOrigin;
                double totalDuration = 0;
                double totalDistance = 0;
                string stepsBuilder = "Itinéraire optimisé :\n";

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

                    if (entryStation == null || exitStation == null) continue;

                    GeoCoordinate entryGeo = new GeoCoordinate(entryStation.position.lat, entryStation.position.lng);
                    GeoCoordinate exitGeo = new GeoCoordinate(exitStation.position.lat, exitStation.position.lng);

                    if (entryGeo.GetDistanceTo(globalDest) > currentLocation.GetDistanceTo(globalDest)) continue;

                    MyGeoCoordinate currentMyGeo = new MyGeoCoordinate { Latitude = currentLocation.Latitude, Longitude = currentLocation.Longitude };
                    MyGeoCoordinate entryMyGeo = new MyGeoCoordinate { Latitude = entryGeo.Latitude, Longitude = entryGeo.Longitude };
                    MyGeoCoordinate exitMyGeo = new MyGeoCoordinate { Latitude = exitGeo.Latitude, Longitude = exitGeo.Longitude };

                    RouteInfo walkSegment = proxyClient.GetRouteInfo(currentMyGeo, entryMyGeo, "foot-walking");
                    if (walkSegment != null)
                    {
                        response.Segments.Add(walkSegment);
                        totalDuration += walkSegment.Duration;
                        totalDistance += walkSegment.Distance;
                        stepsBuilder += $"- Marchez jusqu'à {entryStation.name} ({contract.Name})\n";
                    }

                    RouteInfo bikeSegment = proxyClient.GetRouteInfo(entryMyGeo, exitMyGeo, "cycling-regular");
                    if (bikeSegment != null)
                    {
                        response.Segments.Add(bikeSegment);
                        totalDuration += bikeSegment.Duration;
                        totalDistance += bikeSegment.Distance;
                        stepsBuilder += $"- Prenez un vélo jusqu'à {exitStation.name}\n";
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
                    stepsBuilder += "- Terminez à pied jusqu'à votre destination.";
                }

                response.Status = "OK_MULTI";
                response.Message = "Réponse finale du REST";
                response.Mean = "Vélo recommandé.";
                int heures = (int)(totalDuration / 3600);
                int minutes = (int)((totalDuration % 3600) / 60);
                string finalTime;
                if (heures < 1)
                {
                    finalTime = $" env. {minutes}m";
                }
                else
                {
                    finalTime = $"env. {heures}h {minutes}m";
                }
                int km = (int)(totalDistance / 1000);
                int m = (int)totalDistance;
                string finalDistance;
                if(km < 0)
                {
                    finalDistance = $" env. {m}m.";

                }
                else
                {
                    finalDistance = $"env. {km}km.";
                }
                response.Distance = finalDistance;
                response.Time = finalTime;
                response.Comparison = $"🚴 Le trajet en vélo est plus rapide : env. {finalTime}.\n" +
                             $"En faisant le trajet uniquement à pieds : env. {(int)(fullWalkSeg.Duration / 3600)}h  {(int)((fullWalkSeg.Duration % 3600)/60)}min.\n";
                response.Steps = stepsBuilder;
                return response;

            }
            catch (Exception e)
            {
                response.Status = "ERREUR";
                response.Message = "Erreur : " + e.Message;
            }

            return response;
        }

        private bool IsOnPath(GeoCoordinate point, GeoCoordinate start, GeoCoordinate end)
        {
            double margin = 0.5;
            double minLat = Math.Min(start.Latitude, end.Latitude) - margin;
            double maxLat = Math.Max(start.Latitude, end.Latitude) + margin;
            double minLon = Math.Min(start.Longitude, end.Longitude) - margin;
            double maxLon = Math.Max(start.Longitude, end.Longitude) + margin;

            if (!(point.Latitude >= minLat && point.Latitude <= maxLat &&
                  point.Longitude >= minLon && point.Longitude <= maxLon))
            {
                return false; 
            }

            double distanceToLine = GetDistanceFromPointToLine(point, start, end);

            return distanceToLine < 40000; //40 Km de marge pour dire si on passe par la ville ou pas 
        }

        private double GetDistanceFromPointToLine(GeoCoordinate point, GeoCoordinate start, GeoCoordinate end)
        { 
            double d1 = point.GetDistanceTo(start);
            double d2 = point.GetDistanceTo(end);
            double lineLength = start.GetDistanceTo(end);

            if (lineLength == 0) return d1;

            if (d2 * d2 >= d1 * d1 + lineLength * lineLength) return d1; //Pythagore pour verifier si le point est derrière la ville de départ 

            if (d1 * d1 >= d2 * d2 + lineLength * lineLength) return d2; //Pythagore pour verifier si le point est derrière la ville d'arrivée 

            double s = (d1 + d2 + lineLength) / 2;

            double areaArg = s * (s - d1) * (s - d2) * (s - lineLength); //Héron pour trouver l'aire du triangle formé par d1, d2 et le pt
            if (areaArg <= 0) return 0; //gestion de cas innatendus de racines négatives

            double area = Math.Sqrt(areaArg); 

            return 2 * area / lineLength; //on retrouve la hauteur du triangle avec sa base et son aire
        }
    }
}
