using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;
using System.ServiceModel.Web;
using RoutingServer.ProxyCacheReference;

namespace RoutingServer
{
    // REMARQUE : vous pouvez utiliser la commande Renommer du menu Refactoriser pour changer le nom d'interface "IService1" à la fois dans le code et le fichier de configuration.
    [ServiceContract]
    public interface IRoutingService
    {
        [OperationContract]
        [WebGet(UriTemplate = "/itinerary?from={origin}&to={destination}", ResponseFormat = WebMessageFormat.Json)]
        ItineraryResponse FindItinerary(string origin, string destination);

    }

    // Utilisez un contrat de données comme indiqué dans l'exemple ci-après pour ajouter les types composites aux opérations de service.
    // Vous pouvez ajouter des fichiers XSD au projet. Une fois le projet généré, vous pouvez utiliser directement les types de données qui y sont définis, avec l'espace de noms "RoutingServer.ContractType".
    [DataContract]
    public class ItineraryResponse
    {
        [DataMember]
        public string Status
        {
            get; set;
        }

        [DataMember]
        public string Message
        {
            get; set;
        }

        [DataMember]
        public List<RouteInfo> Segments { get; set; }

    }

}
