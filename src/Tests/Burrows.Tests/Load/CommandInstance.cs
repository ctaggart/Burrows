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
namespace Burrows.Tests.Load
{
    using System;
    using Magnum;

    public class CommandInstance
    {
        public CommandInstance()
        {
            Id = NewIds.NewId.NextGuid();
            CreatedAt = SystemUtil.UtcNow;
        }

        public Guid Id { get; set; }

        public DateTime ResponseReceivedAt { get; set; }

        public Uri Worker { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime ResponseCreatedAt { get; set; }
    }
}