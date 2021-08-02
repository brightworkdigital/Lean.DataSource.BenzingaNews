﻿/*
 * QUANTCONNECT.COM - Democratizing Finance, Empowering Individuals.
 * Lean Algorithmic Trading Engine v2.0. Copyright 2014 QuantConnect Corporation.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
*/

using NUnit.Framework;
using QuantConnect.DataSource.DataQueueHandlers;
using System;

namespace QuantConnect.DataLibrary.Tests
{
    [TestFixture]
    public class BenzingaNewsLiveJsonConverterTests
    {
        [TestCase("Wed Mar 4 2020 19:54:18 GMT+0000 (UTC)")]
        [TestCase("Wed Mar   4 2020 19:54:18  GMT+0000 (UTC)")]
        [TestCase("Wed Mar    4 2020 19:54:18  GMT+0000 (UTC)")]
        [TestCase("Wed Mar     4 2020 19:54:18 GMT+0000  (UTC)")]
        [TestCase("Wed Mar      4 2020 19:54:18 GMT+0000  (UTC)")]
        [TestCase("Wed Mar       4 2020 19:54:18  GMT+0000 (UTC)")]
        public void NormalizeBadDateStringToUtc(string time)
        {
            var expected = new DateTime(2020, 3, 4, 19, 54, 18);
            expected = DateTime.SpecifyKind(expected, DateTimeKind.Utc);

            var actual = BenzingaNewsLiveJsonConverter.NormalizeUtcDateTime(time);

            Assert.AreEqual(expected, actual);
        }
    }
}
