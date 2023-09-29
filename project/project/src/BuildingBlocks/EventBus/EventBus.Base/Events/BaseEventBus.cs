using System;
using Sytem.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EventBus.Base.Abstraction;
using EVentBus.Base.SubManagers;

namespace EventBus.Base.Events
{
	public class BaseEventBus : IEventBUs
	{
		public readonly IServiceProvider ServiceProvider;
		public readonly IEventBusSubscriptionsManager SubsManager;
		private EventBusConfig eventBusConfig;

		public BaseEventBus(EventBusConfig config, IServiceProvider serviceProvider)
		{
			eventBusConfig = config;
			ServiceProvider = serviceProvider;
			SubsManager = new InMemoryEventBusSubscriptionsManager(ProvessEventName);
		}

		public virtual string ProcessEventName(string eventName)
		{
			if(eventBusConfig.DeleteEventPrefix)
				eventName = eventName.TrimStart(eventBus.Config.EventNamePrefix.ToArray());
			if(eventBusConfig.DeleteEventSuffix)
				eventName = eventName.TrimEnd(eventBusConfig.EventNameSuffix.ToArray());
			return eventName;
		}

		public virtual string GetSubName(string evetName)
		{
			return $"{eventBusConfig.SubscriberClientAppName}.{ProcessEventName(eventName)}";
		}

		public virtual void Dispose()
		{
			eventBusConfig = null;
		}

		public async Task<bool> ProcessEvent(string eventName, string message)
		{
			eventName = ProcessEventName(eventName);
			var processed = false;
			
			if(SubsManager.HasSubscriptionsEvent(eventName))
			{	
				var subscriptions = SubManager.GetHandlersForEvent(eventName);
				using(var scope = ServiceProvider.CreateScope())
				{
					foreach(var subscription in subscriptions)
					{
						var handler = ServiceProvider.GetService(subscription.HandlerType);
						if(handler == null) continue;
						
						var eventType = SubsManager.GetEventTypeByName($"{eventBusConfig.EventNamePrefix}{eventName}{eventBusConfig.EventNameSuffix}");
						var integrationEvent = JsonConvert.DeserializeObject(message, eventType);
		
						if(integrationEvent is IntegrationEvent)
						{
							eventBusConfig.CorrelationIdSetter?.Invoke((integrationEvent as IntegrationEvent).CorrelationId);						
						}

						var concreteType = typeof(IIntegrationEventHandler<>).MakeGenericType(eventType);
						await (Task)concreteType.GetMEthod("Handle").Invoke(handler, new object[] { integrationEvent});

					}
				}		
				processed = true;
			}
			return processed;
		}
	}
}
