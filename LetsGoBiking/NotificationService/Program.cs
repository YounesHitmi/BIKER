using System;
using System.Threading;
using Apache.NMS;

namespace NotificationService
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Lancement du service de notifications (Apache NMS STOMP)...");

            IConnectionFactory factory = new NMSConnectionFactory("stomp:tcp://localhost:61613");

            try
            {
                using (IConnection connection = factory.CreateConnection())
                {
                    connection.Start();

                    using (ISession session = connection.CreateSession())
                    {
                        IDestination destination = session.GetTopic("BikingEvents");

                        using (IMessageProducer producer = session.CreateProducer(destination))
                        {
                            producer.DeliveryMode = MsgDeliveryMode.NonPersistent;

                            Console.WriteLine("Connecté ! Envoi des messages...");

                            var random = new Random();
                            string[] types = { "Meteo", "Pollution", "InfoTrafic" };
                            string[] levels = { "Bon", "Moyen", "Critique" };

                            while (true)
                            {
                                string type = types[random.Next(types.Length)];
                                string level = levels[random.Next(levels.Length)];
                                string messageText = $"{type}: Niveau {level} actuellement.";

                                string json = $"{{\"type\": \"{type}\", \"level\": \"{level}\", \"message\": \"{messageText}\"}}";

                                ITextMessage message = session.CreateTextMessage(json);
                                producer.Send(message);

                                Console.WriteLine($"[STOMP] Envoyé : {json}");

                                Thread.Sleep(10000); 
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERREUR : {ex.Message}");
                Console.WriteLine("Vérifiez que :");
                Console.WriteLine("1. ActiveMQ est lancé.");
                Console.WriteLine("2. Le connecteur 'stomp' (61613) est bien dans activemq.xml");
                Console.ReadLine();
            }
        }
    }
}