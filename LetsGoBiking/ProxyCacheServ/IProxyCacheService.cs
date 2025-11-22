using System;
using System.Collections.Generic;
using System.Device.Location;
using System.Diagnostics.Contracts;
using System.Globalization;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;
using static ProxyCacheServer.ProxyCacheService;

namespace ProxyCacheServer
{
    // REMARQUE : vous pouvez utiliser la commande Renommer du menu Refactoriser pour changer le nom d'interface "IService1" à la fois dans le code et le fichier de configuration.
    [ServiceContract]
    public interface IProxyCacheService
    {
        [OperationContract]
        string GetStationsForContract(String contract);

        [OperationContract]
        MyGeoCoordinate GetCoordinates(string address);

        [OperationContract]
        RouteInfo GetRouteInfo(MyGeoCoordinate start, MyGeoCoordinate end, string profile);

        [OperationContract]
        string GetAllStations();

        [OperationContract]
        List<MyContract> GetContracts();

        [OperationContract]
        string GetCityFromCoordinates(MyGeoCoordinate coord);


    }

    [DataContract]
    public class MyContract
    {
        [DataMember]
        public string name { get; set; }

        [DataMember]
        public string commercial_name { get; set; }

        [DataMember]
        public string country_code { get; set; }

        [DataMember]
        public List<string> cities { get; set; }
    }


    [DataContract]
    public class MyGeoCoordinate
    {
        [DataMember]
        public double Latitude { get; set; }

        [DataMember]
        public double Longitude { get; set; }
    }

    [DataContract]
    public class RouteInstruction
    {
        [DataMember]
        public string Description { get; set; }
        [DataMember]
        public double Distance { get; set; }
        [DataMember]
        public int Type { get; set; }
    }

    [DataContract]
    public class RouteInfo
    {
        [DataMember]
        public double Duration { get; set; } 

        [DataMember]
        public double Distance { get; set; } 

        [DataMember]
        public string Geometry { get; set; }

        [DataMember]
        public string Profile { get; set; }

        [DataMember]
        public RouteInstruction[] Instructions { get; set; }

    }
}
