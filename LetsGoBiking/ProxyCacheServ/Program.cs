using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ServiceModel;

namespace ProxyCacheServer
{
    internal class Program
    {   
        static void Main(string[] args)
        {
            ServiceHost host = null; //déclaration de l'hôte

            try
            {
                host = new ServiceHost(typeof(ProxyCacheService));
                host.Open();
                Console.WriteLine("Le serveur Proxy/Cache (SOAP) est démarré.\n");
                Console.WriteLine("Il écoute sur le port 8733 (configuré dans App.config).\n");
                Console.WriteLine("Appuyez sur <Entrée> pour arrêter le service.\n");

                Console.ReadLine();
                host.Close();
            }
            catch (AddressAccessDeniedException)
            {
                Console.WriteLine("Erreur : Accès refusé.\n Impossible d'ouvrir le port 8733.\n Veuillez executer ce programme en tant qu'administrateur.\n");
                Console.ReadLine();
                host?.Abort();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Erreur inconnue au démarrage : " + ex);
                Console.ReadLine();
                host?.Abort();
            }
        }
    }
}
