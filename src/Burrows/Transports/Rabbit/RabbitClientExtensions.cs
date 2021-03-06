﻿// Copyright 2007-2012 Chris Patterson, Dru Sellers, Travis Smith, et. al.
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

using Burrows.Logging;
using System;
using RabbitMQ.Client;

namespace Burrows.Transports.Rabbit
{
    public static class RabbitClientExtensions
    {
        private static readonly ILog _log = Logger.Get<RabbitTransportFactory>();

        /// <summary>
        /// Close and dispose of a RabbitMQ channel without throwing any exceptions
        /// </summary>
        /// <param name="channel">The channel (can be null)</param>
        /// <param name="replyCode"></param>
        /// <param name="message">Message for channel closure</param>
        public static void Cleanup(this IModel channel, ushort replyCode = 200, string message = "Unknown")
        {
            if (channel != null)
            {
                try
                {
                    channel.Close(replyCode, message);
                }
                catch (Exception ex)
                {
                    _log.Warn("Failed to close channel", ex);
                }

                try
                {
                    channel.Dispose();
                }
                catch (Exception ex)
                {
                    _log.Warn("Failed to dispose channel", ex);
                }
            }
        }

        /// <summary>
        /// Close and dispose of a RabbitMQ connection without throwing any exceptions
        /// </summary>
        /// <param name="connection">The channel (can be null)</param>
        /// <param name="replyCode"></param>
        /// <param name="message">Message for channel closure</param>
        public static void Cleanup(this IConnection connection, ushort replyCode = 200, string message = "Unknown")
        {
            if (connection != null)
            {
                try
                {
                    connection.Close(replyCode, message);
                }
                catch (Exception ex)
                {
                    _log.Warn("Failed to close connection", ex);
                }

                try
                {
                    connection.Dispose();
                }
                catch (Exception ex)
                {
                    _log.Warn("Failed to dispose connection", ex);
                }
            }
        }

        public static Uri GetUri(this ConnectionFactory factory)
        {
            return new UriBuilder("rabbitmq", factory.HostName, factory.Port, factory.VirtualHost)
            {
                UserName = factory.UserName,
                Password = factory.Password,
            }.Uri;
        } 
    }
}