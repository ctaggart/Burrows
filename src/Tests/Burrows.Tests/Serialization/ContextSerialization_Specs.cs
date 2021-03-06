// Copyright 2007-2011 Chris Patterson, Dru Sellers, Travis Smith, et. al.
//  
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use 
// this file except in compliance with the License. You may obtain a copy of the 
// License at 
// 
//     http://www.apache.org/licenses/LICENSE-2.0 
// 
// Unless required by applicable law or agreed to in writing, software distributed 
// under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR 
// CONDITIONS OF ANY KIND, either express or implied. See the License for the 
// specific language governing permissions and limitations under the License.

using Burrows.Configuration;

namespace Burrows.Tests.Serialization
{
    using System.Threading;
    using Context;
    using Magnum.Extensions;
    using Burrows.Serialization;
    using Messages;
    using NUnit.Framework;
    using TextFixtures;

    [TestFixture]
	public abstract class When_sending_a_message_using_the_specified_serializer<TSerializer> :
		LoopbackLocalAndRemoteTestFixture
		where TSerializer : IMessageSerializer, new()
	{
		protected When_sending_a_message_using_the_specified_serializer()
		{
			ConfigureEndpointFactory(x => x.SetDefaultSerializer<TSerializer>());
		}

		[Test]
		public void The_destination_address_should_be_properly_set_on_the_message_envelope()
		{
			var ping = new PingMessage();

			var received = new FutureMessage<PingMessage>();

			RemoteBus.SubscribeHandler<PingMessage>(message =>
				{
					Assert.AreEqual(RemoteBus.Endpoint.Address.Uri, LocalBus.Context().DestinationAddress);

					received.Set(message);
				});

            Thread.Sleep(1.Seconds());

			LocalBus.Publish(ping);

			Assert.IsTrue(received.IsAvailable(10.Seconds()), "Timeout waiting for message");
		}

		[Test]
		public void The_fault_address_should_be_properly_set_on_the_message_envelope()
		{
			var ping = new PingMessage();

			var received = new FutureMessage<PingMessage>();

			RemoteBus.SubscribeHandler<PingMessage>(message =>
				{
					Assert.AreEqual(LocalBus.Endpoint.Address.Uri, LocalBus.Context().FaultAddress);

					received.Set(message);
				});

            Thread.Sleep(1.Seconds());

			LocalBus.Publish(ping, context => context.SendFaultTo(LocalBus.Endpoint.Address.Uri));

			Assert.IsTrue(received.IsAvailable(10.Seconds()), "Timeout waiting for message");
		}

		[Test]
		public void The_message_type_should_be_properly_set_on_the_message_envelope()
		{
			var ping = new PingMessage();

			var received = new FutureMessage<PingMessage>();

			RemoteBus.SubscribeHandler<PingMessage>(message =>
				{
					Assert.AreEqual(typeof(PingMessage).ToMessageName(), LocalBus.Context().MessageType);

					received.Set(message);
				});

            Thread.Sleep(1.Seconds());

            LocalBus.Publish(ping);

			Assert.IsTrue(received.IsAvailable(10.Seconds()), "Timeout waiting for message");
		}

		[Test]
		public void The_response_address_should_be_properly_set_on_the_message_envelope()
		{
			var ping = new PingMessage();

			var received = new FutureMessage<PingMessage>();

			RemoteBus.SubscribeHandler<PingMessage>(message =>
				{
				    var context = LocalBus.Context();
					Assert.AreEqual(LocalBus.Endpoint.Address.Uri, context.ResponseAddress);

					received.Set(message);
				});

            Thread.Sleep(1.Seconds());

			LocalBus.Publish(ping, context => context.SendResponseTo(LocalBus.Endpoint.Address.Uri));

			Assert.IsTrue(received.IsAvailable(10.Seconds()), "Timeout waiting for message");
		}

		[Test]
		public void The_retry_count_should_be_properly_set_on_the_message_envelope()
		{
			var ping = new PingMessage();

			var received = new FutureMessage<PingMessage>();

			int retryCount = 69;
			RemoteBus.SubscribeHandler<PingMessage>(message =>
				{
					Assert.AreEqual(retryCount, LocalBus.Context().RetryCount);

					received.Set(message);
				});

            Thread.Sleep(1.Seconds());

			LocalBus.Publish(ping, context => context.SetRetryCount(retryCount));

			Assert.IsTrue(received.IsAvailable(10.Seconds()), "Timeout waiting for message");
		}

		[Test]
		public void The_source_address_should_be_properly_set_on_the_message_envelope()
		{
			var ping = new PingMessage();

			var received = new FutureMessage<PingMessage>();

			RemoteBus.SubscribeHandler<PingMessage>(message =>
				{
					Assert.AreEqual(LocalBus.Endpoint.Address.Uri, LocalBus.Context().SourceAddress);

					received.Set(message);
				});

            Thread.Sleep(1.Seconds());

			LocalBus.Publish(ping);

			Assert.IsTrue(received.IsAvailable(10.Seconds()), "Timeout waiting for message");
		}
	}


	[TestFixture]
	[Explicit]
	public class For_the_json_message_serializer :
		When_sending_a_message_using_the_specified_serializer<JsonMessageSerializer>
	{
	}

}