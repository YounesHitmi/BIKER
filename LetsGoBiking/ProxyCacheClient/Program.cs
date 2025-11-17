using ProxyCacheClient.ProxyCacheReference;
using System;

namespace ProxyCacheClient
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Appel du service SOAP...");

            ProxyCacheServiceClient client = new ProxyCacheServiceClient();

            try
            {
                Console.Write("Pour quel contrat voulez-vous les stations ? ");
                string contratChoisi = Console.ReadLine();
                string stationsJson = client.GetStationsForContract(contratChoisi.ToLower());

                Console.WriteLine("Réponse reçue !");
                Console.WriteLine(stationsJson);
            }
            catch (Exception ex)
            {
                Console.WriteLine("ERREUR : " + ex.Message);
            }

            Console.WriteLine("\nAppuyez sur Entrée pour quitter.");
            Console.ReadLine();
        }
    }
}