using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ServiceModel;
using System.ServiceModel.Description;
using System.ServiceModel.Web;

namespace RoutingServer
{
    class Program
    {
        static void Main(string[] args)
        {
            Uri httpUrl = new Uri("http://localhost:8734/RoutingServer/");

            ServiceHost host = new ServiceHost(typeof(RoutingService), httpUrl);

            WebHttpBinding restBinding = new WebHttpBinding();

            ServiceEndpoint restEndpoint = host.AddServiceEndpoint(
                typeof(IRoutingService),
                restBinding,
                "");

            restEndpoint.Behaviors.Add(new WebHttpBehavior());

            host.AddServiceEndpoint(typeof(IRoutingService), new BasicHttpBinding(), "Soap");

            //Enable metadata exchange
            ServiceMetadataBehavior smb = new ServiceMetadataBehavior();
            smb.HttpGetEnabled = true;
            host.Description.Behaviors.Add(smb);

            //Start the Service
            host.Open();

            Console.WriteLine("Service is host at " + DateTime.Now.ToString());
            Console.WriteLine("Host is running... Press <Enter> key to stop");
            Console.ReadLine();

        }
    }
}
