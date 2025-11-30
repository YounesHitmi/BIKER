using System.ServiceModel;
using HeavyClient.HeavyClientService;
using System;

namespace HeavyClient
{
    internal class Program
    {
        static void Main(string[] args)
        {
            bool ongoing = true;
            Console.WriteLine("---------------------------- CLIENT LOURD-------------------------------");
            Console.WriteLine('\n');
            Console.WriteLine('\n');

            BasicHttpBinding binding = new BasicHttpBinding();
            binding.MaxReceivedMessageSize = 2147483647;
            binding.ReaderQuotas.MaxStringContentLength = 2147483647;
            EndpointAddress address = new EndpointAddress("http://localhost:8734/RoutingServer/Soap");
            RoutingServiceClient client = new RoutingServiceClient(binding,address);

            while (ongoing)
            {
                Console.WriteLine("Entrez votre addresse de départ : ");
                string origin = Console.ReadLine();
                Console.WriteLine("Entrez votre addresse d'arrivée : ");
                string destination = Console.ReadLine();

                try
                {
                    ItineraryResponse response = client.FindItinerary(origin, destination);
                    Console.WriteLine($"Pour votre trajet de {origin} à {destination} voici les informations : \n");
                    Console.WriteLine($"Moyen recommandé : {response.Mean}\n");
                    Console.WriteLine($"Temps mis pour arriver à destination : {response.Time} min.\n");
                    Console.WriteLine($"Distance à parcourir : {response.Distance} m.\n");
                    Console.WriteLine($"{response.Comparison}\n");

                    foreach (var segment in response.Segments)
                    {
                        foreach (var route in segment.Instructions)
                        {
                            Console.WriteLine($"{route.Description} dans {route.Distance} m.\n");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("ERREUR : " + ex.Message);
                }
                Console.WriteLine("\nContinuer ? O/N \n ");
                string ans = Console.ReadLine();
                if (ans == "N")
                {
                    ongoing = false;
                }
            }

        }
    }
}
