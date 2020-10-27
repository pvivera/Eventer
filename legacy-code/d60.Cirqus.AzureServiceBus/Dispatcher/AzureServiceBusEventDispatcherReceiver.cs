﻿using System;
using System.Threading;
using d60.Cirqus.Events;
using d60.Cirqus.Views;
using Microsoft.ServiceBus.Messaging;

namespace d60.Cirqus.AzureServiceBus.Dispatcher
{
    public class AzureServiceBusEventDispatcherReceiver : IDisposable
    {
        readonly Serializer _serializer = new Serializer();
        readonly IEventDispatcher _innerEventDispatcher;
        readonly IEventStore _eventStore;
        readonly SubscriptionClient _subscriptionClient;
        readonly Thread _workerThread;

        bool _keepWorking = true;

        public AzureServiceBusEventDispatcherReceiver(string connectionString, IEventDispatcher innerEventDispatcher, IEventStore eventStore, string topicName, string subscriptionName)
        {
            _innerEventDispatcher = innerEventDispatcher;
            _eventStore = eventStore;

            AzureHelpers.EnsureTopicExists(connectionString, topicName);
            AzureHelpers.EnsureSubscriptionExists(connectionString, topicName, subscriptionName);

            _subscriptionClient = SubscriptionClient.CreateFromConnectionString(connectionString, topicName, subscriptionName);

            _workerThread = new Thread(DoWork);
        }

        public event Action<Exception> Error; 

        public void Initialize(bool purgeExistingViews = false)
        {
            _innerEventDispatcher.Initialize(purgeExistingViews);

            _workerThread.Start();
        }

        void DoWork()
        {
            while (_keepWorking)
            {
                try
                {
                    var message = _subscriptionClient.Receive(TimeSpan.FromSeconds(1));
                    if (message == null) continue;

                    var notification = message.GetBody<DispatchNotification>();

                    _innerEventDispatcher.Dispatch(_serializer.Deserialize(notification.DomainEvents));
                }
                catch (Exception e)
                {
                    RaiseError(e);

                    Thread.Sleep(2000);
                }
            }
        }

        void RaiseError(Exception e)
        {
            var errorHandlers = Error;
            
            if (errorHandlers != null)
            {
                errorHandlers(e);
            }
        }

        public void Dispose()
        {
            _keepWorking = false;
            _workerThread.Join();
        }
    }
}