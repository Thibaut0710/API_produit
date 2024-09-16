﻿using API_produit.Context;
using API_produit.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Channels;
using System.Web;

public class RabbitMQService : IRabbitMQService
{
    private readonly string _hostName;
    private readonly string _queueName;
    private readonly string _userName;
    private readonly string _password;
    private readonly IServiceScopeFactory _scopeFactory;
    private static ConcurrentDictionary<string, TaskCompletionSource<string>> _pendingMessages = new ConcurrentDictionary<string, TaskCompletionSource<string>>();
    private readonly IConnection _connection;
    private readonly IModel _channel;


    public RabbitMQService(IConfiguration configuration, IServiceScopeFactory scopeFactory)
    {
        _hostName = configuration["RabbitMQ:HostName"];
        _queueName = configuration["RabbitMQ:QueueName"];
        _userName = configuration["RabbitMQ:UserName"];
        _password = configuration["RabbitMQ:Password"];
        _scopeFactory = scopeFactory;

        var factory = new ConnectionFactory() { HostName = _hostName, UserName = _userName, Password = _password };
        _connection = factory.CreateConnection();
        _channel = _connection.CreateModel();
    }
    public void CreateConsumerProduitInCommmande()
    {
        Console.WriteLine("Create Consumer");
        Task.Run(async () =>
        {
            var factory = new ConnectionFactory() { HostName = _hostName, UserName = _userName, Password = _password };

            using (var connection = factory.CreateConnection())
            using (var channel = connection.CreateModel())
            {
                channel.QueueDeclare(queue: "produitInCommande", durable: true, exclusive: false, autoDelete: false, arguments: null);
                channel.BasicQos(prefetchSize: 0, prefetchCount: 1, global: false);

                Console.WriteLine(" [*] Waiting for messages.");

                var consumer = new EventingBasicConsumer(channel);
                consumer.Received += async (model, ea) =>
                {
                    byte[] body = ea.Body.ToArray();
                    var message = Encoding.UTF8.GetString(body);
                    Console.WriteLine("Received message: " + message);

                    // Désérialiser le message JSON en liste d'IDs
                    List<int> produitsId = null;
                    try
                    {
                        produitsId = JsonSerializer.Deserialize<List<int>>(message);
                    }
                    catch (JsonException ex)
                    {
                        Console.WriteLine("Erreur de désérialisation : " + ex.Message);
                        // Acknowledge the message even if there's an error
                        channel.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);
                        return;
                    }

                    if (produitsId != null && produitsId.Count > 0)
                    {
                        using (var scope = _scopeFactory.CreateScope())
                        {
                            var context = scope.ServiceProvider.GetRequiredService<ProduitContext>();

                            // Obtenir les produits depuis la base de données
                            var response = await context.Produits.Where(produit => produitsId.Contains(produit.Id)).ToListAsync();
                            Console.WriteLine("Produits trouvés : " + string.Join(", ", response.Select(p => p.Id)));

                            // Préparer la réponse en JSON
                            var produits = response.Select(produit => new
                            {
                                produit.Id,
                                produit.Name // Ajoutez d'autres propriétés si nécessaire
                            }).ToList();

                            string json = JsonSerializer.Serialize(produits);
                            Console.WriteLine("Réponse JSON : " + json);

                            // Créer les propriétés du message de réponse avec le CorrelationId d'origine
                            var replyProperties = channel.CreateBasicProperties();
                            replyProperties.CorrelationId = ea.BasicProperties.CorrelationId;

                            // Envoyer la réponse à la queue de réponse
                            channel.BasicPublish(
                                exchange: string.Empty,
                                routingKey: ea.BasicProperties.ReplyTo, // La queue de réponse est spécifiée dans le message original
                                basicProperties: replyProperties,
                                body: Encoding.UTF8.GetBytes(json));
                        }
                    }
                    else
                    {
                        Console.WriteLine("Aucun ID de produit valide trouvé dans le message.");
                    }

                    // Acknowledge the message
                    channel.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);
                };

                channel.BasicConsume(queue: "produitInCommande",
                                     autoAck: false,
                                     consumer: consumer);

                // Keep the thread alive while consuming messages
                while (true)
                {
                    // Add any additional logic needed to keep the consumer alive,
                    // such as waiting for cancellation tokens or other termination signals.
                    await Task.Delay(1000); // Ajoute un délai pour éviter une boucle infinie qui consomme trop de CPU
                }
            }
        });
    }



    public async Task<string> SendMessageAndWaitForResponseAsync(string message, string CommandQueueName, string ReplyQueueName)
    {
        _channel.QueueDeclare(queue: ReplyQueueName, durable: true, exclusive: false, autoDelete: false, arguments: null);
        await Task.Delay(100);

        var consumer = new EventingBasicConsumer(_channel);
        consumer.Received += OnResponseReceived;
        _channel.BasicConsume(consumer: consumer, queue: ReplyQueueName, autoAck: true);
        Console.WriteLine(ReplyQueueName);


        var correlationId = Guid.NewGuid().ToString();
        var tcs = new TaskCompletionSource<string>();

        _pendingMessages[correlationId] = tcs;

        var properties = _channel.CreateBasicProperties();
        properties.CorrelationId = correlationId;
        properties.ReplyTo = ReplyQueueName;

        var body = Encoding.UTF8.GetBytes(message);
        Console.WriteLine(CommandQueueName);
        _channel.BasicPublish(exchange: string.Empty,
                             routingKey: CommandQueueName, // Utilisation de la queue de commande en dur
                             basicProperties: properties,
                             body: body);

        Console.WriteLine($" [x] Sent {message} with CorrelationId {correlationId}");

        // Attendre de manière asynchrone que la réponse arrive
        return await tcs.Task;
    }
    private void OnResponseReceived(object sender, BasicDeliverEventArgs ea)
    {
        var correlationId = ea.BasicProperties?.CorrelationId;

        if (string.IsNullOrEmpty(correlationId))
        {
            Console.WriteLine("Received a message without a CorrelationId or with a null CorrelationId.");
            return;
        }

        var response = Encoding.UTF8.GetString(ea.Body.ToArray());

        Console.WriteLine($" [x] Received {response} with CorrelationId {correlationId}");

        // Vérifier si le message attendu est dans la liste
        if (_pendingMessages.TryRemove(correlationId, out var tcs))
        {
            if (tcs != null)
            {
                tcs.SetResult(response); // Renvoie la réponse à la méthode appelante
            }
            else
            {
                Console.WriteLine($"TaskCompletionSource for CorrelationId {correlationId} is null.");
            }
        }
        else
        {
            Console.WriteLine($"No pending message found for CorrelationId {correlationId}.");
        }
    }
    private void SendReply(IModel channel, BasicDeliverEventArgs ea, string jsonResponse)
    {
        // Create reply properties with the original CorrelationId
        var replyProperties = channel.CreateBasicProperties();
        replyProperties.CorrelationId = ea.BasicProperties.CorrelationId;
        Console.WriteLine(ea.BasicProperties.ReplyTo);
        // Publish the reply to the specified reply queue
        channel.BasicPublish(exchange: string.Empty,
                             routingKey: ea.BasicProperties.ReplyTo, // La queue de réponse est spécifiée dans le message original
                             basicProperties: replyProperties,
                             body: Encoding.UTF8.GetBytes(jsonResponse));

        Console.WriteLine($" [x] Replied with processed data for CorrelationId: {ea.BasicProperties.CorrelationId}");
    }
    static List<int> ConvertJsonElementArrayToIntList(JsonElement jsonArray)
    {
        var intList = new List<int>();

        // Assurez-vous que jsonArray est un tableau
        if (jsonArray.ValueKind == JsonValueKind.Array)
        {
            foreach (var element in jsonArray.EnumerateArray())
            {
                if (element.ValueKind == JsonValueKind.Number)
                {
                    intList.Add(element.GetInt32());
                }
            }
        }
        return intList;
    }
    public void Dispose()
    {
        _channel?.Close();
        _connection?.Close();
    }

}
